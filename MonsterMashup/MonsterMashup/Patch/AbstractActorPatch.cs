﻿using IRBTModUtils;
using IRBTModUtils.Extension;
using MonsterMashup.Helper;
using MonsterMashup.UI;

namespace MonsterMashup.Patch
{

    [HarmonyPatch(typeof(AbstractActor), "BaseInitiative", MethodType.Getter)]
    static class AbstractActor_BaseInitiative
    {
        static bool Prepare() => Mod.Config.LinkChildInitiative;

        static void Postfix(AbstractActor __instance, ref int __result)
        {
            Mod.Log.Debug?.Write($"AA:BaseInitiative:Getter - entered for actor: {__instance.DistinctId()}");

            if (__instance == null) return; // nothing to do

            if (!__instance.Combat.TurnDirector.IsInterleaved)
                return; // Nothing to do; non-combat turn

            string parentID = __instance.StatCollection.GetValue<string>(ModStats.Linked_Parent_Actor_UID);
            if (parentID == null)
                return; // Nothing to do; not a linked turret

            AbstractActor parent = SharedState.Combat.FindActorByGUID(parentID);
            if (parent != null)
            {
                int parentInit = parent.Initiative;
                Mod.Log.Info?.Write($"Parent actor: {parent.DistinctId()} has initiative: {parentInit} vs. our result: {__result}");
                __result = parentInit;
            }
            else
            {
                Mod.Log.Warn?.Write($"Failed to find parent via ID: {parentID}, cannot link initiative!");
            }
        }
    }

    [HarmonyPatch(typeof(AbstractActor), "OnPlayerVisibilityChanged")]
    static class AbstractActor_OnPlayerVisibilityChanged
    {
        static void Postfix(AbstractActor __instance, VisibilityLevel newLevel)
        {
            Mod.Log.Info?.Write($"AA:OnPlayerVisibilityChanged:POST INVOKED for: {__instance.DistinctId()}");

            if (newLevel == VisibilityLevel.LOSFull && Mod.Config.DeveloperOptions.EnableFootprintVis)
            {
                if (ModState.FootprintVisuals.TryGetValue(__instance.DistinctId(), out FootprintVisualization footprintVis))
                {
                    Mod.Log.Info?.Write($"Showing footprint visualization for actor: {__instance.DistinctId()}");
                    footprintVis.Show();
                }
            }
        }
    }

    [HarmonyPatch(typeof(AbstractActor), "FlagForDeath")]
    static class AbstractActor_FlagForDeath
    {
        static void Postfix(AbstractActor __instance)
        {
            if (__instance is Mech mech)
            {
                Mod.Log.Info?.Write("Mech:FlagForDeath INVOKED");
                LinkedEntityHelper.DestroyLinksInLocation(mech, ChassisLocations.All);
            }
        }
    }

}
