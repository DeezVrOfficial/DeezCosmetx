using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DeezCosmetx.Patches;

public class ChestTags : MonoBehaviour
{
    private const string DevPrefix = "[<color=#FFC0CB>Dev</color>] ";

    private const float UpdateInterval = 0.5f;

    private float updateTimer;

    private void LateUpdate()
    {
        updateTimer += Time.deltaTime;
        if (updateTimer < UpdateInterval)
            return;
        updateTimer = 0f;

        if (!PhotonNetwork.InRoom || !VRRigCache.isInitialized)
            return;

        foreach (RigContainer rigContainer in VRRigCache.ActiveRigContainers)
        {
            if (rigContainer == null)
                continue;

            VRRig rig = rigContainer.Rig;

            if (rig == null || rig.isLocal || rig.playerText1 == null)
                continue;

            NetPlayer creator = rig.Creator;

            if (creator == null || string.IsNullOrEmpty(creator.UserId))
                continue;

            string current = rig.playerText1.text ?? "";

            string baseLine = current.StartsWith(DevPrefix, StringComparison.Ordinal)
                ? current.Substring(DevPrefix.Length)
                : current;

            if (string.IsNullOrEmpty(baseLine) || baseLine == "NAME")
                baseLine = SanitizeName(creator.NickName);

            if (string.IsNullOrEmpty(baseLine))
                continue;

            string desired = (Developers.IsDeveloper(creator.UserId) ? DevPrefix : "") + baseLine;

            if (current != desired)
                rig.playerText1.text = desired;
        }
    }

    private static string SanitizeName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "?";
        }

        StringBuilder sb = new(name.Length);
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (char.IsLetterOrDigit(c) || c == ' ')
            {
                sb.Append(c);
            }
        }

        string result = sb.ToString().Trim();
        return result.Length == 0 ? "?" : result;
    }
}

[HarmonyPatch(typeof(GorillaPlayerScoreboardLine), "UpdateLine", new Type[] { })]
public static class ScoreboardPatch
{
    private const string BoardDevPrefix = "[<color=#000000>Dev</color>] ";

    private static void Postfix(GorillaPlayerScoreboardLine __instance)
    {
        if (__instance == null ||
            __instance.linePlayer == null ||
            __instance.playerNameVisible == null)
            return;

        Player player = Find(__instance.linePlayer.UserId);

        if (player == null)
            return;

        string baseName = __instance.playerNameVisible;

        if (string.IsNullOrEmpty(baseName))
            return;

        if (baseName.StartsWith(BoardDevPrefix, StringComparison.Ordinal))
            baseName = baseName.Substring(BoardDevPrefix.Length);

        string rebuilt = Developers.IsDeveloper(player.UserId)
            ? BoardDevPrefix + baseName
            : baseName;

        if (rebuilt != __instance.playerNameVisible)
            __instance.playerNameVisible = rebuilt;
    }

    private static Dictionary<string, Player> playerLookupById;
    private static int lastPlayerListHash;

    public static Player Find(string id)
    {
        if (!PhotonNetwork.InRoom || string.IsNullOrEmpty(id))
        {
            return null;
        }

        Player[] playerList = PhotonNetwork.PlayerList;
        int hash = playerList.Length;
        for (int i = 0; i < playerList.Length; i++)
        {
            hash = HashCode.Combine(hash, playerList[i].ActorNumber);
        }

        if (hash != lastPlayerListHash)
        {
            lastPlayerListHash = hash;
            playerLookupById.Clear();
            for (int i = 0; i < playerList.Length; i++)
            {
                if (!string.IsNullOrEmpty(playerList[i].UserId))
                {
                    playerLookupById[playerList[i].UserId] = playerList[i];
                }
            }
        }

        playerLookupById.TryGetValue(id, out Player result);
        return result;
    }
}

public static class Developers
{
    public static readonly HashSet<string> DevIds = new(StringComparer.Ordinal)
    {
        "C9027C8A184048B",
        "A7D681BB8CE8B11B",
        "38E2F732D0A8C7D",
        "1D71AB82B1AAF83F"
    };

    public static bool IsDeveloper(string userId)
    {
        return !string.IsNullOrEmpty(userId) && DevIds.Contains(userId);
    }
}