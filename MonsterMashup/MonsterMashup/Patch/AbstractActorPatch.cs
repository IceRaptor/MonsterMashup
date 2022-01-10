using BattleTech;
using Harmony;
using IRBTModUtils;
using IRBTModUtils.Extension;
using MonsterMashup.UI;

namespace MonsterMashup.Patch
{
    [HarmonyPatch(typeof(AbstractActor), "Initiative", MethodType.Setter)]
    static class AbstractActor_Initiative_Setter
    {
        static void Postfix()
        {
            Mod.Log.Info?.Write("AA:Initiative:Setter INVOKED");
        }
    }

    [HarmonyPatch(typeof(AbstractActor), "Initiative", MethodType.Getter)]
    static class AbstractActor_Initiative_Getter
    {
        static void Postfix()
        {
            Mod.Log.Info?.Write("AA:Initiative:Getter INVOKED");
        }
    }

    [HarmonyPatch(typeof(AbstractActor))]
    [HarmonyPatch("BaseInitiative", MethodType.Getter)]
    static class AbstractActor_BaseInitiative
    {
        static bool Prepare() => Mod.Config.LinkChildInitiative;

        static void Postfix(AbstractActor __instance, ref int __result, StatCollection ___statCollection)
        {
            Mod.Log.Info?.Write("AA:BaseInitiative:Getter - entered.");

            if (__instance == null || !(__instance is Turret turret)) return; // nothing to do

            if (!__instance.Combat.TurnDirector.IsInterleaved)
                return; // Nothing to do; non-combat turn

            string parentID = turret.StatCollection.GetValue<string>(ModStats.Linked_Parent_Actor_UID);
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
        static bool Prefix(AbstractActor __instance)
        {
            Mod.Log.Info?.Write($"AA:OnPlayerVisibilityChanged:PRE INVOKED for: {__instance.DistinctId()}");
            if (__instance != null)
            {
                if (ModState.LinkedTurrets.TryGetValue(__instance.DistinctId(), out AbstractActor parent))
                {
                    // Do not show turret icon blips; wait until the entire parent is shown
                    if (!parent.GameRep.VisibleToPlayer)
                    {
                        Mod.Log.Info?.Write($" parent gameRep is not visible to player, skipping");
                        return false;
                    }
                    else
                    {
                        Mod.Log.Info?.Write($" parent gameRep is visible to player, showing turret");
                    }
                }
            }

            return true;
        }

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
}
