using UnityEngine;
using Steamworks;
using TMPro;
using System.Collections.Generic;
using Unity.VisualScripting;

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
    public CanvasGroup SpectatorCanvas;
    public TMP_Text nickname;

    private Camera _camera;
    private GameObject localPlayer;

    public static GameSpawnManager Instance;
    private Dictionary<int, bool> teamBaseAlive = new();
    private bool isSpectating = false;
    private int currentSpectateIndex = 0;
    private List<Transform> livePlayers = new();

    private void Awake()
    {
        Instance = this;
        // Initialize all teams as alive by default
        for (int i = 1; i <= 9; i++)
        {
            teamBaseAlive[i] = true;
        }
    }

    private void Start()
    {
        _camera = Camera.main;
        SpawnLocalPlayer();
        ShowRespawnUI(false);
        ShowSpectatorUI(false);
    }

    private void Update()
    {
        if (isSpectating && Input.GetKeyDown(KeyCode.Space))
        {
            UpdateSpectateTargets();
            SwitchSpectateTarget();
        }
    }

    public void RespawnPlayer(GameObject playerToRespawn)
    {
        if (LobbyManager.Instance.currentLobby == CSteamID.Nil) return;

        var identity = playerToRespawn.GetComponent<PlayerIdentity>();
        CSteamID targetId = identity != null ? identity.SteamId : SteamUser.GetSteamID();

        string slotMeta = SteamMatchmaking.GetLobbyMemberData(
            LobbyManager.Instance.currentLobby,
            targetId,
            "slot"
        );

        if (string.IsNullOrEmpty(slotMeta)) return;

        string[] split = slotMeta.Split('_');
        int teamNum = int.Parse(split[0]);
        int slotNum = int.Parse(split[1]);
        Transform spawnPoint = teams[teamNum - 1].slots[slotNum - 1];

        if (!IsTeamBaseAlive(teamNum))
        {
            Debug.Log($"Team {teamNum}'s base is destroyed - no respawn allowed.");
            if (targetId == SteamUser.GetSteamID())
                StartSpectateMode();
            return;
        }

        playerToRespawn.transform.position = spawnPoint.position;
        playerToRespawn.transform.rotation = spawnPoint.rotation;

        if (playerToRespawn.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.angularVelocity = 0f;
            rb.position = spawnPoint.position;
            rb.rotation = spawnPoint.eulerAngles.z;
        }

        foreach (var trail in playerToRespawn.GetComponentsInChildren<TrailRenderer>())
            trail.Clear();
    }

    public void ShowRespawnUI(bool visible)
    {
        if (RespawnCounter != null)
        {
            RespawnCounter.alpha = visible ? 1 : 0;
            RespawnCounter.blocksRaycasts = visible;
            if (Counter != null)
                Counter.text = visible ? "5" : "";
        }
    }

    public void ShowSpectatorUI(bool visible)
    {
        if (SpectatorCanvas != null)
        {
            SpectatorCanvas.alpha = visible ? 1 : 0;
            SpectatorCanvas.blocksRaycasts = visible;
        }
    }

    public void UpdateRespawnCounter(int seconds)
    {
        if (Counter != null)
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
        localPlayer.tag = $"Team_{teamNum}";

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

    public void SetTeamBaseState(int team, bool alive)
    {
        teamBaseAlive[team] = alive;
        Debug.Log($"Team {team} base state updated: {(alive ? "ALIVE" : "DESTROYED")}");
    }

    public bool IsTeamBaseAlive(int team)
    {
        return teamBaseAlive.TryGetValue(team, out bool alive) && alive;
    }

    public void StartSpectateMode()
    {
        ShowRespawnUI(false);
        isSpectating = true;
        ShowSpectatorUI(true);

        if (localPlayer != null)
            localPlayer.SetActive(false);

        UpdateSpectateTargets();
        SwitchSpectateTarget();
    }

    private void UpdateSpectateTargets()
    {
        livePlayers.Clear();

        foreach (var identity in FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None))
        {
            if (identity.SteamId == SteamUser.GetSteamID())
                continue;

            GameObject playerGO = identity.gameObject;
            if (playerGO.TryGetComponent<PlayerHealth>(out var health))
            {
                if (health.IsAlive())
                    livePlayers.Add(playerGO.transform);
            }
            else
            {
                if (playerGO.activeSelf)
                    livePlayers.Add(playerGO.transform);
            }
        }
    }

    private void SwitchSpectateTarget()
    {
        if (livePlayers.Count == 0)
        {
            Debug.Log("No live players to spectate.");
            if (nickname != null)
                nickname.text = "No players to spectate";
            return;
        }

        currentSpectateIndex = (currentSpectateIndex + 1) % livePlayers.Count;
        Transform nextTarget = livePlayers[currentSpectateIndex];

        _camera.GetComponent<CameraFollow>().target = nextTarget;
        CSteamID memberId = nextTarget.gameObject.GetComponent<PlayerIdentity>().SteamId;
        if (nickname != null)
            nickname.text = SteamFriends.GetFriendPersonaName(memberId);
        Debug.Log($"Spectating: {nextTarget.name}");
    }
}