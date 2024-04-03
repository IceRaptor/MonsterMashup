using IRBTModUtils;
using IRBTModUtils.Extension;
using MonsterMashup.Helper;
using MonsterMashup.UI;
using System.Collections.Generic;

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

        static void Prefix(ref bool __runOriginal, AbstractActor __instance, ref VisibilityLevel newLevel)
        {
            if (!__runOriginal) return; // nothing to do
            if (__instance == null) return; // nothing to do

            // Check for parent's visibility, and try to match
            string parentUID = __instance.StatCollection.GetValue<string>(ModStats.Linked_Parent_Actor_UID);
            if (!string.IsNullOrEmpty(parentUID))
            {
                AbstractActor parent = SharedState.Combat.FindActorByGUID(parentUID);
                if (parent != null && parent.GameRep != null)
                {
                    VisibilityLevel parentVisibility = SharedState.Combat.LocalPlayerTeam.VisibilityToTarget(parent);
                    Mod.Log.Info?.Write($"Target: {__instance.DistinctId()} has linked parent: {parentUID} with visibility: {parentVisibility}");
                    if (parentVisibility < VisibilityLevel.LOSFull)
                    {
                        Mod.Log.Info?.Write($"  --parent is not visible, hiding.");
                        newLevel = VisibilityLevel.None;
                        return;
                    }
                    else if (parentVisibility == VisibilityLevel.LOSFull)
                    {
                        Mod.Log.Info?.Write($"  --parent is visible, force showing.");
                        newLevel = VisibilityLevel.LOSFull;
                        return;
                    }
                }
            }
        }

        static void Postfix(AbstractActor __instance, VisibilityLevel newLevel)
        {
            if (__instance == null) return; // nothing to do
            Mod.Log.Info?.Write($"AA:OnPlayerVisibilityChanged:POST INVOKED for: {__instance.DistinctId()}");

            if (newLevel == VisibilityLevel.LOSFull && Mod.Config.DeveloperOptions.EnableFootprintVis)
            {
                if (ModState.FootprintVisuals.TryGetValue(__instance.DistinctId(), out FootprintVisualization footprintVis))
                {
                    Mod.Log.Info?.Write($"Showing footprint visualization for actor: {__instance.DistinctId()}");
                    footprintVis.Show();
                }
            }

            // Check for linked turrets, hide them if less than full vis
            bool hasKey = ModState.ParentToLinkedActors.TryGetValue(__instance.DistinctId(), out List<AbstractActor> children);
            if (hasKey)
            {
                VisibilityLevel parentVisibility = SharedState.Combat.LocalPlayerTeam.VisibilityToTarget(__instance);
                foreach (AbstractActor child in children)
                {
                    VisibilityLevel childVisiblity = SharedState.Combat.LocalPlayerTeam.VisibilityToTarget(child);
                    Mod.Log.Info?.Write($"Parent: {__instance.DistinctId()} has visibilty: {parentVisibility}, child: {child.DistinctId()} has visibilty: {childVisiblity}");
                    
                    if (parentVisibility < VisibilityLevel.LOSFull)
                    {
                        Mod.Log.Info?.Write($"Parent: {__instance.DistinctId()} has visibility < LOFFull, hiding child: {child.DistinctId()}.");
                        child.OnPlayerVisibilityChanged(VisibilityLevel.None);
                    }
                    else if (parentVisibility == VisibilityLevel.LOSFull)
                    {
                        Mod.Log.Info?.Write($"Parent: {__instance.DistinctId()} has visibility == LOFFull, showing child: {child.DistinctId()}.");
                        child.OnPlayerVisibilityChanged(VisibilityLevel.LOSFull);
                    }
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
