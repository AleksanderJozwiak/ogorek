using UnityEngine;
using Steamworks;
using TMPro;

[System.Serializable]
public class TeamSpawnPoints
{
    public Transform[] slots = new Transform[2];
}

public class GameSpawnManager : MonoBehaviour
{
    [Header("Teams (9 total, each with 2 slots)")]
    public TeamSpawnPoints[] teams = new TeamSpawnPoints[9];
    [SerializeField] ColorPalette colorPalette;
    public CanvasGroup RespawnCounter;
    public TMP_Text Counter;

    private Camera _camera;
    private GameObject localPlayer;

    private void Start()
    {
        _camera = Camera.main;
        SpawnLocalPlayer();
        ShowRespawnUI(false);
    }

    public void RespawnPlayer(GameObject playerToRespawn)
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

        playerToRespawn.transform.position = spawnPoint.position;
        playerToRespawn.transform.rotation = spawnPoint.rotation;
    }

    public void ShowRespawnUI(bool visible)
    {
        RespawnCounter.alpha = visible ? 1 : 0;
        RespawnCounter.blocksRaycasts = visible;
        Counter.text = visible ? "5" : "";
    }

    public void UpdateRespawnCounter(int seconds)
    {
        Counter.text = seconds.ToString();
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

        var id = localPlayer.AddComponent<PlayerIdentity>();
        id.SteamId = SteamUser.GetSteamID();

        var trails = localPlayer.GetComponentsInChildren<TrailRenderer>();
        MaterialPropertyBlock trailBlock = new();
        trailBlock.SetColor("_TeamColor", colorPalette.Colors[teamNum - 1]);
        foreach (TrailRenderer trail in trails)
            trail.SetPropertyBlock(trailBlock);

        localPlayer.GetComponent<PlayerMovement>().enabled = true;
        _camera.GetComponent<CameraFollow>().target = localPlayer.transform;
    }
}
