
using BattleTech;
using BattleTech.UI;
using IRBTModUtils;
using IRBTModUtils.Extension;
using MonsterMashup.Component;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MonsterMashup
{
    public static class ModState {

        // -- Teams we can use for spawning OpFors
        internal static Team TargetTeam = null;
        internal static Team TargetAllyTeam = null;
        internal static Team HostileToAllTeam = null;

        internal static Dictionary<AbstractActor, List<LinkedTurretComponent>> LinkedActors = new Dictionary<AbstractActor, List<LinkedTurretComponent>>();

        internal static void Reset() {
            TargetTeam = null;
            TargetAllyTeam = null;
            HostileToAllTeam = null;

            LinkedActors.Clear();
        }
    }

}


