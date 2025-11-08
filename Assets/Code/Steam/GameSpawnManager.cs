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

    private Dictionary<int, PlanetHealth> teamBases = new();

    private bool isSpectating = false;
    private int currentSpectateIndex = 0;
    private List<Transform> livePlayers = new();

    private bool isLastTeamStanding = false;
    private float ltsCheckTimer = 1.0f;

    private List<int> teamPlacements = new List<int>();

    private void Awake()
    {
        Instance = this;
        // Initialize all teams as alive by default
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

        if (isLastTeamStanding && LobbyManager.Instance.IsHost)
        {
            ltsCheckTimer -= Time.deltaTime;
            if (ltsCheckTimer <= 0f)
            {
                CheckLastTeamStanding();
                ltsCheckTimer = 1.0f; 
            }
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

        StatsManager.Instance?.InitializePlayer(id.SteamId, teamNum);

        if (localPlayer.TryGetComponent<PlayerHealth>(out var health))
        {
            health.SetTeam(teamNum);
        }

        var trails = localPlayer.GetComponentsInChildren<TrailRenderer>();
        MaterialPropertyBlock trailBlock = new();
        trailBlock.SetColor("_TeamColor", colorPalette.Colors[teamNum - 1]);
        foreach (TrailRenderer trail in trails)
            trail.SetPropertyBlock(trailBlock);

        localPlayer.GetComponent<PlayerMovement>().enabled = true;
        _camera.GetComponent<CameraFollow>().target = localPlayer.transform;
    }

    public void RegisterTeamBase(int team, PlanetHealth healthComponent)
    {
        teamBases[team] = healthComponent;
    }

    public void UnregisterTeamBase(int team)
    {
        if (teamBases.ContainsKey(team))
            teamBases.Remove(team);
    }

    public void HandlePlayerLeave(CSteamID leavingPlayerId)
    {
        if (LobbyManager.Instance == null || !LobbyManager.Instance.IsHost) return;

        // 1. ZnajdŸ dru¿ynê gracza, który wyszed³
        string slotMeta = SteamMatchmaking.GetLobbyMemberData(
            LobbyManager.Instance.currentLobby,
            leavingPlayerId,
            "slot"
        );

        if (string.IsNullOrEmpty(slotMeta)) return;

        string[] split = slotMeta.Split('_');
        int teamNum = int.Parse(split[0]);

        int memberCount = SteamMatchmaking.GetNumLobbyMembers(LobbyManager.Instance.currentLobby);
        bool teamIsEmpty = true;
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
            Debug.Log($"Dru¿yna {teamNum} jest pusta. Host niszczy bazê.");
            if (teamBases.TryGetValue(teamNum, out PlanetHealth baseHealth) && baseHealth != null)
            {
                baseHealth.TakeDamage(99999f, CSteamID.Nil);
            }
            else
            {
                SetTeamBaseState(teamNum, false);
            }
        }
    }

    public void SetTeamBaseState(int team, bool alive)
    {
        teamBaseAlive[team] = alive;
        Debug.Log($"Team {team} base state updated: {(alive ? "ALIVE" : "DESTROYED")}");

        if (!isLastTeamStanding && !alive)
        {
            if (!teamPlacements.Contains(team))
            {
                teamPlacements.Insert(0, team);
            }

            CheckForGameEnd();
        }
    }

    private void CheckForGameEnd()
    {
        // Ta logika powinna byæ wykonywana TYLKO przez hosta i TYLKO przed LTS
        if (isLastTeamStanding) return;
        if (LobbyManager.Instance == null || !LobbyManager.Instance.IsHost) return;
        if (LobbyManager.Instance.currentLobby == CSteamID.Nil) return;

        // 1. ZnajdŸ wszystkie dru¿yny, które maj¹ aktywne bazy
        List<int> survivingBaseTeams = new List<int>();
        foreach (var entry in teamBaseAlive)
        {
            if (entry.Value) survivingBaseTeams.Add(entry.Key);
        }

        // 2. Faza "Base Attack": Jeœli > 1 baza ¿yje, gra trwa normalnie.
        if (survivingBaseTeams.Count > 1)
        {
            return;
        }

        // 3. Faza "Last Base Standing": Dok³adnie 1 baza ¿yje.
        if (survivingBaseTeams.Count == 1)
        {
            int lastTeamNum = survivingBaseTeams[0];

            // SprawdŸ, czy ta dru¿yna ma graczy
            bool teamHasPlayers = false;
            foreach (CSteamID member in LobbyManager.Instance.GetAllLobbyMembers())
            {
                string slot = SteamMatchmaking.GetLobbyMemberData(LobbyManager.Instance.currentLobby, member, "slot");
                if (!string.IsNullOrEmpty(slot) && slot.StartsWith($"{lastTeamNum}_"))
                {
                    teamHasPlayers = true;
                    break;
                }
            }

            // Warunek "Bedwars": Ostatnia baza + s¹ gracze = WYGRANA
            if (teamHasPlayers)
            {
                Debug.Log($"Gra zakoñczona (Bedwars)! Wygrywa dru¿yna {lastTeamNum}.");
                SendGameEndMessage(lastTeamNum);
                return; // Gra zakoñczona
            }

            // Jeœli !teamHasPlayers (ostatnia baza nale¿y do pustej dru¿yny),
            // nie robimy `return`. Logika przechodzi do Fazy 4 (przejœcie do LTS).
        }

        // 4. Faza "LTS Transition":
        // Dzieje siê, gdy (survivingBaseTeams.Count == 0) LUB (Count == 1 ale teamHasPlayers == false)

        Debug.Log("Bazy zniszczone lub ostatnia baza pusta. Przejœcie do Last Team Standing (LTS).");
        isLastTeamStanding = true;

        // Host automatycznie zacznie teraz sprawdzaæ `CheckLastTeamStanding()` w Update.
        // Mo¿na tu wys³aæ pakiet do klientów, aby wyœwietlili np. "SUDDEN DEATH!"
    }

    private void CheckLastTeamStanding()
    {
        // (Sprawdzenie IsHost jest ju¿ w Update)

        var allPlayers = FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);

        HashSet<int> activeTeams = new HashSet<int>();
        int alivePlayerCount = 0;

        foreach (var player in allPlayers)
        {
            // Polegamy na `activeSelf`. 
            // PlayerHealth (lokalny) musi siê sam deaktywowaæ przy permanentnej œmierci.
            // RemotePlayerManager (zdalny) deaktywuje obiekt, gdy otrzyma msg.isAlive = false.
            if (player.gameObject.activeSelf)
            {
                alivePlayerCount++;
                // Tag jest ustawiany przy spawnowaniu
                if (int.TryParse(player.tag.Split('_')[1], out int teamNum))
                {
                    activeTeams.Add(teamNum);
                }
            }
        }

        // Jeœli zosta³o > 1 dru¿yn (np. gracz z team 1 walczy z graczem z team 2), gra trwa
        if (activeTeams.Count > 1)
        {
            return;
        }

        // Jeœli zosta³a 1 dru¿yna (nawet jeœli jest to 1 gracz)
        if (activeTeams.Count == 1)
        {
            int winningTeam = activeTeams.First();
            Debug.Log($"Gra zakoñczona (LTS)! Wygrywa dru¿yna {winningTeam}.");
            SendGameEndMessage(winningTeam);
            isLastTeamStanding = false; // Zatrzymaj sprawdzanie
            return;
        }

        // Jeœli zosta³o 0 dru¿yn (activeTeams.Count == 0)
        // Oznacza to, ¿e nie ma ¿ywych graczy (alivePlayerCount == 0)
        // Ostatni gracze zabili siê nawzajem.
        if (alivePlayerCount == 0 && activeTeams.Count == 0)
        {
            Debug.Log("LTS: Wszyscy gracze zginêli. Remis. Koñczê grê.");
            SendGameEndMessage(-1); // Remis (-1)
            isLastTeamStanding = false; // Zatrzymaj sprawdzanie
        }
    }

    private void SendGameEndMessage(int winningTeam)
    {
        StatsManager.Instance?.FinalizeStats(winningTeam, teamPlacements);

        GameEndMessage msg = new()
        {
            winningTeam = winningTeam
        };

        byte[] data = NetworkHelpers.StructToBytes(msg);
        byte[] packet = new byte[data.Length + 1];
        packet[0] = (byte)PacketType.GameEnd;
        System.Buffer.BlockCopy(data, 0, packet, 1, data.Length);

        // Wyœlij do wszystkich (w tym do siebie, aby te¿ zmieniæ scenê)
        SteamNetworkingManager.Instance.BroadcastPacket(packet, EP2PSend.k_EP2PSendReliable);

        // Host równie¿ ³aduje scenê
        LobbyManager.Instance.LoadScene("Statistics");
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