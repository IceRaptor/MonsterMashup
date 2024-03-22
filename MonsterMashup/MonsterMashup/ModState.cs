using MonsterMashup.Component;
using MonsterMashup.Helper;
using MonsterMashup.UI;
using System.Collections.Generic;
using UnityEngine;

namespace MonsterMashup
{
    public static class ModState
    {

        // -- Teams we can use for spawning OpFors
        internal static Team TargetTeam = null;
        internal static Team TargetAllyTeam = null;
        internal static Team HostileToAllTeam = null;

        internal static List<(MechComponent source, LinkedActorComponent linkedTurret)> ComponentsToLink = new List<(MechComponent source, LinkedActorComponent linkedTurret)>();

        internal static Dictionary<string, FootprintVisualization> FootprintVisuals = new Dictionary<string, FootprintVisualization>();

        internal static Dictionary<string, AbstractActor> LinkedActors = new Dictionary<string, AbstractActor>();
        internal static Dictionary<string, Transform> AttachTransforms = new Dictionary<string, Transform>();
        internal static Dictionary<Weapon, Transform> WeaponAttachTransforms = new Dictionary<Weapon, Transform>();
        internal static Dictionary<string, List<SupportSpawnState>> ChildSpawns = new Dictionary<string, List<SupportSpawnState>>();

        internal static HashSet<AbstractActor> Parents = new HashSet<AbstractActor>();

        internal static void Reset()
        {
            TargetTeam = null;
            TargetAllyTeam = null;
            HostileToAllTeam = null;

            ComponentsToLink.Clear();

            foreach (FootprintVisualization footprintVis in FootprintVisuals.Values)
            {
                footprintVis.Destroy();
            }
            FootprintVisuals.Clear();

            LinkedActors.Clear();
            AttachTransforms.Clear();
            WeaponAttachTransforms.Clear();
            ChildSpawns.Clear();
            Parents.Clear();
        }
    }

}


