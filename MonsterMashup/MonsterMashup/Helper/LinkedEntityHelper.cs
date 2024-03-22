using CustomComponents;
using IRBTModUtils;
using IRBTModUtils.Extension;
using MonsterMashup.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MonsterMashup.Helper
{
    public static class LinkedEntityHelper
    {
        public static void DestroyLinksInLocation(Mech mech, ChassisLocations location)
        {
            Mod.Log.Info?.Write($"Scanning location: {location} on actor: {mech.DistinctId()} for linked actors.");
            List<MechComponent> components = mech.GetComponentsForLocation(location, ComponentType.Upgrade);
            foreach (MechComponent comp in components)
            {
                Mod.Log.Debug?.Write($" -- mechComponent: {comp.UIName}");
                if (comp.mechComponentRef.Is<LinkedActorComponent>())
                {
                    KillLinkedTurret(comp);
                }
            }
        }

        public static void KillLinkedTurret(MechComponent component)
        {
            if (component == null || component.mechComponentRef == null) return;

            string turretUID = component.StatCollection.GetValue<string>(ModStats.Linked_Child_UID);
            if (turretUID == null) return; // nothing to do

            try
            {
                Mod.Log.Debug?.Write($"MechComponent: {component.UIName} has linked turret, looking up actor by uid: {turretUID}");
                AbstractActor turretActor = SharedState.Combat.FindActorByGUID(turretUID);

                if (turretActor != null)
                {

                    if (turretActor.IsDead || turretActor.IsFlaggedForDeath)
                    {
                        Mod.Log.Info?.Write($"Turret: {turretActor.DistinctId()} already marked for death, skipping");
                    }
                    else
                    {
                        turretActor.GetPilot()?.KillPilot(SharedState.Combat.Constants, "", 0, DamageType.ComponentExplosion, null, null);
                        turretActor.FlagForDeath("Linked Component Destroyed", DeathMethod.ComponentExplosion, DamageType.ComponentExplosion, -1, -1, "", isSilent: false);
                        turretActor.HandleDeath("0");
                        Mod.Log.Info?.Write($"Turret: {turretActor.DistinctId()} successfully destroyed");
                    }
                }
                else
                {
                    Mod.Log.Warn?.Write($"Failed to find linked turret with uid: {turretUID}, cannot kill linked turret!");
                }
            }
            catch (Exception e)
            {
                Mod.Log.Warn?.Write(e, "Failed to kill linked turret!");
            }

        }

        public static void ProcessWeapons(Mech mech)
        {
            Dictionary<int, Weapon> weaponsByHardpointId = new Dictionary<int, Weapon>();
            foreach (Weapon weapon in mech.Weapons)
            {
                Mod.Log.Info?.Write($"  -- weapon: {weapon.UIName} location: {weapon.LocationDef}  hardpointSlot: {weapon.baseComponentRef.HardpointSlot}");
                if (!weaponsByHardpointId.ContainsKey(weapon.baseComponentRef.HardpointSlot))
                {
                    weaponsByHardpointId.Add(weapon.baseComponentRef.HardpointSlot, weapon);
                }
            }

            // if (component.mechComponentRef.Is<LinkedActorComponent>(out LinkedActorComponent linkedTurret))
            if (mech.MechDef.Chassis.Is<WeaponControllerComponent>(out WeaponControllerComponent arcLimiter))
            {
                Mod.Log.Info?.Write($"  -- actor has firing arc limiters h2t: {arcLimiter?.AttachMappings?.Length}");
                foreach (AttachMapping am in arcLimiter.AttachMappings)
                {
                    Mod.Log.Info?.Write($"  ---- transform: {am.Transform}  hardpoints: {String.Join(",", am.HardpointID)}  " +
                        $"RestrictedFiringArc: {am.RestrictedFiringArc}");
                    ProcessAttachMappings(am, mech, weaponsByHardpointId);
                }
            }
            else
            {
                Mod.Log.Debug?.Write($"Actor had no firing arc limiter");
            }
        }

        static void ProcessAttachMappings(AttachMapping am, Mech parent, Dictionary<int, Weapon> weaponsByHardpoint)
        {
            if (am == null || am.HardpointID?.Length < 1 || weaponsByHardpoint == null || weaponsByHardpoint?.Count == 0) return; // nothing to do

            // Find the transform
            Mod.Log.Debug?.Write($"Looking for transform: {am.Transform} on parent: {parent.DistinctId()}");
            var transformGO = parent.GameRep.gameObject.GetComponentsInChildren<Transform>()
                            .FirstOrDefault(c => c.gameObject.name == am.Transform)?.gameObject;
            Transform attachTransform = transformGO.transform;

            if (attachTransform == null)
            {
                Mod.Log.Warn?.Write($"Failed to find transform: {am.Transform} on parent: {parent.DistinctId()}");
                return;
            }

            foreach (int hardpointId in am.HardpointID)
            {
                bool wasFound = weaponsByHardpoint.TryGetValue(hardpointId, out Weapon hardpointWeapon);
                if (wasFound)
                {
                    ModState.WeaponAttachTransforms.Add(hardpointWeapon, attachTransform);

                    Mod.Log.Info?.Write($"Processing attach_mapping for weapon: {hardpointWeapon.Description.UIName}_{hardpointWeapon.uid} in hardpoint: {hardpointId}");
                    if (am.RestrictedFiringArc != 0)
                    {
                        Mod.Log.Info?.Write($"  - restricting to arc: {am.RestrictedFiringArc}");
                        hardpointWeapon.StatCollection.AddStatistic<float>(ModStats.Weapon_Restricted_Firing_Arc, 0);
                        hardpointWeapon.StatCollection.Set<float>(ModStats.Weapon_Restricted_Firing_Arc, am.RestrictedFiringArc);
                    }

                }
            }
        }
    }
}
