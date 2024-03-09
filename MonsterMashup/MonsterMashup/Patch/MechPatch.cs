using IRBTModUtils.Extension;
using MonsterMashup.Helper;
using static CustomUnits.CustomMech;
using System.Collections.Generic;
using UnityEngine;
using HBS.Collections;
using IRBTModUtils;
using CustomUnits;
using BattleTech;

namespace MonsterMashup.Patch
{
    // TODO: May be unnecessary, will be invoked from component destruction
    //[HarmonyPatch(typeof(Mech), "NukeStructureLocation")]
    //static class Mech_NukeStructureLocation
    //{
    //    static void Postfix(Mech __instance,
    //        WeaponHitInfo hitInfo, int hitLoc, ChassisLocations location, Vector3 attackDirection, DamageType damageType)
    //    {
    //        Mod.Log.Info?.Write("Mech:DamageLocation INVOKED");

    //        if (__instance == null) return;

    //        Mod.Log.Info?.Write($"Structure at location: {location} on actor: {__instance.DistinctId()} has been nuked");
    //        LinkedEntityHelper.DestroyLinksInLocation(__instance, location);
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

    [HarmonyPatch(typeof(Mech), "OnPositionUpdate")]
    [HarmonyAfter("io.mission.customunits")]
    static class Mech_OnPositionUpdate
    {
        static void Postfix(Mech __instance, Vector3 position, Quaternion heading, int stackItemUID, bool updateDesignMask, List<DesignMaskDef> remainingMasks, bool skipLogging = false)
        {
            if (ModState.Parents.Contains(__instance))
            {
                CustomMech customMech = __instance as CustomMech;
                if (customMech != null)
                {
                    // Align linked actors to the current position and rotation of the parent. Works around some CU functionality that doesn't seem to trigger properly
                    Mod.Log.Info?.Write($"Force-invoking CustomMech.OnPositionUpdate for actor: {__instance.DistinctId()}");
                    foreach (LinkedActor link in customMech.linkedActors)
                    {
                        Mod.Log.Info?.Write($"  LinkedActor: {link.actor.DistinctId()} keepPosition: {link.keepPosition} relativePosition: {link.relativePosition}");
                        if (link.keepPosition == true) { continue; }

                        ModState.AttachTransforms.TryGetValue(link.actor.DistinctId(), out Transform attachTransform);
                        if (attachTransform == null)
                        {
                            Mod.Log.Warn?.Write("AttachTransform is null for actor!");
                        }

                        Mod.Log.Info?.Write($"  OnPositionUpdate linkedCurrentPos: {link.actor.currentPosition} relativePos: {link.relativePosition} newPos: {position} linkedNewPos: {position + link.relativePosition}");
                        link.actor.OnPositionUpdate(attachTransform.position, heading, stackItemUID, updateDesignMask, remainingMasks, skipLogging);
                        link.actor.GameRep.transform.position = attachTransform.position;
                        link.actor.CurrentPosition = link.actor.GameRep.transform.position;

                        Mod.Log.Info?.Write($"  aligning actor");
                        Quaternion alignVector = attachTransform.rotation * Quaternion.Euler(90f, 0f, 0f);
                        link.actor.GameRep.transform.rotation = Quaternion.RotateTowards(link.actor.GameRep.transform.rotation, alignVector, 9999f);
                        link.actor.CurrentRotation = link.actor.GameRep.transform.rotation;
                    }
                }
            }
        }
    }

}
