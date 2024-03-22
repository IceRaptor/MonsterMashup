using IRBTModUtils;
using IRBTModUtils.Extension;
using System.Collections.Generic;
using UnityEngine;

namespace MonsterMashup.Patch
{

    [HarmonyPatch(typeof(Pathing), "GetMeleeDestsForTarget")]
    static class Pathing_GetMeleeDestsForTarget
    {
        // bool DoesMovementLineCollide(AbstractActor thisActor, List<AbstractActor> actors, Vector3 start, Vector3 end, out AbstractActor collision, float meleeRangeMultiplier)
        static void Postfix(ref List<PathNode> __result, Pathing __instance, AbstractActor target)
        {
            if (target == null) return; // nothing to do

            Mod.Log.Info?.Write($"Checking target: {target.DistinctId()} for no-melee flag");
            if (target.StatCollection.ContainsStatistic(ModStats.Prevent_Melee))
            {
                Mod.Log.Info?.Write($" -- target marked with prevent melee, returning an empty list of pathnodes.");
                __result.Clear();
                return;
            }

        }
    }


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

    [HarmonyPatch(typeof(PathNode), "HasCollisionAt")]
    static class PathNode_HasCollisionAt
    {
        // static bool HasCollisionAt(Vector3 pos, AbstractActor unit, List<AbstractActor> allActors, out AbstractActor occupyingActor)
        static void Postfix(ref bool __result, Vector3 pos, AbstractActor unit, List<AbstractActor> allActors, ref AbstractActor occupyingActor)
        {
            if (unit == null || pos == null || allActors == null || allActors.Count == 0) return; // Nothing to do
            if (__result == true) return;

            // Check for any oversized actors, and block if within radius of them. It's crude, but should work?
            foreach (AbstractActor actor in allActors)
            {
                if (unit.DistinctId().Equals(actor.DistinctId(), System.StringComparison.InvariantCultureIgnoreCase)) continue; // Nothing to do, it's ourselves

                Mod.Log.Trace?.Write($" -- Checking collision for actor: {unit.DistinctId()}, result: {__result}");

                // Magic number; everything in HBS is set to < 12
                if (actor.Radius > 12f)
                {

                    Vector3 centerPoint = SharedState.Combat.HexGrid.GetClosestPointOnGrid(actor.CurrentIntendedPosition);
                    Vector3 posHexGridPoint = SharedState.Combat.HexGrid.GetClosestPointOnGrid(pos);
                    if (centerPoint == posHexGridPoint)
                    {
                        Mod.Log.Debug?.Write($" -- hexPoint: {centerPoint} conflicts with pos: {pos}, marking actor: {actor.DistinctId()} as colliding.");
                        occupyingActor = actor;
                        __result = true;
                        return;
                    }

                    // Check if friendly
                    bool isFriendly = SharedState.Combat.HostilityMatrix.IsFriendly(unit.team, actor.team);
                    if (isFriendly)
                    {
                        Mod.Log.Debug?.Write($" -- unit: {unit.DistinctId()} is friendly to actor: {actor.DistinctId()}, marking as no-colliding.");
                        __result = false;
                        return;
                    }

                    // Neutral or Hostil, check all hexes in our radius
                    int radius = (int)Mathf.Ceil(actor.Radius);
                    List<Vector3> hexGridPoints = SharedState.Combat.HexGrid.GetGridPointsWithinCartesianDistance(actor.CurrentIntendedPosition, radius);
                    Mod.Log.Debug?.Write($" -- actor: {actor.DistinctId()} occupies: {hexGridPoints.Count} hex points from radius: {radius}");
                    Vector3 axialCoords = SharedState.Combat.HexGrid.CartesianToHexAxial(pos);
                    Vector2 vector = SharedState.Combat.HexGrid.HexAxialRound(axialCoords);

                    foreach (Vector3 hexGridPoint in hexGridPoints)
                    {
                        Mod.Log.Trace?.Write($" -- checking hexPoint: {hexGridPoint}");
                        Vector2 axialCoords2 = SharedState.Combat.HexGrid.CartesianToHexAxial(hexGridPoint);
                        if (SharedState.Combat.HexGrid.HexAxialRound(axialCoords2) == vector)
                        {
                            Mod.Log.Debug?.Write($" -- hexPoint: {hexGridPoint} conflicts with pos: {pos}, marking actor: {actor.DistinctId()} as colliding.");
                            occupyingActor = actor;
                            __result = true;
                            return;
                        }

                    }

                }
            }
        }
    }

}
