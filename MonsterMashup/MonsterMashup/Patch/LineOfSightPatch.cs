using CustomUnits;
using IRBTModUtils;
using IRBTModUtils.Extension;
using MonsterMashup.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Localize.Text;

namespace MonsterMashup.Patch
{
    [HarmonyPatch(typeof(LineOfSight), "GetLineOfFireUncached")]
    //[HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(Vector3), typeof(ICombatant), typeof(Vector3), typeof(Quaternion) })]
    public static class LineOfSight_GetLineOfFireUncached
    {
        //public static List<string> IgnoredActors = new List<string>();

        static void Prefix(ref bool __runOriginal, ref LineOfFireLevel __result, LineOfSight __instance, AbstractActor source, Vector3 sourcePosition, ICombatant target, Vector3 targetPosition, Quaternion targetRotation, ref Vector3 collisionWorldPos, ref HashSet<ICustomMech> __state)
        {
            __state = null;
            if (source == null) return; // Nothing to do
            __state = new HashSet<ICustomMech>();
            try
            {
                // if source is parent, all children are ignored            
                bool hasKey = ModState.ParentState.TryGetValue(source, out ParentRelationships sourceRelationships);
                if (hasKey)
                {
                    //IgnoredActors.AddRange(sourceRelationships.LinkedActors.Select(c => c.DistinctId()));
                    foreach (var link in sourceRelationships.LinkedActors)
                    {
                        if (link is ICustomMech custMech)
                        {
                            __state.Add(custMech);
                            custMech.setTemporaryDead(true);
                        }
                    }
                    return;
                }

                // Check if source is a child
                string parentUID = source.StatCollection.GetValue<string>(ModStats.Linked_Parent_Actor_UID);
                if (!string.IsNullOrEmpty(parentUID))
                {
                    AbstractActor parent = SharedState.Combat.FindActorByGUID(parentUID);
                    if (parent != null)
                    {
                        // Ignore the parent
                        //IgnoredActors.Add(parent.DistinctId());
                        if (parent is ICustomMech custParent)
                        {
                            __state.Add(custParent);
                            custParent.setTemporaryDead(true);
                        }
                        // Check parent for children
                        hasKey = ModState.ParentState.TryGetValue(parent, out ParentRelationships parentRelationships);
                        if (hasKey)
                        {
                            //IgnoredActors.AddRange(parentRelationships.LinkedActors.Select(s => s.DistinctId()));
                            foreach (var link in parentRelationships.LinkedActors)
                            {
                                if (link is ICustomMech custMech)
                                {
                                    __state.Add(custMech);
                                    custMech.setTemporaryDead(true);
                                }
                            }
                            return;
                        }

                    }
                }
            }catch(Exception e)
            {
                CombatGameState.gameInfoLogger.LogException(e);
            }
        }

        static void Postfix(ref bool __runOriginal, ref LineOfFireLevel __result, LineOfSight __instance, AbstractActor source, Vector3 sourcePosition, ICombatant target, Vector3 targetPosition, Quaternion targetRotation, ref Vector3 collisionWorldPos, ref HashSet<ICustomMech> __state)
        {
            if (__state == null) { return; }    
            foreach(var dead in __state)
            {
                dead.setTemporaryDead(false);
            }
            //IgnoredActors.Clear();               
        }

    }

    //[HarmonyPatch(typeof(Mech), "IsDead", MethodType.Getter)]
    //static class Mech_IsDead_GETTER
    //{
    //    static void Postfix(ref bool __result, Mech __instance)
    //    {
    //        Mod.Log.Trace?.Write("Mech:IsDead:GET invoked");
    //        if (LineOfSight_GetLineOfFireUncached.IgnoredActors != null && LineOfSight_GetLineOfFireUncached.IgnoredActors.Count > 0)
    //        {
    //            if (LineOfSight_GetLineOfFireUncached.IgnoredActors.Contains(__instance.DistinctId()))
    //            {
    //                Mod.Log.Debug?.Write($"Treating actor: {__instance.DistinctId()} as if it were dead for LoS purposes.");
    //                __result = true;
    //            };
    //        }
    //    }
    //}
}
