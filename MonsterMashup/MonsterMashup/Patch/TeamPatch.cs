using BattleTech;
using Harmony;
using IRBTModUtils.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterMashup.Patch
{

    [HarmonyPatch(typeof(Team), "AddUnit")]
    public static class Team_AddUnit
    {
        public static void Prefix(Team __instance, AbstractActor unit)
        {
            Mod.Log.Info?.Write($"Adding unit: {unit.DistinctId()} to team: {__instance.DisplayName}");

            //if (__instance.Combat.TurnDirector.CurrentRound > 1)
            //{
            //    // We are spawning reinforcements. Do the work ahead of the main call to prevent it from failing in the visibility lookups
            //    if (__instance.units == null)
            //    {
            //        __instance.units = new List<AbstractActor>();
            //    }
            //    if (__instance.units.Contains(unit))
            //    {
            //        return;
            //    }
            //    __instance.Combat.combatantAdded = true;
            //    __instance.units.Add(unit);
            //    unit.AddToTeam(__instance);
            //}
        }
    }
}
