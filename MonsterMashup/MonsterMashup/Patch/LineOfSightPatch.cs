using IRBTModUtils;
using IRBTModUtils.Extension;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Localize.Text;

//namespace MonsterMashup.Patch
//{
//    [HarmonyPatch(typeof(LineOfSight), "GetVisibilityToTargetWithPositionsAndRotations")]
//    [HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(Vector3), typeof(ICombatant), typeof(Vector3), typeof(Quaternion) })]
//    static class LineOfSight_GetVisibilityToTargetWithPositionsAndRotations
//    {
//        // check to see if our parent's visibility is none. If so, hide our own visiblity.
//        static void Prefix(ref bool __runOriginal, LineOfSight __instance, ref VisibilityLevel __result, ICombatant target)
//        {
//            Mod.Log.Trace?.Write($"GVTTWPAR Target: {target.DistinctId()}");
//            if (!__runOriginal) return;

//            if (__instance == null || target == null) return;

//            string parentUID = target.StatCollection.GetValue<string>(ModStats.Linked_Parent_Actor_UID);
//            if (!string.IsNullOrEmpty(parentUID))
//            {
//                Mod.Log.Info?.Write($"Target: {target.DistinctId()} has linked parent: {parentUID}");
//                AbstractActor parent = SharedState.Combat.FindActorByGUID(parentUID);
//                if (parent != null && parent.GameRep != null)
//                {
//                    VisibilityLevel parentVisibility = SharedState.Combat.LocalPlayerTeam.VisibilityToTarget(parent);
//                    if (!parent.GameRep.VisibleToPlayer || parentVisibility < VisibilityLevel.LOSFull)
//                    {
//                        Mod.Log.Info?.Write($"  --parent is not visible, skipping.");
//                        __result = VisibilityLevel.None;
//                        __runOriginal = false;
//                        return;
//                    }
//                }
//            }

//        }

//        // check to see if our parent's visibility is greater than ours. If so, up our visibility to the same level as the parent
//        static void Postfix(LineOfSight __instance, ref VisibilityLevel __result, ICombatant target)
//        {
//            Mod.Log.Trace?.Write($"GVTTWPAR Target: {target.DistinctId()}");

//            if (__instance == null || target == null) return;

//            string parentUID = target.StatCollection.GetValue<string>(ModStats.Linked_Parent_Actor_UID);
//            if (!string.IsNullOrEmpty(parentUID))
//            {
//                Mod.Log.Info?.Write($"Target: {target.DistinctId()} has linked parent: {parentUID}");
//                AbstractActor parent = SharedState.Combat.FindActorByGUID(parentUID);
//                if (parent != null && parent.GameRep != null)
//                {
//                    VisibilityLevel parentVisibility = SharedState.Combat.LocalPlayerTeam.VisibilityToTarget(parent);
//                    if (parentVisibility == VisibilityLevel.LOSFull)
//                    {
//                        __result = VisibilityLevel.LOSFull;
//                        Mod.Log.Info?.Write($"  -- upping visibility to parent's level of: {parentVisibility}");
//                    }
//                    else
//                    {
//                        __result = VisibilityLevel.None;
//                        Mod.Log.Info?.Write($"  -- Hiding sensor blip");
//                    }
//                }
//            }

//            // Check for linked turrets, hide them if less than full vis
//            bool hasKey = ModState.ParentToLinkedActors.TryGetValue(target.DistinctId(), out List<AbstractActor> children);
//            if (hasKey)
//            {
//                VisibilityLevel parentVisibility = SharedState.Combat.LocalPlayerTeam.VisibilityToTarget(target);
//                if (parentVisibility < VisibilityLevel.LOSFull)
//                {
//                    Mod.Log.Debug?.Write($"Parent: {target.DistinctId()} has visiblity less than LOFFull, hiding children.");
//                    foreach (AbstractActor child in children)
//                    {
//                        Mod.Log.Debug?.Write($"  -- hiding child: {child.DistinctId()}");
//                        child.OnPlayerVisibilityChanged(VisibilityLevel.None);
//                    }
//                }
//            }
//        }
//    }

//}
