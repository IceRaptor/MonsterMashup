using BattleTech;
using Harmony;
using HBS.Collections;
using IRBTModUtils.Extension;
using UnityEngine;

namespace MonsterMashup.Patch
{
   // Prevent monster vehicles from aligning to the ground.This should keep them vertical at all times, which is typically what we want.
   [HarmonyPatch(typeof(ActorMovementSequence), "AlignVehicleToGround")]
    static class ActorMovementSequence_AlignVehicleToGround
    {
        static bool Prefix(Transform vehicleTransform, float deltaTime)
        {
            Mod.Log.Info?.Write("AMS:AlignVehicleToGround INVOKED");

            //if (__instance != null && __instance.OwningActor != null)
            //{
            //    TagSet actorTags = __instance.OwningActor.GetStaticUnitTags();
            //    if (actorTags != null && actorTags.Contains(ModTags.NoGroundAlign))
            //    {
            //        Mod.Log.Info?.Write($"actor: {__instance.OwningActor.DistinctId()} has no align tag, skipping alignment.");
            //        return false;
            //    }

            //}

            return true;
        }
    }
}
