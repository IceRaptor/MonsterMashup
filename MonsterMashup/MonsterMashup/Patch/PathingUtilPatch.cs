using IRBTModUtils.Extension;
using System.Collections.Generic;
using UnityEngine;

namespace MonsterMashup.Patch
{
    [HarmonyPatch(typeof(PathingUtil), "DoesMovementLineCollide")]
    [HarmonyAfter("io.mission.customunits")]
    static class PathingUtil_DoesMovementLineCollide
    {
        // bool DoesMovementLineCollide(AbstractActor thisActor, List<AbstractActor> actors, Vector3 start, Vector3 end, out AbstractActor collision, float meleeRangeMultiplier)
        static void Postfix(ref bool __result, AbstractActor thisActor, List<AbstractActor> actors, Vector3 start, Vector3 end)
        {

            Mod.Log.Info?.Write($" -- SourceActor: {thisActor.DistinctId()} has radius: {thisActor.Radius} with doesCollide: {__result}");
            foreach (AbstractActor targetActor in actors)
            {
                Mod.Log.Info?.Write($" ---- TargetActor: {targetActor.DistinctId()} has radius: {targetActor.Radius}");
            }


        }
    }

}
