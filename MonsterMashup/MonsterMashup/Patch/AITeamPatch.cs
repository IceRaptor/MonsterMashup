using IRBTModUtils;
using IRBTModUtils.Extension;
using System;
using System.Collections.Generic;

namespace MonsterMashup.Patch
{
    [HarmonyPatch(typeof(AITeam), "selectCurrentUnit")]
    static class AITeam_selectCurrentUnit
    {
        static bool Prepare() => Mod.Config.LinkChildInitiative;

        static void Postfix(AITeam __instance, ref AbstractActor __result)
        {
            Mod.Log.Info?.Write($"AITeam::selectCurrentUnit - unit: {__result.DistinctId()}");

            if (__result == null) return;

            string parentUID = __result.StatCollection.GetValue<string>(ModStats.Linked_Parent_Actor_UID);
            Mod.Log.Info?.Write($"  unit: {__result.DistinctId()}  has parentUID: {parentUID}");
            if (parentUID != null)
            {
                List<AbstractActor> unusedUnits = __instance.GetUnusedUnitsForCurrentPhase();
                Mod.Log.Info?.Write($"Currently {unusedUnits} unused units in phase: {SharedState.Combat.TurnDirector.CurrentPhase}");
                foreach (AbstractActor actor in unusedUnits)
                {
                    Mod.Log.Debug?.Write($"actor: {actor.DistinctId()}");
                    if (actor.uid.Equals(parentUID, StringComparison.InvariantCultureIgnoreCase))
                    {
                        Mod.Log.Debug?.Write($"Found parent actor: {actor.DistinctId()}");
                        if (!actor.HasActivatedThisRound)
                        {
                            Mod.Log.Debug?.Write($"  parent has not yet activated, returning them instead of the child.");
                            __result = actor;
                            return;
                        }
                    }
                }
            }
        }

    }
}
