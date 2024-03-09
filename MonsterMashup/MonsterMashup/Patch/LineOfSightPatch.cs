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
        static void Prefix(ref bool __runOriginal, LineOfSight __instance, ref VisibilityLevel __result, ICombatant target)
        {
            if (!__runOriginal) return;

            if (__instance == null || target == null) return;

            string parentUID = target.StatCollection.GetValue<string>(ModStats.Linked_Parent_Actor_UID);
            if (!string.IsNullOrEmpty(parentUID))
            {
                Mod.Log.Trace?.Write($"Target: {target.DistinctId()} has linked parent: {parentUID}");
                AbstractActor parent = SharedState.Combat.FindActorByGUID(parentUID);
                if (parent != null && parent.GameRep != null && !parent.GameRep.VisibleToPlayer)
                {
                    Mod.Log.Trace?.Write($"  --parent is not visible, skipping.");
                    __result = VisibilityLevel.None;
                    __runOriginal = false;
                }
            }

        }
    }

}
