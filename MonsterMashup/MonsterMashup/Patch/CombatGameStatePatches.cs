using BattleTech;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterMashup.Patch
{
    [HarmonyPatch(typeof(CombatGameState), "OnCombatGameDestroyed")]
    public static class CombatGameState_OnCombatGameDestroyed
    {

        public static void Postfix()
        {
            Mod.Log.Trace?.Write("CGS:OCGD - entered.");

            // Reset any combat state
            ModState.Reset();
        }
    }
}
