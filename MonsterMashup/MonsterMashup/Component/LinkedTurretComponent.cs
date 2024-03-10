using CustomComponents;
using System;

namespace MonsterMashup.Component
{
    [CustomComponent("LinkedTurret")]
    internal class LinkedTurretComponent : SimpleCustomComponent
    {
        public string CUVehicleDefId = "";
        public string PilotDefId = "";

        public string AttachPoint = "";

        public bool LinkInitiative = true;

        public bool IsCUVehicle() { return !String.IsNullOrWhiteSpace(CUVehicleDefId); }
    }

}
