using BattleTech;
using IRBTModUtils;
using IRBTModUtils.Extension;
using MonsterMashup.AI;
using MonsterMashup.Component;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Localize.Text;

namespace MonsterMashup.Helper
{

    public class ParentRelationships
    {
        AbstractActor parent;

        private List<AbstractActor> linkedActors = new List<AbstractActor>();
        private Dictionary<AbstractActor, Transform> linkedActorTransforms = new Dictionary<AbstractActor, Transform> { };
        private Lance linkedActorsLance;

        private List<SupportSpawnConfig> supportSpawnConfigs = new List<SupportSpawnConfig>();
        private List<AbstractActor> spawnedSupportActors = new List<AbstractActor>();
        private Lance supportSpawnLance;

        public List<AbstractActor> LinkedActors { get => linkedActors; set => linkedActors = value; }
        public Dictionary<AbstractActor, Transform> LinkedActorTransforms { get => linkedActorTransforms; set => linkedActorTransforms = value; }
        public Lance LinkedActorsLance { get => linkedActorsLance; set => linkedActorsLance = value; }

        public List<SupportSpawnConfig> SupportSpawnConfigs { get => supportSpawnConfigs; set => supportSpawnConfigs = value; }
        public List<AbstractActor> SpawnedSupportActors { get => spawnedSupportActors; set => spawnedSupportActors = value; }
        public Lance SupportSpawnLance { get => supportSpawnLance; set => supportSpawnLance = value; }

        public ParentRelationships(AbstractActor parent)
        {
            this.parent = parent;
            LinkedActorsLance = CreateLinkedLance(parent.team, parent.lance);
            SupportSpawnLance = CreateLinkedLance(parent.team, parent.lance);
        }

        internal static Lance CreateLinkedLance(Team parentTeam, Lance parentLance)
        {
            Lance lance = new Lance(parentTeam, new BattleTech.Framework.LanceSpawnerRef[] { });
            Guid g = Guid.NewGuid();
            string lanceGuid = LanceSpawnerGameLogic.GetLanceGuid(g.ToString());
            lance.lanceGuid = lanceGuid;
            Mod.Log.Info?.Write($"Created new linked Lance: {lance.lanceGuid} for team: {parentTeam.DisplayName}");
            SharedState.Combat.ItemRegistry.AddItem(lance);
            parentTeam.lances.Add(lance);

            if (parentLance.IsAlerted)
            {
                lance.BehaviorVariables.SetVariable(BehaviorVariableName.Bool_Alerted, new BehaviorVariableValue(b: true));
            }

            return lance;
        }
    }

    public class SpawnHelper
    {
        public static void SpawnAllLinkedActors()
        {
            try
            {
                Mod.Log.Info?.Write($"Iterating {ModState.ComponentsToLink.Count} components to link actors");
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
                    bool parentStateFound = ModState.ParentState.TryGetValue(parent, out ParentRelationships parentRelationships);
                    if (!parentStateFound)
                    {
                        Mod.Log.Info?.Write($"Relationships not initialized for parent: {parent.DistinctId()}, initing");
                        parentRelationships = new ParentRelationships(parent);
                        ModState.ParentState.Add(parent, parentRelationships);
                    }

                    SpawnLinkedCUVehicle(parent, parentRelationships, sourceComponent, linkedTurret, attachTransform);
                }
            }
            catch (Exception e)
            {
                Mod.Log.Warn?.Write(e, "Error initializing linkedActor.");
            }
        }

        // Vehicles are mechs under the covers, so spawn mechs
        internal static void SpawnLinkedCUVehicle(AbstractActor parent, ParentRelationships parentRelationships, 
            MechComponent sourceComponent, LinkedActorComponent linkedTurret, Transform attachTransform)
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

                Mod.Log.Info?.Write($" Spawned mech, adding to team: {parent.team} and lance: {parentRelationships.LinkedActorsLance}");
                parent.team.AddUnit(fakeVehicle);
                fakeVehicle.AddToTeam(parent.team);
                fakeVehicle.AddToLance(parentRelationships.LinkedActorsLance);

                // Mission Control has a patch that expects a team to be present before this call is made. So it must be performed *after* the addTeam/addLance calls
                fakeVehicle.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(SharedState.Combat.BattleTechGame, fakeVehicle, BehaviorTreeIDEnum.CoreAITree);
                fakeVehicle.BehaviorTree.RootNode = MM_AI_ImmobileTurret.InitRootNode(fakeVehicle.BehaviorTree, fakeVehicle, fakeVehicle.BehaviorTree.battleTechGame);
                Mod.Log.Debug?.Write("Updated behaviorTree");

                // Notify everything else that the turret is active
                UnitSpawnedMessage message = new UnitSpawnedMessage("MONSTER_MASH", fakeVehicle.GUID);
                SharedState.Combat.MessageCenter.PublishMessage(message);

                // Force the turret to be hidden from the player
                fakeVehicle.OnPlayerVisibilityChanged(VisibilityLevel.None);

                // Add the unit to the initiative tracker
                SharedState.CombatHUD.PhaseTrack.AddIconTracker(fakeVehicle);

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
                parentRelationships.LinkedActors.Add(fakeVehicle);
                parentRelationships.LinkedActorTransforms.Add(fakeVehicle, attachTransform);
                ModState.Parents.Add(parent);
                SharedState.Combat.ItemRegistry.AddItem(fakeVehicle);

                Mod.Log.Info?.Write($"Spawned vehicleDefId: {linkedTurret.CUVehicleDefId} + pilotDefId: {linkedTurret.PilotDefId} at attach: {attachTransform.position}");
            }
            catch (Exception e)
            {
                Mod.Log.Error?.Write(e, $"Failed to SPAWN vehicleDefId: {linkedTurret.CUVehicleDefId} + pilotDefId: {linkedTurret.PilotDefId} !");
                Mod.Log.Error?.Write("InnerEx: " + e.InnerException.StackTrace);
            }
        }
    }

    public class SupportSpawnConfig
    {
        public AbstractActor Parent;
        public SpawnConfig Config;
        public Lance Lance;
        public Transform SpawnTransform;
        private int SpawnCount = 0;
        private int LastSpawnedRound = 0;
        
        private bool IsReady = false;

        public void Init(AbstractActor parent, SpawnConfig config, Lance lance)
        {
            this.Parent = parent;
            this.Config = config;

            // Find the transform in the parent
            Transform[] childTransforms = this.Parent.GameRep.GetComponentsInChildren<Transform>();
            Mod.Log.Trace?.Write($" == Iterating transforms");
            foreach (Transform childTF in childTransforms)
            {
                Mod.Log.Trace?.Write($" ==== Found transform: {childTF.name}");
                if (childTF.name.Equals(this.Config.AttachPoint, StringComparison.InvariantCultureIgnoreCase))
                {
                    Mod.Log.Trace?.Write($" ==== Found target transform: {childTF.name}");
                    this.SpawnTransform = childTF;
                }
            }

            if (this.SpawnTransform == null)
            {
                Mod.Log.Warn?.Write($"Failed to find attach point: {this.SpawnTransform}, skipping!");
                return;
            }

            this.Lance = lance;

            IsReady = true;
        }

        public AbstractActor TrySpawn()
        {
            Mod.Log.Info?.Write($"Trying spawn for parent: {this.Parent.DistinctId()} at point: {this.Config.AttachPoint}");

            Mod.Log.Debug?.Write($"isReady: {this.IsReady}" +
                $"  parent.isDead: {this.Parent.IsDead}  parent.isFlaggedForDeath: {this.Parent.IsFlaggedForDeath}\n" +
                $"  isInterleaved: {SharedState.Combat.TurnDirector.IsInterleaved}\n" +
                $"  spawnCount: {this.SpawnCount}  maxSpawns: {this.Config.MaxSpawns}");

            if (!this.IsReady) return null;
            if (this.Parent.IsDead || this.Parent.IsFlaggedForDeath) return null; // nothing to do
            if (this.Config.MaxSpawns == 0) return null; // nothing to do
            if (this.SpawnCount >= this.Config.MaxSpawns) return null; // already spawned everyone
            if (!SharedState.Combat.TurnDirector.IsInterleaved) return null; // not in combat

            // Check delay since last spawn turn
            int nextSpawnRound = this.LastSpawnedRound + this.Config.RoundsBetweenSpawns;

            Mod.Log.Debug?.Write($"Current round: {this.Parent.Combat.TurnDirector.CurrentRound}  nextSpawnRound: {nextSpawnRound}");
            if (this.Parent.Combat.TurnDirector.CurrentRound >= nextSpawnRound)
            {
                Mod.Log.Debug?.Write($"Spawning unit: {this.Config.CUVehicleDefId}_{this.Config.PilotDefId} on round: {this.Parent.Combat.TurnDirector.CurrentRound}");
                this.LastSpawnedRound = this.Parent.Combat.TurnDirector.CurrentRound;
                this.SpawnCount++;
                return SpawnSupportUnit();
            }

            return null;
        }

        internal AbstractActor SpawnSupportUnit()
        {
            AbstractActor spawnedUnit = null;

            Mod.Log.Debug?.Write($"Fetching VehicleDefId: {this.Config.CUVehicleDefId} + pilotDefId: {this.Config.PilotDefId}");
            Mod.Log.Trace?.Write($"isNull: DataManager: {SharedState.Combat.DataManager == null}  " +
                $"PilotDefs: {SharedState.Combat.DataManager.PilotDefs}  " +
                $"MechDefs: {SharedState.Combat.DataManager.MechDefs}");
            PilotDef pilotDef = SharedState.Combat.DataManager.PilotDefs.Get(this.Config.PilotDefId);
            MechDef mechDef = SharedState.Combat.DataManager.MechDefs.GetOrCreate(this.Config.CUVehicleDefId);
            try
            {
                if (mechDef == null || pilotDef == null) Mod.Log.Error?.Write($"Failed to LOAD VehicleDefId: {this.Config.CUVehicleDefId} + pilotDefId: {this.Config.PilotDefId} !");

                mechDef.Refresh();

                //Turret turret = ActorFactory.CreateTurret(turretDef, pilotDef, parent.EncounterTags, SharedState.Combat, parent.team.GetNextSupportUnitGuid(), "", null);
                Mech fakeVehicle = ActorFactory.CreateMech(mechDef, pilotDef, new HBS.Collections.TagSet(), SharedState.Combat, 
                    this.Parent.team.GetNextSupportUnitGuid(), "", null);
                if (fakeVehicle == null)
                {
                    Mod.Log.Warn?.Write($"Failed to CREATE vehicleDef: {this.Config.CUVehicleDefId} + pilotDefId: {this.Config.PilotDefId} !");
                    return null;
                }

                // Find the closest hex grid point
                Vector3 hexPosition = SharedState.Combat.HexGrid.GetClosestPointOnGrid(this.SpawnTransform.position);
                hexPosition.y = SharedState.Combat.MapMetaData.GetCellAt(hexPosition).cachedHeight;

                Mod.Log.Info?.Write($" Spawn point is: {this.SpawnTransform.position}  rotation: {this.SpawnTransform.rotation.eulerAngles}");
                Mod.Log.Info?.Write($" hexPosition is: {hexPosition}");

                fakeVehicle.Init(hexPosition, this.SpawnTransform.rotation.eulerAngles.z, true);
                fakeVehicle.InitGameRep(null);
                Mod.Log.Info?.Write($" -- start position: {hexPosition}  rotation: {this.SpawnTransform.rotation.eulerAngles.z}");

                // Align the turrets to the orientation of the parent transform. This allows us to customize where the turrets will be.
                // We need to align both the visuals (gameRep) and object. The former for display, the latter for LoS calculations
                //Quaternion alignVector = attachTransform.rotation * Quaternion.Euler(90f, 0f, 0f);
                //fakeVehicle.GameRep.transform.rotation = Quaternion.LookRotation(hexPosition, Vector3.up);
                UnitHelper.AlignVehicleToGround(fakeVehicle.GameRep.transform, 1);
                fakeVehicle.CurrentRotation = fakeVehicle.GameRep.transform.rotation;

                Mod.Log.Info?.Write($" -- rotated position: {fakeVehicle.GameRep.transform.position}  rotation: {fakeVehicle.GameRep.transform.rotation.eulerAngles}");

                Mod.Log.Info?.Write($" Spawned mech, adding to team: {this.Parent.team} and lance: {this.Lance}.");
                this.Parent.team.AddUnit(fakeVehicle);
                fakeVehicle.AddToTeam(this.Parent.team);
                fakeVehicle.AddToLance(this.Lance);

                fakeVehicle.MechDef.UnitRole = UnitRole.Brawler;
                fakeVehicle.DynamicUnitRole = UnitRole.Brawler;

                // Mission Control has a patch that expects a team to be present before this call is made. So it must be performed *after* the addTeam/addLance calls
                fakeVehicle.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(SharedState.Combat.BattleTechGame, fakeVehicle, BehaviorTreeIDEnum.CoreAITree);
                //fakeVehicle.BehaviorTree.RootNode = MM_AI_ImmobileTurret.InitRootNode(fakeVehicle.BehaviorTree, fakeVehicle, fakeVehicle.BehaviorTree.battleTechGame);
                Mod.Log.Debug?.Write("Updated behaviorTree");
                
                SharedState.Combat.ItemRegistry.AddItem(fakeVehicle);
                
                // Notify everything else that the turret is active
                UnitSpawnedMessage message = new UnitSpawnedMessage("MONSTER_MASH", fakeVehicle.GUID);
                SharedState.Combat.MessageCenter.PublishMessage(message);

                // Force the turret to be hidden from the player
                fakeVehicle.OnPlayerVisibilityChanged(VisibilityLevel.LOSFull);

                // Add the unit to the initiative tracker
                SharedState.CombatHUD.PhaseTrack.AddIconTracker(fakeVehicle);

                fakeVehicle.AlertLance();

                spawnedUnit = fakeVehicle;
            }
            catch (Exception e)
            {
                Mod.Log.Error?.Write(e, $"Failed to SPAWN vehicleDefId: {this.Config.CUVehicleDefId} + pilotDefId: {this.Config.PilotDefId} !");
                Mod.Log.Error?.Write("InnerEx: " + e.InnerException.StackTrace);
            }

            return spawnedUnit;
        }
    }
   
}
