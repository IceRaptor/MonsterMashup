using CustomComponents;
using IRBTModUtils;
using IRBTModUtils.Extension;
using MonsterMashup.Component;
using MonsterMashup.Helper;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MonsterMashup.Patch
{

    [HarmonyPatch(typeof(Mech), "DEBUG_DamageLocation")]
    static class Mech_DEBUG_DamageLocation
    {
        static void Postfix(Mech __instance, ArmorLocation aLoc)
        {
            Mod.Log.Info?.Write("Mech:DEBUG_DamageLocation INVOKED");

            if (Mod.Config.DeveloperOptions.DebugDamageLocationKillsLinkedTurrets)
            {
                ChassisLocations chassisLocationFromArmorLocation = MechStructureRules.GetChassisLocationFromArmorLocation(aLoc);
                Mod.Log.Info?.Write($" Developer option to kill linked turrets invoked on actor: {__instance.DistinctId()} at location: {aLoc}");
                LinkedEntityHelper.DestroyLinksInLocation(__instance, chassisLocationFromArmorLocation);
            }
        }
    }

    [HarmonyPatch(typeof(Mech), "OnActivationEnd")]
    static class Mech_OnActivationEnd
    {
        static void Postfix(Mech __instance, string sourceID, int stackItemID)
        {
            bool hasSpawns = ModState.ChildSpawns.TryGetValue(__instance.DistinctId(), out List<SupportSpawnState> spawns);
            bool didSpawn = false;
            if (hasSpawns)
            {
                Mod.Log.Debug?.Write($"Actor: {__instance.DistinctId()} has spawns, iterating configs.");
                foreach (SupportSpawnState state in spawns)
                {
                    Mod.Log.Debug?.Write($"Actor: {__instance.DistinctId()} has spawns, iterating configs.");
                    if (state.TrySpawn())
                        didSpawn = true;
                }
            }

            if (didSpawn)
            {
                Mod.Log.Debug?.Write("Taunting player.");
                // Create a quip
                Guid g = Guid.NewGuid();
                QuipHelper.PlayQuip(SharedState.Combat, g.ToString(), __instance.team, __instance.DisplayName, Mod.LocalizedText.Quips.SupportSpawn, 5f);
            }

        }
    }

    [HarmonyPatch(typeof(Mech), "OnPositionUpdate")]
    [HarmonyAfter("io.mission.customunits")]
    static class Mech_OnPositionUpdate
    {
        static void Prefix(ref bool __runOriginal, Mech __instance, ref Vector3 position, ref Quaternion heading, int stackItemUID, bool updateDesignMask, List<DesignMaskDef> remainingMasks, bool skipLogging = false)
        {
            Mod.Log.Trace?.Write($"OnPositionUpdate for actor: {__instance.DistinctId()} => newPos: {position}  heading: {heading}");

            // If we're an attached child, make sure we re-align to the parent's target transform and heading when we move.
            //   If we don't, the model will get it's new position from the actual move sequence and get out of alignment with the parnet
            bool isAttached = ModState.AttachTransforms.TryGetValue(__instance.DistinctId(), out Transform attachTransform);
            if (isAttached)
            {
                __instance.GameRep.transform.position = attachTransform.position;

                Mod.Log.Trace?.Write($"  aligning actor");
                Quaternion alignVector = attachTransform.rotation * Quaternion.Euler(90f, 0f, 0f);
                Quaternion linkedRot = Quaternion.RotateTowards(__instance.GameRep.transform.rotation, alignVector, 9999f);
                __instance.GameRep.transform.rotation = linkedRot;
                position = attachTransform.position;
                heading = linkedRot;
            }

            __runOriginal = true;
        }

        // Check to see if the unit has 'crush on collision' enabled. If so, destroy any enemy units within range of us
        static void Postfix(Mech __instance, ref Vector3 position, ref Quaternion heading)
        {
            Mod.Log.Trace?.Write($"OnPositionUpdate for actor: {__instance.DistinctId()} => newPos: {position}  heading: {heading}");
            if (__instance.StatCollection.ContainsStatistic(ModStats.Crush_On_Move))
            {
                __instance.MechDef.Chassis.Is(out CrushOnCollisionComponent crushComponent);
                Mod.Log.Info?.Write($"Actor: {__instance.DistinctId()} marked with crush on move, checking for nearby hostiles");
                foreach (AbstractActor target in SharedState.Combat.AllActors)
                {
                    if (target.IsFlaggedForDeath || target.IsDead) continue; // nothing to do

                    float distToTarget = Vector3.Distance(target.CurrentIntendedPosition, position);
                    bool isHostile = SharedState.Combat.HostilityMatrix.IsEnemy(target.team.GUID, __instance.team.GUID);
                    Mod.Log.Debug?.Write($"  -- checking target: {target.DistinctId()} distance of: {distToTarget} vs. radius: {crushComponent.Radius}  isHostile: {isHostile}");
                    if (distToTarget < crushComponent.Radius && isHostile)
                    {
                        Mod.Log.Info?.Write($"  target: {target.DistinctId()} within crush radius {crushComponent.Radius}, destroying.");
                        UnitHelper.CrushTarget(__instance, target, crushComponent.PlayTaunt);
                    }
                }
            }
        }
    }
}
