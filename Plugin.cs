using BepInEx;
using DeezCosmetx.Patches;
using GorillaNetworking;
using HarmonyLib;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace DeezCosmetx
{
    [BepInPlugin(Constants.Guid, Constants.Name, Constants.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin instance;
        bool running;

        void Awake()
        {
            instance = this;
            new Harmony(Constants.Guid).PatchAll();
            Begin();
        }

        void Start() =>
            gameObject.AddComponent<ChestTags>();

        public void Begin()
        {
            if (running) return;
            running = true;
            StartCoroutine(UnlockWhenReady());
        }

        IEnumerator UnlockWhenReady()
        {
            for (float t = 60f; t > 0f; t -= 0.5f)
            {
                CosmeticsController c = CosmeticsController.instance;
                if (c != null && c.allCosmetics != null && c.allCosmetics.Count > 0 && !string.IsNullOrEmpty(c.concatStringCosmeticsAllowed))
                    break;
                yield return new WaitForSeconds(0.5f);
            }

            yield return new WaitForSeconds(2f);
            UnlockCosmetics();
            running = false;
        }

        public void UnlockCosmetics()
        {
            CosmeticsController controller = CosmeticsController.instance;
            MethodInfo unlockItem = typeof(CosmeticsController).GetMethod("UnlockItem", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (CosmeticsController.CosmeticItem item in controller.allCosmetics)
            {
                if (controller.concatStringCosmeticsAllowed.Contains(item.itemName))
                    continue;

                try { unlockItem.Invoke(controller, new object[] { item.itemName, false }); }
                catch { }
            }

            try { controller.OnCosmeticsUpdated?.Invoke(); }
            catch { }
        }
    }
}