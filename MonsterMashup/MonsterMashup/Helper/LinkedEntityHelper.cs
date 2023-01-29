using BattleTech;
using CustomComponents;
using IRBTModUtils;
using IRBTModUtils.Extension;
using MonsterMashup.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterMashup.Helper
{
    public static class LinkedEntityHelper
    {
        public static void DestroyLinksInLocation(Mech mech, ChassisLocations location)
        {
            Mod.Log.Info?.Write($"Scanning location: {location} on actor: {mech.DistinctId()} for linked turrets.");
            List<MechComponent> components = mech.GetComponentsForLocation(location, ComponentType.Upgrade);
            foreach (MechComponent comp in components)
            {
                Mod.Log.Debug?.Write($" -- mechComponent: {comp.UIName}");
                if (comp.mechComponentRef.Is<LinkedTurretComponent>())
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
    }
}
