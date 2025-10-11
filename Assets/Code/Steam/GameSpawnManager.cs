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

    private GameObject localPlayer;

    private void Start()
    {
        _camera = Camera.main;
        SpawnLocalPlayer();
    }

    public void RespawnPlayer()
    {
        if (LobbyManager.Instance.currentLobby == CSteamID.Nil) return;

        string slotMeta = SteamMatchmaking.GetLobbyMemberData(
            LobbyManager.Instance.currentLobby,
            SteamUser.GetSteamID(),
            "slot"
        );

        string[] split = slotMeta.Split('_');
        int teamNum = int.Parse(split[0]);
        int slotNum = int.Parse(split[1]);

        Transform spawnPoint = teams[teamNum - 1].slots[slotNum - 1];

        localPlayer.transform.position = spawnPoint.position;
    }

    void SpawnLocalPlayer()
    {
        if (LobbyManager.Instance.currentLobby == CSteamID.Nil) return;

        string slotMeta = SteamMatchmaking.GetLobbyMemberData(
            LobbyManager.Instance.currentLobby,
            SteamUser.GetSteamID(),
            "slot"
        );

        if (string.IsNullOrEmpty(slotMeta)) return;

        string[] split = slotMeta.Split('_');
        int teamNum = int.Parse(split[0]); 
        int slotNum = int.Parse(split[1]); 

        Transform spawnPoint = teams[teamNum - 1].slots[slotNum - 1];
        GameObject playerPrefab = Resources.Load<GameObject>($"PlayerShip_{teamNum}");
        
        localPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        var trails = localPlayer.GetComponentsInChildren<TrailRenderer>();

        MaterialPropertyBlock trailBlock = new();
        trailBlock.SetColor("_TeamColor", colorPalette.Colors[teamNum - 1]);
        foreach (TrailRenderer trail in trails)
        {
            trail.SetPropertyBlock(trailBlock);
        }

        // Enable control only for the local player
        localPlayer.GetComponent<PlayerMovement>().enabled = true;
        _camera.GetComponent<CameraFollow>().target = localPlayer.transform;
    }
}
