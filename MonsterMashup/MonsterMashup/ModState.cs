using MonsterMashup.Component;
using MonsterMashup.UI;
using System.Collections.Generic;

namespace MonsterMashup
{
    public static class ModState
    {

        // -- Teams we can use for spawning OpFors
        internal static Team TargetTeam = null;
        internal static Team TargetAllyTeam = null;
        internal static Team HostileToAllTeam = null;

        internal static List<(MechComponent source, LinkedTurretComponent linkedTurret)> ComponentsToLink = new List<(MechComponent source, LinkedTurretComponent linkedTurret)>();

        internal static Dictionary<string, FootprintVisualization> FootprintVisuals = new Dictionary<string, FootprintVisualization>();

        internal static Dictionary<string, AbstractActor> LinkedActors = new Dictionary<string, AbstractActor>();

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
        }
    }

}


