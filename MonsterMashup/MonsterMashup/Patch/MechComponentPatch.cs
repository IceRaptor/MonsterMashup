﻿using BattleTech;
using CustomComponents;
using Harmony;
using IRBTModUtils.Extension;
using MonsterMashup.Component;
using MonsterMashup.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonsterMashup.Patch
{
    [HarmonyPatch(typeof(MechComponent), "DamageComponent")]
    static class MechComponent_DamageComponent
    {

        static void Postfix(MechComponent __instance, WeaponHitInfo hitInfo, ComponentDamageLevel damageLevel, bool applyEffects)
        {
            Mod.Log.Debug?.Write("MechComponent:DamageComponent - INVOKED");

            if (__instance == null || __instance.mechComponentRef == null || 
                damageLevel != ComponentDamageLevel.Destroyed) return; // nothing to do

            if (__instance.mechComponentRef.Is<LinkedTurretComponent>())
            {
                Mod.Log.Info?.Write($"MechComponent: {__instance.parent.DistinctId()}:{__instance.uid} has been destroyed, killing linked turret.");
                LinkedEntityHelper.KillLinkedTurret(__instance);
            }
        }
    }
}
