using HarmonyLib;

namespace DeezCosmetx.Patches
{
    [HarmonyPatch(typeof(VRRig))]
    [HarmonyPatch("IsItemAllowed", MethodType.Normal)]
    internal class SlidePatch
    {
        private static void Postfix(VRRig __instance, ref bool __result) =>
            __result = true;
    }
}