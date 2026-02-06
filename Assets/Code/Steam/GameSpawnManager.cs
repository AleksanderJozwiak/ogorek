using UnityEngine;
using Steamworks;
using TMPro;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq;

[System.Serializable]
public class TeamSpawnPoints
{
    public Transform[] slots = new Transform[2];
}

public class GameSpawnManager : MonoBehaviour
{
    public static GameSpawnManager Instance;

    [Header("Teams (9 total, each with 2 slots)")]
    public TeamSpawnPoints[] teams = new TeamSpawnPoints[9];
    [SerializeField] ColorPalette colorPalette;
    public CanvasGroup RespawnCounter;
    public TMP_Text Counter;
    public CanvasGroup SpectatorCanvas;
    public TMP_Text nickname;

    private Camera _camera;
    private GameObject localPlayer;

    private Dictionary<int, bool> teamBaseAlive = new();
    private Dictionary<int, PlanetHealth> teamBases = new();

    private bool isSpectating = false;
    private int currentSpectateIndex = 0;
    private List<Transform> livePlayers = new();

    private void Awake()
    {
        Instance = this;
        for (int i = 1; i <= 9; i++)
        {
            teamBaseAlive[i] = false;
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

    // --- SPAWN & RESPAWN ---

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

        // BED WARS LOGIC: Tylko jeœli baza ¿yje
        if (!IsTeamBaseAlive(teamNum))
        {
            Debug.Log($"Team {teamNum} base destroyed - no respawn.");
            if (targetId == SteamUser.GetSteamID())
                StartSpectateMode();
            return;
        }

        playerToRespawn.transform.position = spawnPoint.position;
        playerToRespawn.transform.rotation = spawnPoint.rotation;

        if (playerToRespawn.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = spawnPoint.position;
            rb.rotation = spawnPoint.eulerAngles.z;
        }

        foreach (var trail in playerToRespawn.GetComponentsInChildren<TrailRenderer>())
            trail.Clear();

        playerToRespawn.SetActive(true);
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

        StatsManager.Instance?.InitializePlayer(id.SteamId, teamNum);

        if (localPlayer.TryGetComponent<PlayerHealth>(out var health))
        {
            health.SetTeam(teamNum);
        }

        var trails = localPlayer.GetComponentsInChildren<TrailRenderer>();
        MaterialPropertyBlock trailBlock = new();
        if (colorPalette != null && colorPalette.Colors.Length >= teamNum)
            trailBlock.SetColor("_TeamColor", colorPalette.Colors[teamNum - 1]);

        foreach (TrailRenderer trail in trails)
            trail.SetPropertyBlock(trailBlock);

        localPlayer.GetComponent<PlayerMovement>().enabled = true;
        _camera.GetComponent<CameraFollow>().target = localPlayer.transform;
    }

    // --- BASE MANAGEMENT ---

    public void RegisterTeamBase(int team, PlanetHealth healthComponent)
    {
        teamBases[team] = healthComponent;
        teamBaseAlive[team] = true;
    }

    public void UnregisterTeamBase(int team)
    {
        if (teamBases.ContainsKey(team))
            teamBases.Remove(team);
    }

    public void SetTeamBaseState(int team, bool alive)
    {
        teamBaseAlive[team] = alive;
        // USUNIÊTO: CheckForGameEnd(). 
        // Teraz GameSpawnManager NIE decyduje o koñcu gry. Robi to StatsManager.
    }

    public bool IsTeamBaseAlive(int team)
    {
        return teamBaseAlive.TryGetValue(team, out bool alive) && alive;
    }

    // --- GAME END (Wywo³ywane przez StatsManagera) ---

    public void TriggerGameEnd(int winningTeam)
    {
        if (LobbyManager.Instance == null || !LobbyManager.Instance.IsHost) return;

        Debug.Log($"GameSpawnManager: Triggering Game End. Winner: {winningTeam}");

        // UWAGA: Nie wywo³ujemy tu FinalizeStats, bo StatsManager ju¿ to zrobi³ przed wywo³aniem tej metody!

        // 1. Pakowanie danych
        var allStats = StatsManager.Instance.GetAllStats();
        string statsJson = JsonUtility.ToJson(new PlayerStatsListWrapper { stats = allStats });
        string payload = winningTeam + "|" + statsJson;
        byte[] payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

        // 2. Wys³anie pakietu
        byte[] packet = new byte[payloadBytes.Length + 1];
        packet[0] = (byte)PacketType.GameEnd;
        System.Buffer.BlockCopy(payloadBytes, 0, packet, 1, payloadBytes.Length);

        foreach (CSteamID member in LobbyManager.Instance.GetAllLobbyMembers())
        {
            if (member != SteamUser.GetSteamID())
                SteamNetworking.SendP2PPacket(member, packet, (uint)packet.Length, EP2PSend.k_EP2PSendReliable);
        }

        // 3. Zmiana sceny u Hosta
        LobbyManager.Instance.LoadScene("Statistics");
    }

    public void HandlePlayerLeave(CSteamID leavingPlayerId)
    {
        if (LobbyManager.Instance == null || !LobbyManager.Instance.IsHost) return;

        string slotMeta = SteamMatchmaking.GetLobbyMemberData(
            LobbyManager.Instance.currentLobby,
            leavingPlayerId,
            "slot"
        );

        if (string.IsNullOrEmpty(slotMeta)) return;
        int teamNum = int.Parse(slotMeta.Split('_')[0]);

        // SprawdŸ czy team jest pusty
        bool teamIsEmpty = true;
        int memberCount = SteamMatchmaking.GetNumLobbyMembers(LobbyManager.Instance.currentLobby);

        for (int i = 0; i < memberCount; i++)
        {
            CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(LobbyManager.Instance.currentLobby, i);
            if (member == leavingPlayerId) continue;
            string memberSlot = SteamMatchmaking.GetLobbyMemberData(LobbyManager.Instance.currentLobby, member, "slot");
            if (!string.IsNullOrEmpty(memberSlot) && memberSlot.StartsWith($"{teamNum}_"))
            {
                teamIsEmpty = false;
                break;
            }
        }

        if (teamIsEmpty)
        {
            // Jeœli team pusty -> niszczymy bazê, co uruchomi lawinê w StatsManagerze
            if (teamBases.TryGetValue(teamNum, out PlanetHealth baseHealth) && baseHealth != null)
            {
                baseHealth.TakeDamage(99999f, CSteamID.Nil);
            }
            else
            {
                SetTeamBaseState(teamNum, false);
                StatsManager.Instance?.SetPlanetState(teamNum, false);
            }
        }

        // Wa¿ne: powiadom StatsManagera, ¿e gracz fizycznie znikn¹³ (nie ¿yje)
        StatsManager.Instance?.SetPlayerAliveState(leavingPlayerId, false);
    }

    // --- UI & SPECTATOR ---

    public void ShowRespawnUI(bool visible)
    {
        if (RespawnCounter != null)
        {
            RespawnCounter.alpha = visible ? 1 : 0;
            RespawnCounter.blocksRaycasts = visible;
            if (Counter != null) Counter.text = visible ? "5" : "";
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
        if (Counter != null) Counter.text = seconds.ToString();
    }

    public void StartSpectateMode()
    {
        ShowRespawnUI(false);
        isSpectating = true;
        ShowSpectatorUI(true);

        if (localPlayer != null) localPlayer.SetActive(false);

        UpdateSpectateTargets();
        SwitchSpectateTarget();
    }

    private void UpdateSpectateTargets()
    {
        livePlayers.Clear();
        foreach (var identity in FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None))
        {
            if (identity.SteamId == SteamUser.GetSteamID()) continue;

            GameObject p = identity.gameObject;
            // Dodajemy do obserwowanych, jeœli obiekt jest aktywny LUB (ma Health i ¿yje logicznie)
            if (p.activeSelf || (p.TryGetComponent<PlayerHealth>(out var h) && h.IsAlive()))
            {
                livePlayers.Add(p.transform);
            }
        }
    }

    private void SwitchSpectateTarget()
    {
        if (livePlayers.Count == 0)
        {
            if (nickname != null) nickname.text = "No players";
            return;
        }

        currentSpectateIndex = (currentSpectateIndex + 1) % livePlayers.Count;
        Transform nextTarget = livePlayers[currentSpectateIndex];

        if (nextTarget != null)
        {
            _camera.GetComponent<CameraFollow>().target = nextTarget;
            CSteamID memberId = nextTarget.gameObject.GetComponent<PlayerIdentity>().SteamId;
            if (nickname != null) nickname.text = SteamFriends.GetFriendPersonaName(memberId);
        }
    }

    [System.Serializable]
    public class PlayerStatsListWrapper
    {
        public List<PlayerStats> stats;
    }
}