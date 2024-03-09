using IRBTModUtils.Extension;
using MonsterMashup.Helper;
using static CustomUnits.CustomMech;
using System.Collections.Generic;
using UnityEngine;
using HBS.Collections;
using IRBTModUtils;
using CustomUnits;
using BattleTech;
using CustomComponents;

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

    [HarmonyPatch(typeof(Mech), "OnPositionUpdate")]
    [HarmonyAfter("io.mission.customunits")]
    static class Mech_OnPositionUpdate
    {
        static void Prefix(ref bool __runOriginal, Mech __instance, ref Vector3 position, ref Quaternion heading, int stackItemUID, bool updateDesignMask, List<DesignMaskDef> remainingMasks, bool skipLogging = false)
        {
            Mod.Log.Info?.Write($"OnPositionUpdate for actor: {__instance.DistinctId()} => newPos: {position}  heading: {heading}");

            // If we're an attached child, make sure we re-align to the parent's target transform and heading when we move.
            //   If we don't, the model will get it's new position from the actual move sequence and get out of alignment with the parnet
            bool isAttached = ModState.AttachTransforms.TryGetValue(__instance.DistinctId(), out Transform attachTransform);
            if (isAttached)
            {
                __instance.GameRep.transform.position = attachTransform.position;

                Mod.Log.Info?.Write($"  aligning actor");
                Quaternion alignVector = attachTransform.rotation * Quaternion.Euler(90f, 0f, 0f);
                Quaternion linkedRot = Quaternion.RotateTowards(__instance.GameRep.transform.rotation, alignVector, 9999f);
                __instance.GameRep.transform.rotation = linkedRot;
                position = attachTransform.position;
                heading = linkedRot;
            }

            __runOriginal = true;
        }
    }

}
