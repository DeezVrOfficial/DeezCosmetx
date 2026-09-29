using GorillaNetworking;
using HarmonyLib;
using UnityEngine;

namespace DeezCosmetx.Patches
{
    [HarmonyPatch(typeof(CosmeticsController.CosmeticSet), "LoadFromPlayerPreferences")]
    internal class NoNull : MonoBehaviour
    {
        private static bool Prefix(CosmeticsController.CosmeticSet __instance, CosmeticsController controller)
        {
            for (int i = 0; i < __instance.items.Length; i++)
            {
                CosmeticsController.CosmeticSlots slot = (CosmeticsController.CosmeticSlots)i;
                __instance.items[i] = controller.GetItemFromDict(PlayerPrefs.GetString(CosmeticsController.CosmeticSet.SlotPlayerPreferenceName(slot), "NOTHING"));
            }
            return false;
        }
    }
}