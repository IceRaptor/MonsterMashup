using BattleTech;
using IRBTModUtils;
using IRBTModUtils.Extension;
using MonsterMashup.Component;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MonsterMashup.Helper
{
    public class SpawnHelper
    {
        public static void SpawnAllLinkedActors()
        {
            try
            {
                Dictionary<string, Lance> lanceDefsByParent = new Dictionary<string, Lance>();
                Mod.Log.Info?.Write($"Iterating {ModState.ComponentsToLink.Count} components to link actors");
                List<AbstractActor> parents = new List<AbstractActor>();
                foreach ((MechComponent sourceComponent, LinkedActorComponent linkedTurret) in ModState.ComponentsToLink)
                {
                    AbstractActor parent = sourceComponent.parent;
                    Mod.Log.Info?.Write($" -- Spawning linkedActors for component: {sourceComponent.Description.UIName} on actor: {parent.DistinctId()}");

                    Mod.Log.Debug?.Write($" -- Looking for attach: '{linkedTurret.AttachPoint}'");
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


                    Mod.Log.Info?.Write($" -- Found attach point: {linkedTurret.AttachPoint}");

                    // Spawned turrets get a new LanceId. This should prevent them messing with the parent's objectives.
                    if (!lanceDefsByParent.TryGetValue(parent.DistinctId(), out Lance linkedLance))
                    {
                        Mod.Log.Info?.Write($"Lance not found for parent: {parent.DistinctId()}, creating new.");
                        linkedLance = CreateLinkedLance(parent.team);
                        lanceDefsByParent.Add(parent.DistinctId(), linkedLance);
                    }

                    SpawnLinkedCUVehicle(parent, linkedLance, sourceComponent, linkedTurret, attachTransform);

                    parents.Add(parent);
                }
            }
            catch (Exception e)
            {
                Mod.Log.Warn?.Write(e, "Error initializing linkedActor.");
            }
        }

        internal static Lance CreateLinkedLance(Team team)
        {
            Lance lance = new Lance(team, new BattleTech.Framework.LanceSpawnerRef[] { });
            Guid g = Guid.NewGuid();
            string lanceGuid = LanceSpawnerGameLogic.GetLanceGuid(g.ToString());
            lance.lanceGuid = lanceGuid;
            Mod.Log.Info?.Write($"Created new linked Lance: {lance.lanceGuid} for team: {team.DisplayName}");
            SharedState.Combat.ItemRegistry.AddItem(lance);
            team.lances.Add(lance);

            return lance;
        }

        // Vehicles are mechs under the covers, so spawn mechs
        internal static void SpawnLinkedCUVehicle(AbstractActor parent, Lance lance, MechComponent sourceComponent,
            LinkedActorComponent linkedTurret, Transform attachTransform)
        {
            PilotDef pilotDef = SharedState.Combat.DataManager.PilotDefs.Get(linkedTurret.PilotDefId);
            MechDef mechDef = SharedState.Combat.DataManager.MechDefs.GetOrCreate(linkedTurret.CUVehicleDefId);
            try
            {
                mechDef.Refresh();

                if (mechDef == null || pilotDef == null) Mod.Log.Error?.Write($"Failed to LOAD VehicleDefId: {linkedTurret.CUVehicleDefId} + pilotDefId: {linkedTurret.PilotDefId} !");

                //Turret turret = ActorFactory.CreateTurret(turretDef, pilotDef, parent.EncounterTags, SharedState.Combat, parent.team.GetNextSupportUnitGuid(), "", null);
                Mech fakeVehicle = ActorFactory.CreateMech(mechDef, pilotDef, new HBS.Collections.TagSet(), SharedState.Combat, parent.team.GetNextSupportUnitGuid(), "", null);
                if (fakeVehicle == null)
                {
                    Mod.Log.Warn?.Write($"Failed to CREATE vehicleDef: {linkedTurret.CUVehicleDefId} + pilotDefId: {linkedTurret.PilotDefId} !");
                    return;
                }

                Mod.Log.Info?.Write($" Attach point is: {attachTransform.position}  rotation: {attachTransform.rotation.eulerAngles}");
                Mod.Log.Info?.Write($" Parent position is: {parent.GameRep.transform.position}  rotation: {parent.GameRep.transform.rotation.eulerAngles}");

                fakeVehicle.Init(attachTransform.position, attachTransform.rotation.eulerAngles.z, true);
                fakeVehicle.InitGameRep(null);
                Mod.Log.Info?.Write($" -- start position: {fakeVehicle.GameRep.transform.position}  rotation: {fakeVehicle.GameRep.transform.rotation.eulerAngles}");

                // Align the turrets to the orientation of the parent transform. This allows us to customize where the turrets will be.
                // We need to align both the visuals (gameRep) and object. The former for display, the latter for LoS calculations
                //Quaternion alignVector = attachTransform.rotation * Quaternion.Euler(90f, 0f, 0f);
                Quaternion alignVector = attachTransform.rotation;
                fakeVehicle.GameRep.transform.rotation = Quaternion.RotateTowards(fakeVehicle.GameRep.transform.rotation, alignVector, 9999f);
                fakeVehicle.CurrentRotation = fakeVehicle.GameRep.transform.rotation;
                Mod.Log.Info?.Write($" -- rotated position: {fakeVehicle.GameRep.transform.position}  rotation: {fakeVehicle.GameRep.transform.rotation.eulerAngles}");

                Mod.Log.Info?.Write($" Spawned mech, adding to team.");
                parent.team.AddUnit(fakeVehicle);
                fakeVehicle.AddToTeam(parent.team);
                fakeVehicle.AddToLance(lance);

                // Mission Control has a patch that expects a team to be present before this call is made. So it must be performed *after* the addTeam/addLance calls
                fakeVehicle.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(SharedState.Combat.BattleTechGame, fakeVehicle, BehaviorTreeIDEnum.CoreAITree);
                Mod.Log.Debug?.Write("Updated behaviorTree");

                // Notify everything else that the turret is active
                UnitSpawnedMessage message = new UnitSpawnedMessage("MONSTER_MASH", fakeVehicle.GUID);
                SharedState.Combat.MessageCenter.PublishMessage(message);

                // Force the turret to be hidden from the player
                fakeVehicle.OnPlayerVisibilityChanged(VisibilityLevel.None);

                // Link units via CU
                ICustomMech custMech = parent as ICustomMech;
                if (custMech != null)
                {
                    Vector3 spawnPosition = attachTransform.position;
                    Vector3 relativePosition = attachTransform.position - parent.CurrentPosition;
                    Mod.Log.Info?.Write($"Parent currPos: {parent.CurrentPosition} attachPos: {attachTransform.position}  delta: {relativePosition}");

                    Mod.Log.Info?.Write($"CU Linking fakeVehicle: {fakeVehicle.DistinctId()} to parent: {parent.DistinctId()}");
                    custMech.AddLinkedActor(fakeVehicle, relativePosition, false);

                    if (parent.IsTeleportedOffScreen)
                    {
                        Mod.Log.Info?.Write($" parent is offscreen. Need to spawn offscreen too");
                        spawnPosition = SharedState.Combat.LocalPlayerTeam.OffScreenPosition;
                    }
                }

                // Write linking stats for component and turret
                Mod.Log.Info?.Write($" Bi-directionally linking mech: {fakeVehicle.uid} to component: {sourceComponent.parent.uid}:{sourceComponent.uid}");
                sourceComponent.StatCollection.AddStatistic<string>(ModStats.Linked_Child_UID, fakeVehicle.uid, null);
                sourceComponent.parent.StatCollection.AddStatistic<string>(ModStats.Linked_Child_UID, fakeVehicle.uid, null);

                fakeVehicle.StatCollection.AddStatistic<string>(ModStats.Linked_Parent_Actor_UID, sourceComponent.parent.uid, null);
                fakeVehicle.StatCollection.AddStatistic<string>(ModStats.Linked_Parent_MechComp_UID, sourceComponent.uid, null);

                LinkedEntityHelper.ProcessWeapons(fakeVehicle);

                // Populate the modstate
                ModState.Parents.Add(parent);
                ModState.LinkedActors.Add(fakeVehicle.DistinctId(), parent);
                ModState.AttachTransforms.Add(fakeVehicle.DistinctId(), attachTransform);

                Mod.Log.Info?.Write($"Spawned vehicleDefId: {linkedTurret.CUVehicleDefId} + pilotDefId: {linkedTurret.PilotDefId} at attach: {attachTransform.position}");
            }
            catch (Exception e)
            {
                Mod.Log.Error?.Write(e, $"Failed to SPAWN vehicleDefId: {linkedTurret.CUVehicleDefId} + pilotDefId: {linkedTurret.PilotDefId} !");
                Mod.Log.Error?.Write("InnerEx: " + e.InnerException.StackTrace);
            }
        }
       
    }
}
