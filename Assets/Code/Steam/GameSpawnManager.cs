using UnityEngine;
using Steamworks;

[System.Serializable]
public class TeamSpawnPoints
{
    public Transform[] slots = new Transform[2]; // slot 0 and slot 1
}


public class GameSpawnManager : MonoBehaviour
{
    [Header("Teams (9 total, each with 2 slots)")]
    public TeamSpawnPoints[] teams = new TeamSpawnPoints[9];

    private void Start()
    {
        SpawnLocalPlayer();
    }

    void SpawnLocalPlayer()
    {
        if (LobbyManager.Instance.currentLobby == CSteamID.Nil) return;

        // Get my slot metadata from lobby
        string slotMeta = SteamMatchmaking.GetLobbyMemberData(
            LobbyManager.Instance.currentLobby,
            SteamUser.GetSteamID(),
            "slot"
        );

        if (string.IsNullOrEmpty(slotMeta)) return;

        // slotMeta format: "team_slot" (e.g., "3_2")
        string[] split = slotMeta.Split('_');
        int teamNum = int.Parse(split[0]); // 1–9
        int slotNum = int.Parse(split[1]); // 1–2

        Transform spawnPoint = teams[teamNum - 1].slots[slotNum - 1];
        GameObject playerPrefab = Resources.Load<GameObject>($"PlayerShip_{teamNum}"); // prefab in Resources folder

        GameObject localPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        // Enable control only for the local player
        localPlayer.GetComponent<PlayerMovement>().enabled = true;
    }
}
