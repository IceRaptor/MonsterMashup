using BattleTech.Data;
using CustomComponents;
using IRBTModUtils.Extension;
using MonsterMashup.Component;
using System;
using System.Collections.Generic;

namespace MonsterMashup.Helper
{
    public static class DataLoadHelper
    {
        public static void LoadLinkedResources(CombatGameState combat)
        {
            // Load the necessary turret defs
            Mod.Log.Info?.Write($"== BEGIN load request for all possible ambush spawns");
            LoadRequest asyncSpawnReq = combat.DataManager.CreateLoadRequest(
                delegate (LoadRequest loadRequest) { OnLoadComplete(combat); }, false
                );
            Mod.Log.Info?.Write($" -- Pre-load counts => " +
                $"weaponDefs: {combat.DataManager.WeaponDefs.Count}  " +
                $"pilotDefs: {combat.DataManager.PilotDefs.Count}  mechDefs: {combat.DataManager.MechDefs.Count}  " +
                $"turretDefs: {combat.DataManager.TurretDefs.Count}  vehicleDefs: {combat.DataManager.VehicleDefs.Count}");

            // Filter requests so we don't load multiple times
            HashSet<string> vehiclesToLoad = new HashSet<string>();
            HashSet<string> pilotsToLoad = new HashSet<string>();
            Mod.Log.Info?.Write($"== Checking all actors for linked turrets");
            foreach (AbstractActor actor in combat.AllActors)
            {
                Mod.Log.Info?.Write($" -- Found actor: {actor.DistinctId()}");
                foreach (MechComponent component in actor.allComponents)
                {
                    Mod.Log.Debug?.Write($" --- Component: {component.UIName}");
                    if (component.mechComponentRef.Is<LinkedActorComponent>(out LinkedActorComponent linkedTurret))
                    {
                        Mod.Log.Info?.Write($" ---- linked actor at location: {component.mechComponentRef.MountedLocation}  " +
                            $"attachPoint: {linkedTurret.AttachPoint}  CU vehicle: {linkedTurret.CUVehicleDefId}  pilotDefId: {linkedTurret.PilotDefId}");

                        if (!String.IsNullOrEmpty(linkedTurret.CUVehicleDefId))
                            vehiclesToLoad.Add(linkedTurret.CUVehicleDefId);

                        pilotsToLoad.Add(linkedTurret.PilotDefId);

                        ModState.ComponentsToLink.Add((component, linkedTurret));
                    }
                }
            }
            Mod.Log.Info?.Write($"== DONE");

            if (vehiclesToLoad.Count == 0 || pilotsToLoad.Count == 0)
                return; // Nothing to do

            // Add the filtered requests to the async load
            foreach (string defId in vehiclesToLoad)
            {
                Mod.Log.Info?.Write($"  - CU VehicleDefId: {defId}");
                asyncSpawnReq.AddBlindLoadRequest(BattleTechResourceType.VehicleDef, defId, new bool?(false));
            }
            foreach (string defId in pilotsToLoad)
            {
                Mod.Log.Info?.Write($"  - PilotDefId: {defId}");
                asyncSpawnReq.AddBlindLoadRequest(BattleTechResourceType.PilotDef, defId, new bool?(false));
            }

            // Fire the load request
            asyncSpawnReq.ProcessRequests(1000U);
        }

        private static void OnLoadComplete(CombatGameState combat)
        {
            Mod.Log.Info?.Write($"== END load request for linked components");
            Mod.Log.Info?.Write($" -- Post-load counts => " +
                $"weaponDefs: {combat.DataManager.WeaponDefs.Count}  " +
                $"pilotDefs: {combat.DataManager.PilotDefs.Count}  mechDefs: {combat.DataManager.MechDefs.Count}  " +
                $"CU vehicleDefs: {combat.DataManager.VehicleDefs.Count}");

            SpawnHelper.SpawnAllLinkedActors();
        }

        public static void UnloadAmbushResources(CombatGameState combat)
        {
            // TODO: Looks like data manager has no unload function, just a 'clear' function?
            // Possibly just set defs=true for clear... but would that screw up say salvage?
        }

    }
}
