using BattleTech;
using Harmony;
using HBS.Collections;
using IRBTModUtils.Extension;
using UnityEngine;

namespace MonsterMashup.Patch
{
    // Look for CU style units. 
    [HarmonyPatch(typeof(UnitSpawnPointGameLogic), "SpawnMech")]
    [HarmonyAfter("io.mission.customunits")]
    static class UnitSpawnPointGameLogic_SpawnMech
    {
        static void Postfix(UnitSpawnPointGameLogic __instance, Mech __result, 
            MechDef mDef, PilotDef pilot, Team team, Lance lance, HeraldryDef customHeraldryDef)
        {
            Mod.Log.Info?.Write($"USPGL:SpawnMech INVOKED");
            if (__result != null)
            {
                Mod.Log.Info?.Write($"USPGL:SpawnMech for actor: {__result.DistinctId()} ");

                TagSet staticTags = __result.GetStaticUnitTags();
                if (staticTags != null && staticTags.Contains(ModTags.NoGroundAlign))
                {
                    Mod.Log.Info?.Write($"actor: {__result.DistinctId()} has no align tag, resetting alignment");
                    __result.GameRep.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

                    Quaternion zeroRot = Quaternion.Euler(0f, 0f, 0f);
                    __result.GameRep.transform.rotation = Quaternion.RotateTowards(__result.GameRep.transform.rotation, zeroRot, 180f);
                    // Find and update j_root as well
                    Transform[] childTransforms = __result.GameRep.GetComponentsInChildren<Transform>();
                    foreach (Transform childT in childTransforms)
                    {
                        if (childT.name.Equals("j_Root", System.StringComparison.InvariantCulture))
                        {
                            childT.rotation = zeroRot;
                        }
                    }                    
                }
            }
        }
    }
}
