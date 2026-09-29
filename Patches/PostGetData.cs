using GorillaNetworking.Store;
using HarmonyLib;

namespace DeezCosmetx.Patches
{
    [HarmonyPatch(typeof(StoreUpdater))]
    [HarmonyPatch("Initialize", MethodType.Normal)]
    public class PostGetData
    {
        private static void Postfix() =>
            Plugin.instance.Begin();
    }
}