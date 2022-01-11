using BattleTech;
using Harmony;
using IRBTModUtils;
using IRBTModUtils.Extension;
using System;
using UnityEngine;

namespace MonsterMashup.Patch
{
    [HarmonyPatch(typeof(LineOfSight), "GetVisibilityToTargetWithPositionsAndRotations")]
    [HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(Vector3), typeof(ICombatant), typeof(Vector3), typeof(Quaternion) })]
    static class LineOfSight_GetVisibilityToTargetWithPositionsAndRotations
    {
        static bool Prefix(LineOfSight __instance, ref VisibilityLevel __result,
            AbstractActor source, Vector3 sourcePosition, ICombatant target, Vector3 targetPosition, Quaternion targetRotation)
        {
            if (__instance == null || target == null) return true;
            
            string parentUID = target.StatCollection.GetValue<string>(ModStats.Linked_Parent_Actor_UID);
            if (!string.IsNullOrEmpty(parentUID))
            {
                Mod.Log.Debug?.Write($"Target: {target.DistinctId()} has linked parent: {parentUID}");
                AbstractActor parent = SharedState.Combat.FindActorByGUID(parentUID);
                if (parent != null && parent.GameRep != null && !parent.GameRep.VisibleToPlayer)
                {
                    Mod.Log.Debug?.Write($"  --parent is not visible, skipping.");
                    __result = VisibilityLevel.None;
                    return false;
                }
            }

            return true;
        }
    }

}
