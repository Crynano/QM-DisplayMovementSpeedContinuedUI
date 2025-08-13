using HarmonyLib;
using MGSC;

namespace QM_DisplayMovementSpeedContinuedUI
{
    public class HoverPatches
    {
        // Custom new patch for UI
        [HarmonyPatch(typeof(ObjHighlightController), nameof(ObjHighlightController.Process))]
        public static class Patch_ObjHighlightController_Process
        {
            public static void Postfix(CellPosition cellUnderCursor, ObjHighlightController __instance)
            {
                Plugin.UpdateUI(cellUnderCursor, __instance);
            }
        }

        [HarmonyPatch(typeof(ObjHighlightController), nameof(ObjHighlightController.Unhighlight))]
        public static class Patch_ObjHighlightController_Unhighlight
        {
            public static void Postfix()
            {
                Plugin.ForceDisableUI();
            }
        }
    }
}