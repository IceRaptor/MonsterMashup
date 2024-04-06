using BattleTech.Framework;
using CustomComponents;
using IRBTModUtils;
using IRBTModUtils.Extension;
using MonsterMashup.Component;
using MonsterMashup.Helper;
using System;
using System.Collections.Generic;
using UnityEngine;
using static BattleTech.SimGameBattleSimulator;
using static Localize.Text;

namespace MonsterMashup.Patch
{

    //[HarmonyPatch(typeof(MechValidationRules), "ValidateMechTonnage")]
    //static class MechValidationRules_ValidateMechTonnage
    //{
    //    static void Prefix(ref bool __runOriginal, DataManager dataManager, MechDef mechDef, ref Dictionary<MechValidationType, List<Text>> errorMessages)
    //    {
    //        Mod.Log.Info?.Write($"Validating tonnage for unit: {mechDef?.chassisID}");
    //    }
    //}

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

            if (__instance == null) return; // nothing to do

            bool hasParentState = ModState.ParentState.TryGetValue(__instance, out ParentRelationships parentRelationships);

            // Check for child spawns
            bool didSpawn = false;
            if (hasParentState && parentRelationships.SupportSpawnConfigs != null && parentRelationships.SupportSpawnConfigs.Count > 0)
            {
                Mod.Log.Debug?.Write($"Actor: {__instance.DistinctId()} has spawns, iterating configs.");
                foreach (SupportSpawnConfig state in parentRelationships.SupportSpawnConfigs)
                {
                    Mod.Log.Debug?.Write($"Actor: {__instance.DistinctId()} has spawns, iterating configs.");
                    AbstractActor spawnedUnit = state.TrySpawn();
                    if (spawnedUnit != null)
                    {
                        didSpawn = true;
                        parentRelationships.SpawnedSupportActors.Add(spawnedUnit);
                    }
                 
                }
            }

            if (didSpawn)
            {
                Mod.Log.Debug?.Write("Taunting player.");
                // Create a quip
                Guid g = Guid.NewGuid();
                QuipHelper.PlayQuip(SharedState.Combat, g.ToString(), __instance.team, __instance.DisplayName, Mod.LocalizedText.Quips.SupportSpawn, 5f);
            }

            // Check for flee conditions
            if (__instance.StatCollection.ContainsStatistic(ModStats.Flee_On_Round))
            {
                if (SharedState.Combat.TurnDirector.CurrentRound >= __instance.StatCollection.GetValue<int>(ModStats.Flee_On_Round))
                {
                    Mod.Log.Info?.Write($"Forcing mission end due to unit: {__instance.DistinctId()} fleeing on round: {SharedState.Combat.TurnDirector.CurrentRound}!");

                    // Quip and force-end the mission
                    Mod.Log.Debug?.Write("Taunting player.");
                    Guid g = Guid.NewGuid();
                    QuipHelper.PlayQuip(SharedState.Combat, g.ToString(), __instance.team, __instance.DisplayName, Mod.LocalizedText.Quips.Retreat, 5f);

                    // End the mission
                    SharedState.Combat.MessageCenter.PublishMessage(new MissionFailedMessage());
                }
                else
                {
                    Mod.Log.Info?.Write($"Unit: {__instance.DistinctId()} will flee on round: {__instance.StatCollection.GetValue<int>(ModStats.Flee_On_Round)} currentRound: {SharedState.Combat.TurnDirector.CurrentRound}");
                }
            }
            else if (__instance.MechDef.Chassis.Is<FleeComponent>(out FleeComponent fleeComponent))
            {
                // Check trigger
                if (fleeComponent.TriggerOnAllLinkedActorsDead)
                {
                    bool hasAliveChildren = false;

                    if (hasParentState)
                    {
                        hasAliveChildren = parentRelationships.LinkedActors.FindAll(child => !child.IsFlaggedForDeath && !child.IsDead).Count > 0;
                    }

                    if (!hasAliveChildren)
                    {
                        // record the state
                        int roundToFlee = SharedState.Combat.TurnDirector.CurrentRound + fleeComponent.RoundsToDelay;
                        Mod.Log.Info?.Write($"Marking unit: {__instance.DistinctId()} as fleeing on round: {roundToFlee} due to delay: {fleeComponent.RoundsToDelay}");
                        __instance.StatCollection.AddStatistic<int>(ModStats.Flee_On_Round, roundToFlee);

                        // Find the particle systems transform
                        Transform[] childTransforms = __instance.GameRep.GetComponentsInChildren<Transform>(true);
                        foreach (Transform childTF in childTransforms)
                        {
                            Mod.Log.Trace?.Write($"  -- Found transform: {childTF.name}");
                            if (childTF.name.Equals(fleeComponent.ParticleParentGO, StringComparison.InvariantCultureIgnoreCase))
                            {                             
                                Mod.Log.Debug?.Write($" --Enabling particle systems under: {childTF.name}");

                                ParticleSystem[] particleSystems = childTF.GetComponentsInChildren<ParticleSystem>(true);
                                foreach (ParticleSystem ps in particleSystems)
                                {
                                    ps.Stop(true);
                                    ps.Clear(true);
                                    ps.Play(true);
                                }

                                childTF.gameObject.SetActive(true);
                            }
                        }

                        // Quip about takeoff
                        Mod.Log.Debug?.Write("Taunting player.");
                        Guid g = Guid.NewGuid();
                        QuipHelper.PlayQuip(SharedState.Combat, g.ToString(), __instance.team, __instance.DisplayName, Mod.LocalizedText.Quips.RetreatPrep, 5f);
                    }
                }
            }

        }
    }

    [HarmonyPatch(typeof(Mech), "OnPositionUpdate")]
    [HarmonyAfter("io.mission.customunits")]
    static class Mech_OnPositionUpdate
    {
        static void Prefix(ref bool __runOriginal, Mech __instance, ref Vector3 position, ref Quaternion heading, int stackItemUID, bool updateDesignMask, List<DesignMaskDef> remainingMasks, bool skipLogging = false)
        {
            if (__instance == null) return; // nothing to do
            if (!__runOriginal) return; // nothing to do

            Mod.Log.Trace?.Write($"OnPositionUpdate for actor: {__instance.DistinctId()} => newPos: {position}  heading: {heading}");

            // If we're an attached child, make sure we re-align to the parent's target transform and heading when we move.
            //   If we don't, the model will get it's new position from the actual move sequence and get out of alignment with the parent
            if (__instance.StatCollection.ContainsStatistic(ModStats.Linked_Parent_Actor_UID))
            {
                string parentUID = __instance.StatCollection.GetValue<string>(ModStats.Linked_Parent_Actor_UID);
                AbstractActor parent = SharedState.Combat.FindActorByGUID(parentUID);
                bool wasFound = ModState.ParentState.TryGetValue(parent, out ParentRelationships parentRelationships);
                if (wasFound)
                {
                    Transform attachTransform = parentRelationships.LinkedActorTransforms[__instance];
                    __instance.GameRep.transform.position = attachTransform.position;

                    Mod.Log.Trace?.Write($"  aligning actor: {__instance.DistinctId()}");
                    // IF this rotation isn't in place, the units all rotate 90' during the move. Why? No fucking clue.
                    //Quaternion alignVector = attachTransform.rotation * Quaternion.Euler(90f, 0f, 0f);
                    Quaternion alignVector = attachTransform.rotation * Quaternion.Euler(0f, 0f, 0f);
                    Quaternion linkedRot = Quaternion.RotateTowards(__instance.GameRep.transform.rotation, alignVector, 9999f);
                    __instance.GameRep.transform.rotation = linkedRot;
                    position = attachTransform.position;
                    heading = linkedRot;

                }
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
