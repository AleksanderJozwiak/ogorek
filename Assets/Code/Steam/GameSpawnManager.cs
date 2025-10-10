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
    [SerializeField] ColorPalette colorPalette;

    private Camera _camera;

    private void Start()
    {
        _camera = Camera.main;
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

        
        var trails = playerPrefab.GetComponentsInChildren<TrailRenderer>();
        MaterialPropertyBlock trailBlock = new();
        trailBlock.SetColor("_TeamColor", colorPalette.Colors[teamNum]);
        foreach (TrailRenderer trail in trails)
        {
            trail.SetPropertyBlock(trailBlock);
        }

        GameObject localPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);


        // Enable control only for the local player
        localPlayer.GetComponent<PlayerMovement>().enabled = true;
        _camera.GetComponent<CameraFollow>().target = localPlayer.transform;
    }
}
