using CustomComponents;

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

    [CustomComponent("MM_CrushOnCollision")]
    public class CrushOnCollisionComponent : SimpleCustomChassis
    {
        public bool PlayTaunt = true;
        public float Radius = 8f;
        public bool ShowRadius = false;
        public string RadiusTransform = string.Empty;
    }

    [CustomComponent("MM_PreventMelee")]
    public class PreventMeleeComponent : SimpleCustomChassis
    {
    }

    [CustomComponent("MM_CombatSpawn")]
    public class CombatSpawnComponent : SimpleCustomChassis
    {
        public SpawnConfig[] Spawns;
    }

    public class SpawnConfig
    {
        public string AttachPoint;
        public string CUVehicleDefId;
        public string PilotDefId;
        public int MaxSpawns = 0; // total to spawn
        public int RoundsBetweenSpawns = 0; // rounds between spawns
    }

    [CustomComponent("MM_TriggeredFlee")]
    public class FleeComponent: SimpleCustomChassis
    {
        public bool TriggerOnAllLinkedActorsDead = true;
        public int RoundsToDelay = 10;
        public string ParticleParentGO = string.Empty;
    }
}
