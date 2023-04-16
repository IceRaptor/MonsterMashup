using IRBTModUtils.Extension;

namespace MonsterMashup.Patch
{

    [HarmonyPatch(typeof(Team), "AddUnit")]
    public static class Team_AddUnit
    {
        public static void Prefix(ref bool __runOriginal, Team __instance, AbstractActor unit)
        {
            if (!__runOriginal) return;

            Mod.Log.Info?.Write($"Adding unit: {unit.DistinctId()} to team: {__instance.DisplayName}");
        }
    }
}
