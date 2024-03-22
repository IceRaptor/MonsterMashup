
using IRBTModUtils;
using System;

namespace MonsterMashup.Helper
{
    public static class UnitHelper
    {
        public static void CrushTarget(Mech attacker, AbstractActor target, bool quip)
        {
            Mod.Log.Debug?.Write("Taunting player.");
            // Create a quip
            Guid g = Guid.NewGuid();
            QuipHelper.PlayQuip(SharedState.Combat, g.ToString(), attacker.team, attacker.DisplayName, Mod.LocalizedText.Quips.Crush, 5f);

            // TODO: CRUSHED is used here as source, needs to come from component

            if (target is Mech targetMech)
            {
                // Should work for CU or vanilla units
                targetMech.statCollection.ModifyStat("-1", -1, targetMech.GetStringForStructureLocation(ChassisLocations.Head), StatCollection.StatOperation.Set, 0f);
                targetMech.statCollection.ModifyStat("-1", -1, targetMech.GetStringForStructureLocation(ChassisLocations.CenterTorso), StatCollection.StatOperation.Set, 0f);
                targetMech.statCollection.ModifyStat("-1", -1, targetMech.GetStringForStructureLocation(ChassisLocations.LeftTorso), StatCollection.StatOperation.Set, 0f);
                targetMech.statCollection.ModifyStat("-1", -1, targetMech.GetStringForStructureLocation(ChassisLocations.RightTorso), StatCollection.StatOperation.Set, 0f);
                targetMech.statCollection.ModifyStat("-1", -1, targetMech.GetStringForStructureLocation(ChassisLocations.LeftArm), StatCollection.StatOperation.Set, 0f);
                targetMech.statCollection.ModifyStat("-1", -1, targetMech.GetStringForStructureLocation(ChassisLocations.RightArm), StatCollection.StatOperation.Set, 0f);
                targetMech.statCollection.ModifyStat("-1", -1, targetMech.GetStringForStructureLocation(ChassisLocations.LeftLeg), StatCollection.StatOperation.Set, 0f);
                targetMech.statCollection.ModifyStat("-1", -1, targetMech.GetStringForStructureLocation(ChassisLocations.RightLeg), StatCollection.StatOperation.Set, 0f);
            }
            else if (target is Vehicle targetVehicle)
            {
                targetVehicle.statCollection.ModifyStat("-1", -1, targetVehicle.GetStringForStructureLocation(VehicleChassisLocations.Front), StatCollection.StatOperation.Set, 0f);
                targetVehicle.statCollection.ModifyStat("-1", -1, targetVehicle.GetStringForStructureLocation(VehicleChassisLocations.Left), StatCollection.StatOperation.Set, 0f);
                targetVehicle.statCollection.ModifyStat("-1", -1, targetVehicle.GetStringForStructureLocation(VehicleChassisLocations.Right), StatCollection.StatOperation.Set, 0f);
                targetVehicle.statCollection.ModifyStat("-1", -1, targetVehicle.GetStringForStructureLocation(VehicleChassisLocations.Rear), StatCollection.StatOperation.Set, 0f);

            }
            else if (target is Turret targetTurret)
            {
                targetTurret.statCollection.ModifyStat("-1", -1, targetTurret.GetStringForStructureLocation(BuildingLocation.Structure), StatCollection.StatOperation.Set, 0f);
            }

            // Common functionality
            target.GetPilot()?.KillPilot(SharedState.Combat.Constants, "", 0, DamageType.DropShip, null, null);
            target.FlagForDeath("Crushed!", DeathMethod.PilotKilled, DamageType.DropShip, -1, -1, "", isSilent: false);
            target.HandleDeath("0");
        }
    }
}
