using HarmonyLib;
using MGSC;
using System;

namespace QM_DisplayMovementSpeedContinuedUI.Patches
{
    public static class HoverPatches
    {
        [HarmonyPatch(typeof(Monster), nameof(Monster.UpdateVisibility), new Type[] { typeof(bool) })]
        public static class Path_Monster_UpdateVisibility_Bool
        {
            public static void Postfix(bool isSeen, Monster __instance)
            {
                if (isSeen)
                {
                    Plugin.UpdateMonsterUI(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(Monster), nameof(Monster.Highlight))]
        public static class Patch_Monster_Highlight
        {
            public static void Postfix(bool val, bool destroyable, Monster __instance)
            {
                if (val)
                {
                    Plugin.UpdateMonsterUI(__instance);
                    Plugin.SetEnemyFocus(__instance);
                }
            }
        }
    }
}