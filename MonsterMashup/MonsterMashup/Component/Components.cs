using CustomComponents;
using System;
using System.Collections.Generic;

namespace MonsterMashup.Component
{
    [CustomComponent("MM_LinkedActor")]
    public class LinkedActorComponent : SimpleCustomComponent
    {
        public string CUVehicleDefId = "";
        public string PilotDefId = "";

        public string AttachPoint = "";

        public bool LinkInitiative = true;
    }

    [CustomComponent("MM_WeaponController")]
    public class WeaponControllerComponent: SimpleCustomChassis
    {
#pragma warning disable CS0649
        public AttachMapping[] AttachMappings;
    }

    public class AttachMapping
    {
        public string Transform = "";
#pragma warning disable CS0649
        public int[] HardpointID;
        public int RestrictedFiringArc = 0;
    }


}
