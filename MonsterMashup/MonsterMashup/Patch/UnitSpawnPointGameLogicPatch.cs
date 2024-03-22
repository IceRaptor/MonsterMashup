using CustomComponents;
using HBS.Collections;
using IRBTModUtils.Extension;
using MonsterMashup.Component;
using MonsterMashup.Helper;
using MonsterMashup.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MonsterMashup.Patch
{

    // Check for units with MM components bound to them
    [HarmonyPatch(typeof(UnitSpawnPointGameLogic), "initializeActor")]
    static class UnitSpawnPointGameLogic_initializeActor
    {
        static void Postfix(UnitSpawnPointGameLogic __instance, AbstractActor actor, Team team, Lance lance)
        {
            Mod.Log.Info?.Write($"UnitSpawnPointGameLogic::initializeActor for actor: {actor.DistinctId()}");

            if (actor is Mech mech)
            {
                LinkedEntityHelper.ProcessWeapons(mech);
                ProcessComponents(mech);
            }
        }

        private static void ProcessComponents(Mech mech)
        {
            Mod.Log.Info?.Write($"Checking for MM components on unit: {mech.DistinctId()}");

            // Crush components
            if (mech.MechDef.Chassis.Is(out CrushOnCollisionComponent crushComponent))
            {
                Mod.Log.Debug?.Write($"  - actor has CrushOnCollisionComponent");
                mech.StatCollection.AddStatistic<bool>(ModStats.Crush_On_Move, true);

                if (!string.IsNullOrEmpty(crushComponent.RadiusTransform))
                {
                    Mod.Log.Debug?.Write($"Trying to find crush radius transform: {crushComponent.RadiusTransform}");
                    Transform[] childTransforms = mech.GameRep.GetComponentsInChildren<Transform>(true);
                    foreach (Transform childTF in childTransforms)
                    {
                        Mod.Log.Trace?.Write($"  -- Found transform: {childTF.name}");
                        if (childTF.name.Equals(crushComponent.RadiusTransform, StringComparison.InvariantCultureIgnoreCase))
                        {
                            float newScale = crushComponent.Radius * 2.0f + 20f; // The asset needs 20 added to it to match the radius, not sure why.
                            Mod.Log.Debug?.Write($" -- Setting crush radius transform: {childTF.name} to scale: {newScale}");
                            childTF.localScale = new Vector3(newScale, newScale, newScale);
                            childTF.gameObject.SetActive(true);
                        }
                    }
                }
            }
            else
            {
                Mod.Log.Debug?.Write($"  - actor had no CrushOnCollisionComponent");
            }

            // Prevent melee
            if (mech.MechDef.Chassis.Is(out PreventMeleeComponent preventMeleeComponent))
            {
                Mod.Log.Debug?.Write($"  - actor has PreventMeleeComponent, marking it as invalid melee target");
                mech.StatCollection.AddStatistic<bool>(ModStats.Prevent_Melee, true);
            }
            else
            {
                Mod.Log.Debug?.Write($"  - actor had no PreventMeleeComponent");
            }

            // Timed Spawn components
            if (mech.MechDef.Chassis.Is<CombatSpawnComponent>(out CombatSpawnComponent timedSpawnComponent))
            {
                Mod.Log.Debug?.Write($"Actor: {mech.DistinctId()} has spawn configs, creating new spawnstates");
                foreach (SpawnConfig spawnConfig in timedSpawnComponent.Spawns)
                {
                    SupportSpawnState sss = new SupportSpawnState();
                    sss.Init(mech, spawnConfig);
                    Mod.Log.Debug?.Write($"Spawn state initialized for: {spawnConfig.CUVehicleDefId}_{spawnConfig.PilotDefId}");

                    bool hasKey = ModState.ChildSpawns.TryGetValue(mech.DistinctId(), out List<SupportSpawnState> supportSpawns);
                    if (!hasKey)
                    {
                        supportSpawns = new List<SupportSpawnState>
                        {
                            sss
                        };
                        ModState.ChildSpawns.Add(mech.DistinctId(), supportSpawns);
                    }
                    else
                    {
                        supportSpawns.Add(sss);
                    }
                }
            }
            else
            {
                Mod.Log.Debug?.Write($"  - actor had no TimedSpawnComponent");
            }
        }
    }

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
                    // Reset alignment for the unit
                    Mod.Log.Info?.Write($"actor: {__result.DistinctId()} has no align tag, resetting alignment");
                    __result.GameRep.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

                    Quaternion zeroRot = Quaternion.Euler(0f, 0f, 0f);
                    __result.GameRep.transform.rotation = Quaternion.RotateTowards(__result.GameRep.transform.rotation, zeroRot, 180f);

                    // Find and update j_root as well; this will prevent the unit from 'falling over'
                    Transform[] childTransforms = __result.GameRep.GetComponentsInChildren<Transform>();
                    foreach (Transform childT in childTransforms)
                    {
                        if (childT.name.Equals("j_Root", System.StringComparison.InvariantCulture))
                        {
                            childT.rotation = zeroRot;
                        }
                    }
                }

                // Create a new footprint visualization for the unit, to show what ground it covers
                if (Mod.Config.DeveloperOptions.EnableFootprintVis)
                {
                    FootprintVisualization footprintVis = new FootprintVisualization(__result);
                    ModState.FootprintVisuals.Add(__result.DistinctId(), footprintVis);
                }
            }
        }
    }
}
