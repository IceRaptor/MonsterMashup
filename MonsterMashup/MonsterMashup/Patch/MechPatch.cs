﻿using IRBTModUtils.Extension;
using MonsterMashup.Helper;

namespace MonsterMashup.Patch
{
    // TODO: May be unnecessary, will be invoked from component destruction
    //[HarmonyPatch(typeof(Mech), "NukeStructureLocation")]
    //static class Mech_NukeStructureLocation
    //{
    //    static void Postfix(Mech __instance,
    //        WeaponHitInfo hitInfo, int hitLoc, ChassisLocations location, Vector3 attackDirection, DamageType damageType)
    //    {
    //        Mod.Log.Info?.Write("Mech:DamageLocation INVOKED");

    //        if (__instance == null) return;

    //        Mod.Log.Info?.Write($"Structure at location: {location} on actor: {__instance.DistinctId()} has been nuked");
    //        LinkedEntityHelper.DestroyLinksInLocation(__instance, location);
    //    }
    //}

    [HarmonyPatch(typeof(Mech), "DEBUG_DamageLocation")]
    static class Mech_DEBUG_DamageLocation
    {
        static void Postfix(Mech __instance, ArmorLocation aLoc)
        {
            Mod.Log.Info?.Write("Mech:DEBUG_DamageLocation INVOKED");

            if (Mod.Config.DeveloperOptions.DebugDamageLocationKillsLinkedTurrets)
            {
                ChassisLocations chassisLocationFromArmorLocation = MechStructureRules.GetChassisLocationFromArmorLocation(aLoc);
                Mod.Log.Info?.Write($" Developer option to kill linked turrets invoked on actor: {__instance.DistinctId()} at location: {aLoc}");
                LinkedEntityHelper.DestroyLinksInLocation(__instance, chassisLocationFromArmorLocation);
            }
        }
    }



}
