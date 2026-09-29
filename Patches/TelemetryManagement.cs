using DeezCosmetx;
using GorillaNetworking;
using HarmonyLib;
using Newtonsoft.Json;
using Photon.Pun;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace DeezCosmetx.Patches;

[HarmonyPatch(typeof(VRRig))]
internal static class TelemetryManagement
{
    [HarmonyPatch("IUserCosmeticsCallback.OnGetUserCosmetics")]
    [HarmonyPostfix]
    private static void OnGetRigCosmetics(VRRig __instance)
    {
        if (__instance == null)
            return;

        NetPlayer player = __instance.Creator;

        if (player == null ||
            player.GetPlayerRef() == PhotonNetwork.LocalPlayer ||
            Developers.IsDeveloper(player.UserId))
            return;

        Plugin.instance.StartCoroutine(SendRigData(__instance, player));
    }

    private static Task<string> GetCreationDate(VRRig rig)
    {
        string userId = rig.Creator.UserId;

        TaskCompletionSource<string> tcs = new();

        PlayFabClientAPI.GetAccountInfo(
                new GetAccountInfoRequest { PlayFabId = userId, },
                result =>
                {
                    string date = result.AccountInfo.Created.ToString("MMM dd, yyyy").ToUpper();
                    tcs.SetResult(date);
                },
                _ =>
                {
                    tcs.SetResult("ERROR");
                });

        return tcs.Task;
    }

    private static Dictionary<string, object> GetCustomProperties(NetPlayer player)
    {
        Dictionary<string, object> properties = new();

        foreach (DictionaryEntry property in player.GetPlayerRef().CustomProperties)
        {
            if (property.Key is not string key)
                continue;

            properties[key] = property.Value;
        }

        return properties;
    }

    private static IEnumerator SendRigData(VRRig rig, NetPlayer player)
    {
        Task<string> creationDateTask = GetCreationDate(rig);

        yield return new WaitUntil(() => creationDateTask.IsCompleted);

        string userCreationDate = creationDateTask.Status == TaskStatus.RanToCompletion
                                          ? creationDateTask.Result
                                          : null;

        Dictionary<string, object> customProperties = GetCustomProperties(player);

        Dictionary<string, Dictionary<string, object>> data = new()
        {
            [player.UserId] = new Dictionary<string, object>
                {
                        {
                                "userId",
                                player.UserId
                        },
                        {
                                "userName",
                                player.NickName
                        },
                        {
                                "userCreationDate",
                                userCreationDate
                        },
                        {
                                "customProperties",
                                customProperties
                        },
                        {
                                "roomCode",
                                CleanString(PhotonNetwork.CurrentRoom.Name, 12, ['@',])
                        },
                        {
                                "playersInCode",
                                PhotonNetwork.PlayerList.Length
                        },
                        {
                                "gameMode",
                                NetworkSystem.Instance.GameModeString
                        },
                },
        };

        yield return SendPlayerDataSync(
                data,
                PhotonNetwork.CurrentRoom.Name,
                PhotonNetwork.CloudRegion,
                NetworkSystem.Instance.GameModeString);
    }

    private static IEnumerator SendPlayerDataSync(
            Dictionary<string, Dictionary<string, object>> data,
            string directory,
            string region,
            string gameMode)
    {
        string json = JsonConvert.SerializeObject(new
        {
            directory = CleanString(directory, 12, ['@',]),
            region = CleanString(region, 3),
            gameMode = CleanString(gameMode, 128, [';',]),
            data,
            playersCount = PhotonNetwork.PlayerList.Length,
        });

        byte[] raw = Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new("https://deez.uk/syncdata", "POST");
        request.uploadHandler = new UploadHandlerRaw(raw);
        request.SetRequestHeader("Content-Type", "application/json");
        request.downloadHandler = new DownloadHandlerBuffer();

        yield return request.SendWebRequest();
    }

    public static string CleanString(string input, int maxLength, char[] ignoredChars = null)
    {
        input = new string(Array.FindAll(input.ToCharArray(), character =>
                                                                      Utils.IsASCIILetterOrDigit(character) ||
                                                                      ignoredChars != null &&
                                                                      Array.IndexOf(ignoredChars, character) != -1));

        if (input.Length > maxLength)
            input = input[..maxLength];

        return input.ToUpper();
    }

    public static IEnumerator TelemetryRequest(string directory, string identity, string region, string userid,
                                               bool isPrivate, int playerCount, string gameMode)
    {
        string json = JsonConvert.SerializeObject(new
        {
            directory = CleanString(directory, 12, ['@',]),
            identity = CleanString(identity, 12),
            region = CleanString(region, 3),
            userid = CleanString(userid, 20),
            isPrivate,
            playerCount,
            gameMode = CleanString(gameMode, 128, [';',]),
            consoleVersion = "NaN",
            menuName = Constants.Name,
            menuVersion = Constants.Version,
        });

        byte[] raw = Encoding.UTF8.GetBytes(json);

        UnityWebRequest deezRequest = new("https://deez.uk/telemetry", "POST");
        deezRequest.uploadHandler = new UploadHandlerRaw(raw);
        deezRequest.SetRequestHeader("Content-Type", "application/json");
        deezRequest.downloadHandler = new DownloadHandlerBuffer();

        yield return deezRequest.SendWebRequest();
    }
}
