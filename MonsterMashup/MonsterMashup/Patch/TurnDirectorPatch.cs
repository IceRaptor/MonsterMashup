using BattleTech;
using Harmony;
using IRBTModUtils;
using MonsterMashup.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterMashup.Patch
{
    // Defer until all actors are initialized
    [HarmonyPatch(typeof(TurnDirector), "OnInitializeContractComplete")]
    public static class TurnDirector_OnInitializeContractComplete
    {
        public static void Postfix(TurnDirector __instance, MessageCenterMessage message)
        {
            Mod.Log.Trace?.Write("TD:OICC - entered.");

            foreach (Team team in SharedState.Combat.Teams)
            {
                if (team.GUID == TeamDefinition.TargetsTeamDefinitionGuid) ModState.TargetTeam = team;
                else if (team.GUID == TeamDefinition.TargetsAllyTeamDefinitionGuid) ModState.TargetAllyTeam = team;
                else if (team.GUID == TeamDefinition.HostileToAllTeamDefinitionGuid) ModState.HostileToAllTeam = team;
            }
            Mod.Log.Info?.Write($"" +
                $"TargetTeam identified as: {ModState.TargetTeam?.DisplayName}  " +
                $"TargetAllyTeam identified as: {ModState.TargetAllyTeam?.DisplayName}  " +
                $"HostileToAllTeam identified as: {ModState.HostileToAllTeam?.DisplayName}.");

            // Load any resources necessary for our ambush
            try
            {
                DataLoadHelper.LoadLinkedResources(SharedState.Combat);
            }
            catch (Exception e)
            {
                Mod.Log.Error?.Write(e, "Failed to load ambush resources due to exception!");
            }
        }
    }

    [HarmonyPatch(typeof(TurnDirector), "OnCombatGameDestroyed")]
    static class TurnDirector_OnCombatGameDestroyed
    {

        static void Postfix()
        {
            Mod.Log.Trace?.Write("TD:OCGD - entered.");

            // Reset any combat state
            ModState.Reset();
        }
    }
}
