using UnityEngine;
using Steamworks;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class PlayerStats
{
    public ulong SteamId;
    public string Name;
    public int Team;
    public bool IsAlive = true; // Czy statek gracza fizycznie istnieje na mapie?

    public int PlanetsDestroyed = 0;
    public int Kills = 0;
    public int FinalKills = 0;
    public int Assists = 0;
    public int Deaths = 0;
    public float TotalDamageDealt = 0;
    public int Placement = 0;

    public Dictionary<ulong, float> DamageContributions = new Dictionary<ulong, float>();
}

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance;

    private Dictionary<ulong, PlayerStats> allPlayerStats = new Dictionary<ulong, PlayerStats>();
    private Dictionary<int, bool> teamPlanetsAlive = new Dictionary<int, bool>();

    // Lista dru¿yn w kolejnoœci odpadania (pierwszy na liœcie = odpad³ jako pierwszy)
    private List<int> eliminationOrder = new List<int>();

    public int WinningTeam { get; private set; } = -1;
    private Dictionary<int, int> teamPlacements = new Dictionary<int, int>();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // --- INICJALIZACJA I REJESTRACJA ---

    public void InitializePlayer(CSteamID steamId, int team)
    {
        ulong id = steamId.m_SteamID;
        if (!allPlayerStats.ContainsKey(id))
        {
            allPlayerStats[id] = new PlayerStats
            {
                SteamId = id,
                Name = SteamFriends.GetFriendPersonaName(steamId),
                Team = team,
                IsAlive = true // Na starcie gracz ¿yje
            };

            if (!teamPlanetsAlive.ContainsKey(team))
                teamPlanetsAlive[team] = true;
        }
    }

    // --- LOGIKA STANU GRY (BED WARS) ---

    public void SetPlanetState(int teamId, bool isAlive)
    {
        teamPlanetsAlive[teamId] = isAlive;
        CheckTeamElimination(teamId); // SprawdŸ, czy to zniszczenie wyeliminowa³o dru¿ynê
    }

    public void SetPlayerAliveState(CSteamID steamId, bool isAlive)
    {
        ulong id = steamId.m_SteamID;
        if (allPlayerStats.TryGetValue(id, out PlayerStats stats))
        {
            stats.IsAlive = isAlive;
            if (!isAlive)
            {
                CheckTeamElimination(stats.Team); // SprawdŸ, czy œmieræ gracza wyeliminowa³a dru¿ynê
            }
        }
    }

    public bool IsTeamPlanetAlive(int teamId)
    {
        if (teamPlanetsAlive.TryGetValue(teamId, out bool alive)) return alive;
        return false;
    }

    // Kluczowa funkcja: Sprawdza, czy dru¿yna definitywnie przegra³a
    private void CheckTeamElimination(int teamId)
    {
        // 1. Jeœli planeta ¿yje -> Dru¿yna jest bezpieczna
        if (IsTeamPlanetAlive(teamId)) return;

        // 2. Jeœli planeta zniszczona, sprawdzamy czy ¿yje jakikolwiek gracz z tej dru¿yny
        bool anyPlayerAlive = allPlayerStats.Values
            .Where(p => p.Team == teamId)
            .Any(p => p.IsAlive);

        if (anyPlayerAlive) return; // Ktoœ jeszcze walczy statkiem -> Dru¿yna gra dalej

        // 3. Brak planety + Brak ¿ywych graczy -> ELIMINACJA
        if (!eliminationOrder.Contains(teamId))
        {
            eliminationOrder.Add(teamId);
            Debug.Log($"StatsManager: Dru¿yna {teamId} zosta³a wyeliminowana!");

            CheckWinCondition(); // SprawdŸ, czy gra siê skoñczy³a
        }
    }

    private void CheckWinCondition()
    {
        HashSet<int> allTeams = new HashSet<int>();
        foreach (var p in allPlayerStats.Values) allTeams.Add(p.Team);

        int totalTeams = allTeams.Count;
        // Zabezpieczenie na wypadek testów solo
        if (totalTeams < 2) totalTeams = 2;

        int eliminatedTeamsCount = eliminationOrder.Count;

        // WARUNEK ZWYCIÊSTWA: Zosta³a 1 (lub 0 w przypadku remisu) dru¿yna
        if (totalTeams - eliminatedTeamsCount <= 1)
        {
            int winner = -1;
            // Szukamy dru¿yny, której nie ma na liœcie wyeliminowanych
            foreach (int team in allTeams)
            {
                if (!eliminationOrder.Contains(team))
                {
                    winner = team;
                    break;
                }
            }

            Debug.Log($"GAME OVER! Wygrywa dru¿yna: {winner}");

            // 1. Oblicz statystyki
            FinalizeStats(winner, eliminationOrder);

            // 2. Wyzwól koniec gry w SpawnManagerze (który tylko wyœle pakiet)
            GameSpawnManager.Instance?.TriggerGameEnd(winner);
        }
    }

    // --- REJESTRACJA ZDARZEÑ ---

    public void RecordKill(CSteamID killerId, CSteamID victimId, float victimMaxHealth)
    {
        ulong killId = killerId.m_SteamID;
        ulong vicId = victimId.m_SteamID;
        bool isFinalKill = false;

        if (allPlayerStats.TryGetValue(vicId, out PlayerStats victimStats))
        {
            // Jeœli planeta ofiary nie ¿yje -> To jest Final Kill
            if (!IsTeamPlanetAlive(victimStats.Team))
                isFinalKill = true;
        }

        if (allPlayerStats.ContainsKey(killId))
        {
            if (isFinalKill)
                allPlayerStats[killId].FinalKills++;
            else
                allPlayerStats[killId].Kills++;
        }

        // Asysty...
        float assistThreshold = victimMaxHealth * 0.50f;
        foreach (var attackerStats in allPlayerStats.Values)
        {
            if (attackerStats.SteamId == killId) continue;
            if (attackerStats.DamageContributions.TryGetValue(vicId, out float dmg) && dmg >= assistThreshold)
            {
                attackerStats.Assists++;
            }
        }

        // Czyœæ dmg log
        foreach (var stats in allPlayerStats.Values)
            if (stats.DamageContributions.ContainsKey(vicId)) stats.DamageContributions.Remove(vicId);
    }

    public void RecordPlanetKill(CSteamID killerId)
    {
        if (killerId.IsValid() && allPlayerStats.ContainsKey(killerId.m_SteamID))
            allPlayerStats[killerId.m_SteamID].PlanetsDestroyed++;
    }

    public void RecordDeath(CSteamID victimId)
    {
        if (allPlayerStats.ContainsKey(victimId.m_SteamID))
            allPlayerStats[victimId.m_SteamID].Deaths++;

        // WA¯NE: Oznaczamy, ¿e gracz fizycznie zgin¹³
        SetPlayerAliveState(victimId, false);
    }

    public void RecordRespawn(CSteamID playerId)
    {
        // WA¯NE: Oznaczamy, ¿e gracz wróci³ do gry
        SetPlayerAliveState(playerId, true);
    }

    public void RecordPlanetDamage(CSteamID attackerId, float damage)
    {
        if (attackerId.IsValid() && allPlayerStats.ContainsKey(attackerId.m_SteamID))
            allPlayerStats[attackerId.m_SteamID].TotalDamageDealt += damage;
    }

    public void RecordPlayerDamage(CSteamID attackerId, CSteamID victimId, float damage, float maxHp)
    {
        if (!attackerId.IsValid() || attackerId == victimId) return;
        ulong attId = attackerId.m_SteamID;
        if (allPlayerStats.TryGetValue(attId, out PlayerStats stats))
        {
            stats.TotalDamageDealt += damage;
            if (!stats.DamageContributions.ContainsKey(victimId.m_SteamID)) stats.DamageContributions[victimId.m_SteamID] = 0;
            stats.DamageContributions[victimId.m_SteamID] += damage;
        }
    }

    // --- FINALIZACJA I PLACEMENT ---

    public void FinalizeStats(int winningTeamId, List<int> eliminatedTeamsOrder)
    {
        WinningTeam = winningTeamId;
        teamPlacements.Clear();

        // Policz ile unikalnych dru¿yn w ogóle gra³o
        HashSet<int> participatingTeams = new HashSet<int>();
        foreach (var p in allPlayerStats.Values) participatingTeams.Add(p.Team);
        int totalTeams = participatingTeams.Count;

        // Zabezpieczenie: jeœli gra³o mniej ni¿ 2 teamy (testy), zak³adamy 2 dla logiki miejsc
        if (totalTeams < 2) totalTeams = 2;

        // Pêtla przydzielania miejsc PRZEGRANYM
        // i=0 to pierwszy wyeliminowany (ostatnie miejsce)
        for (int i = 0; i < eliminatedTeamsOrder.Count; i++)
        {
            int teamId = eliminatedTeamsOrder[i];

            // Logika: 
            // Total = 2. i=0. Place = 2 - 0 = 2. (2nd place) - POPRAWNE
            // Total = 4. i=0. Place = 4 - 0 = 4. (4th place) - POPRAWNE
            // Total = 4. i=2. Place = 4 - 2 = 2. (2nd place) - POPRAWNE
            int place = totalTeams - i;
            teamPlacements[teamId] = place;
        }

        // Przydzielenie 1 miejsca ZWYCIÊZCZY
        if (winningTeamId != -1)
        {
            teamPlacements[winningTeamId] = 1;
        }

        // Zaktualizuj poszczególnych graczy
        foreach (var stats in allPlayerStats.Values)
        {
            if (teamPlacements.TryGetValue(stats.Team, out int place))
            {
                stats.Placement = place;
            }
            // Jeœli z jakiegoœ powodu gracza nie ma w placementach (np. b³¹d), daj 0
            else
            {
                stats.Placement = 0;
            }
        }
    }

    public void LoadStatsFromHost(int winningTeam, List<PlayerStats> receivedStats)
    {
        Debug.Log($"StatsManager: Nadpisywanie statystyk. Zwyciêzca: {winningTeam}");

        if (receivedStats == null)
        {
            Debug.LogError("B£¥D KRYTYCZNY: Otrzymano puste statystyki (null) od Hosta!");
            return;
        }

        this.WinningTeam = winningTeam;
        allPlayerStats.Clear();

        foreach (var s in receivedStats)
        {
            // Poniewa¿ Dictionary nie przechodzi przez sieæ, musimy go zainicjowaæ rêcznie,
            // ¿eby unikn¹æ b³êdów, gdyby ktoœ próbowa³ go u¿yæ póŸniej.
            s.DamageContributions = new Dictionary<ulong, float>();

            allPlayerStats[s.SteamId] = s;
        }

        Debug.Log($"Pomyœlnie za³adowano statystyki dla {receivedStats.Count} graczy.");
    }

    public List<PlayerStats> GetAllStats() => allPlayerStats.Values.ToList();
}