using IRBTModUtils;
using MonsterMashup.Helper;
using MonsterMashup.UI;
using System;

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
                Mod.Log.Error?.Write(e, "Failed to load linked resources due to exception!");
            }
        }
    }

    [HarmonyPatch(typeof(TurnDirector), "StartFirstRound")]
    static class TurnDirector_StartFirstRound
    {

        static void Postfix()
        {
            Mod.Log.Trace?.Write("TD:StartFirstRound - entered.");
            foreach (FootprintVisualization footprintVis in ModState.FootprintVisuals.Values)
            {
                footprintVis.Init();
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
