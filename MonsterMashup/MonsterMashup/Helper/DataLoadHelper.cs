using BattleTech;
using BattleTech.Data;
using CustomComponents;
using IRBTModUtils;
using IRBTModUtils.Extension;
using MonsterMashup.Component;
using System;
using System.Collections.Generic;
using UnityEngine;

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
            HashSet<string> turretsToLoad = new HashSet<string>();
            HashSet<string> pilotsToLoad = new HashSet<string>();
            Mod.Log.Info?.Write($"== Checking all actors for linked turrets");
            foreach (AbstractActor actor in combat.AllActors)
            {
                Mod.Log.Info?.Write($" -- Found actor: {actor.DistinctId()}");
                foreach (MechComponent component in actor.allComponents)
                {
                    Mod.Log.Debug?.Write($" --- Component: {component.UIName}");
                    if (component.mechComponentRef.Is<LinkedTurretComponent>(out LinkedTurretComponent linkedTurret))
                    {
                        Mod.Log.Info?.Write($" ---- linked turret at location: {component.mechComponentRef.MountedLocation}  " +
                            $"attachPoint: {linkedTurret.AttachPoint}  turret: {linkedTurret.TurretDefId}  pilotDefId: {linkedTurret.PilotDefId}");
                        turretsToLoad.Add(linkedTurret.TurretDefId);
                        pilotsToLoad.Add(linkedTurret.PilotDefId);

                        ModState.ComponentsToLink.Add((component, linkedTurret));
                    }
                }
            }
            Mod.Log.Info?.Write($"== DONE");

            if (turretsToLoad.Count == 0 && pilotsToLoad.Count == 0)
                return; // Nothing to do

            // Add the filtered requests to the async load
            foreach (string defId in turretsToLoad)
            {
                Mod.Log.Info?.Write($"  - TurretDefId: {defId}");
                asyncSpawnReq.AddBlindLoadRequest(BattleTechResourceType.TurretDef, defId, new bool?(false));
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
                $"turretDefs: {combat.DataManager.TurretDefs.Count}  vehicleDefs: {combat.DataManager.VehicleDefs.Count}");

            try
            {
                Mod.Log.Info?.Write($"Iterating {ModState.ComponentsToLink.Count} components to link turrets");
                foreach ((MechComponent sourceComponent, LinkedTurretComponent linkedTurret) in ModState.ComponentsToLink)
                {
                    AbstractActor parent = sourceComponent.parent;
                    Mod.Log.Info?.Write($" -- Spawning turrets for component: {sourceComponent.Description.UIName} on actor: {parent.DistinctId()}");

                    Mod.Log.Info?.Write($" -- Looking for turretDef: {linkedTurret.TurretDefId} at attach: '{linkedTurret.AttachPoint}'");

                    if (parent == null || parent.GameRep == null || parent.GameRep.transform == null)
                    {
                        Mod.Log.Warn?.Write("Failed to find parent's transform!");
                        continue;
                    }

                    Transform[] childTransforms = parent.GameRep.GetComponentsInChildren<Transform>();
                    Transform attachTransform = null;
                    Mod.Log.Trace?.Write($" == Iterating transforms");
                    foreach (Transform childTF in childTransforms)
                    {
                        Mod.Log.Trace?.Write($" ==== Found transform: {childTF.name}");
                        if (childTF.name.Equals(linkedTurret.AttachPoint, StringComparison.InvariantCultureIgnoreCase))
                        {
                            Mod.Log.Trace?.Write($" ==== Found target transform: {childTF.name}");
                            attachTransform = childTF;
                        }
                    }

                    if (attachTransform == null)
                    {
                        Mod.Log.Warn?.Write($"Failed to find attach point, skipping!");
                        continue;
                    }
                    Mod.Log.Info?.Write($" Found attach point: {linkedTurret.AttachPoint}");

                    PilotDef pilotDef = SharedState.Combat.DataManager.PilotDefs.Get(linkedTurret.PilotDefId);
                    TurretDef turretDef = SharedState.Combat.DataManager.TurretDefs.GetOrCreate(linkedTurret.TurretDefId);
                    try
                    {
                        turretDef.Refresh();

                        if (turretDef == null || pilotDef == null) Mod.Log.Error?.Write($"Failed to LOAD turretDefId: {linkedTurret.TurretDefId} + pilotDefId: {linkedTurret.PilotDefId} !");

                        Turret turret = ActorFactory.CreateTurret(turretDef, pilotDef, parent.EncounterTags, SharedState.Combat, parent.team.GetNextSupportUnitGuid(), "", null);
                        if (turret == null)
                        {
                            Mod.Log.Warn?.Write($"Failed to SPAWN turretDefId: {linkedTurret.TurretDefId} + pilotDefId: {linkedTurret.PilotDefId} !");
                            continue;
                        }

                        Mod.Log.Info?.Write($" Attach point is: {attachTransform.position}  rotation: {attachTransform.rotation.eulerAngles}");
                        Mod.Log.Info?.Write($" Parent position is: {parent.GameRep.transform.position}  rotation: {parent.GameRep.transform.rotation.eulerAngles}");

                        
                        //turret.Init(attachTransform.position, alignVector.y, true);
                        //turret.Init(attachTransform.position, attachTransform.rotation.eulerAngles.y, true);
                        turret.Init(attachTransform.position, attachTransform.rotation.eulerAngles.z, true);
                        turret.InitGameRep(null);
                        Mod.Log.Info?.Write($" Turret start position: {turret.GameRep.transform.position}  rotation: {turret.GameRep.transform.rotation.eulerAngles}");
                        //turret.InitGameRep(attachTransform);
                        //turret.GameRep.transform.LookAt(attachTransform.position + attachTransform.forward, Vector3.up);

                        //turret.GameRep.transform.rotation = Quaternion.FromToRotation(Vector3.up, attachTransform.forward);

                        // THIS PUTS THEM ALL VERTICAL BUT ALIGNED PROPERLY
                        //turret.GameRep.transform.rotation = Quaternion.RotateTowards(turret.GameRep.transform.rotation, attachTransform.rotation, 9999f);                        

                        Quaternion alignVector = attachTransform.rotation * Quaternion.Euler(90f, 0f, 0f);
                        turret.GameRep.transform.rotation = Quaternion.RotateTowards(turret.GameRep.transform.rotation, alignVector, 9999f);
                        Mod.Log.Info?.Write($" Turret rotated position: {turret.GameRep.transform.position}  rotation: {turret.GameRep.transform.rotation.eulerAngles}");

                        //Quaternion zeroRot = Quaternion.Euler(0f, 0f, 0f);
                        //Vector3 alignVector = attachTransform.position + attachTransform.forward;
                        //turret.GameRep.transform.rotation = Quaternion.RotateTowards(turret.GameRep.transform.rotation, alignVector, 180f);
                        //turret.GameRep.transform.eulerAngles = alignVector;

                        // Update rotation to match that attach point's rotation
                        //Vector3 alignVector = attachTransform.position + attachTransform.forward;
                        //Mod.Log.Info?.Write($" Aligning turret rotation to: {alignVector}");
                        //turret.GameRep.transform.LookAt(attachTransform.position + attachTransform.forward, Vector3.zero);
                        //custRep.UpdateRotation(custRep.transform, custRep.transform.forward, 9999f);
                        //moveTransform.LookAt(moveTransform.position + forward, Vector3.up);

                        turret.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(SharedState.Combat.BattleTechGame, turret, BehaviorTreeIDEnum.CoreAITree);
                        Mod.Log.Debug?.Write("Updated turret behaviorTree");

                        Mod.Log.Info?.Write($" Spawned turret, adding to team.");
                        parent.team.AddUnit(turret);
                        turret.AddToTeam(parent.team);
                        turret.AddToLance(parent.lance);

                        // Notify everything else that the turret is active
                        UnitSpawnedMessage message = new UnitSpawnedMessage("MONSTER_MASH", turret.GUID);
                        SharedState.Combat.MessageCenter.PublishMessage(message);

                        // Finally force the turret to be fully visible
                        turret.OnPlayerVisibilityChanged(VisibilityLevel.LOSFull);
                        Mod.Log.Info?.Write($" Turret should be player visible.");

                        // Finally write linking stats for component and turret
                        Mod.Log.Info?.Write($" Bi-directionally linking turret: {turret.uid} to component: {sourceComponent.parent.uid}:{sourceComponent.uid}");
                        sourceComponent.StatCollection.AddStatistic<string>(ModStats.Linked_Child_UID, turret.uid, null);
                        turret.StatCollection.AddStatistic<string>(ModStats.Linked_Parent_Actor_UID, sourceComponent.parent.uid, null);
                        turret.StatCollection.AddStatistic<string>(ModStats.Linked_Parent_MechComp_UID, sourceComponent.uid, null);
                    }
                    catch (Exception e)
                    {
                        Mod.Log.Error?.Write(e, $"Failed to SPAWN turretDefId: {linkedTurret.TurretDefId} + pilotDefId: {linkedTurret.PilotDefId} !");
                    }

                }
            }
            catch (Exception e)
            {
                Mod.Log.Warn?.Write(e, "Error initializing turret.");
            }
        }

        public static void UnloadAmbushResources(CombatGameState combat)
        {
            // TODO: Looks like data manager has no unload function, just a 'clear' function?
            // Possibly just set defs=true for clear... but would that screw up say salvage?
        }

    }
}
