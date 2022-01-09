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
        public static void SpawnAllLinkedTurrets()
        {
            try
            {
                Dictionary<string, Lance> lanceDefsByParent = new Dictionary<string, Lance>();
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
                    
                    // Spawned turrets get a new LanceId. This should prevent them messing with the parent's objectives.
                    if (!lanceDefsByParent.TryGetValue(parent.DistinctId(), out Lance linkedLance))
                    {
                        Mod.Log.Info?.Write($"Lance not found for parent: {parent.DistinctId()}, creating new.");
                        linkedLance = CreateLinkedLance(parent.team);
                        lanceDefsByParent.Add(parent.DistinctId(), linkedLance);
                    }

                    SpawnLinkedTurret(parent, linkedLance, sourceComponent, linkedTurret, attachTransform);
                }
            }
            catch (Exception e)
            {
                Mod.Log.Warn?.Write(e, "Error initializing turret.");
            }
        }

        internal static Lance CreateLinkedLance(Team team)
        {
            Lance lance = new Lance(team, new BattleTech.Framework.LanceSpawnerRef[] { });
            Guid g = Guid.NewGuid();
            string lanceGuid = LanceSpawnerGameLogic.GetLanceGuid(g.ToString());
            lance.lanceGuid = lanceGuid;
            SharedState.Combat.ItemRegistry.AddItem(lance);
            team.lances.Add(lance);

            return lance;
        }

        internal  static void SpawnLinkedTurret(AbstractActor parent, Lance lance, MechComponent sourceComponent, 
            LinkedTurretComponent linkedTurret, Transform attachTransform)
        {
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
                    return;
                }

                Mod.Log.Info?.Write($" Attach point is: {attachTransform.position}  rotation: {attachTransform.rotation.eulerAngles}");
                Mod.Log.Info?.Write($" Parent position is: {parent.GameRep.transform.position}  rotation: {parent.GameRep.transform.rotation.eulerAngles}");

                turret.Init(attachTransform.position, attachTransform.rotation.eulerAngles.z, true);
                turret.InitGameRep(null);
                Mod.Log.Info?.Write($" Turret start position: {turret.GameRep.transform.position}  rotation: {turret.GameRep.transform.rotation.eulerAngles}");

                // Align the turrets to the orientation of the parent transform. This allows us to customize where the turrets will be.
                Quaternion alignVector = attachTransform.rotation * Quaternion.Euler(90f, 0f, 0f);
                turret.GameRep.transform.rotation = Quaternion.RotateTowards(turret.GameRep.transform.rotation, alignVector, 9999f);
                Mod.Log.Info?.Write($" Turret rotated position: {turret.GameRep.transform.position}  rotation: {turret.GameRep.transform.rotation.eulerAngles}");

                turret.BehaviorTree = BehaviorTreeFactory.MakeBehaviorTree(SharedState.Combat.BattleTechGame, turret, BehaviorTreeIDEnum.CoreAITree);
                Mod.Log.Debug?.Write("Updated turret behaviorTree");

                Mod.Log.Info?.Write($" Spawned turret, adding to team.");
                parent.team.AddUnit(turret);
                turret.AddToTeam(parent.team);
                turret.AddToLance(lance);

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
}
