using System;
using UnityEngine;

namespace MonsterMashup.Patch
{
    [HarmonyPatch(typeof(Weapon), "WillFireAtTargetFromPosition")]
    [HarmonyPatch(new Type[] { typeof(ICombatant), typeof(Vector3), typeof(Quaternion) })]
    static class WeaponPatch
    {
        // Position and rotation are attacker, not target
        static void Postfix(Weapon __instance, ref bool __result, ICombatant target, Vector3 position, Quaternion rotation)
        {
            if (__instance == null || target == null) return;

            Mod.Log.Debug?.Write($"WillFireAtTargetFromPosition for weapon: {__instance.UIName}_{__instance.uid} has result: {__result}");

            if (__result == false) return; // Out ammo or has no ammo

            if (__instance.StatCollection.ContainsStatistic(ModStats.Weapon_Restricted_Firing_Arc))
            {
                float restrictedFiringArc = __instance.StatCollection.GetValue<float>(ModStats.Weapon_Restricted_Firing_Arc);
                bool hasAttach = ModState.WeaponAttachTransforms.TryGetValue(__instance, out Transform weaponAttachTransform);
                if (!hasAttach)
                {
                    Mod.Log.Warn?.Write($"Attach for weapon: {__instance.UIName}_{__instance.uid} could not be found!");
                    return;
                }

                Mod.Log.Debug?.Write($"Weapon: {__instance.UIName}_{__instance.uid} has restrictedFiringArc: {restrictedFiringArc} with transform: {weaponAttachTransform.name}");
                //Quaternion attachTransformRot = Quaternion.LookRotation(weaponAttachTransform.forward);
                //float firingAngle = Quaternion.Angle(target.CurrentRotation, attachTransformRot);
                //float firingAngle = Vector3.Angle(attachTransform.forward, target.CurrentPosition);
                Vector3 targetToAttach = target.TargetPosition - weaponAttachTransform.position;
                float firingAngle = Vector3.Angle(weaponAttachTransform.forward, targetToAttach);
                __result = firingAngle < restrictedFiringArc;
                Mod.Log.Debug?.Write($"  firingAngle: {firingAngle} = " +
                    $"attachTransform.forward: {weaponAttachTransform.forward} attachTransform.rotation: {weaponAttachTransform.rotation} " +
                    $"target.currentPosition: {target.CurrentRotation} - " +
                    $"weaponAttachTransform.position {weaponAttachTransform.position} => targetToAttach: {targetToAttach}"
                    );

                Mod.Log.Debug?.Write($"  result: {__result} from firingAngle: {firingAngle} < {restrictedFiringArc}");
            }
        }
    }
}
