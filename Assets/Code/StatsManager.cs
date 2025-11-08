using UnityEngine;
using Steamworks;
using System.Collections.Generic;
using System.Linq;

// Struktura przechowuj¹ca statystyki dla jednego gracza
public class PlayerStats
{
    public ulong SteamId;
    public string Name;
    public int Team;

    public int PlanetsDestroyed = 0;
    public int Kills = 0;
    public int Assists = 0;
    public int Deaths = 0;
    public float TotalDamageDealt = 0;
    public int Placement = 0;

    // S³ownik do œledzenia obra¿eñ zadanych konkretnym ofiarom (na potrzeby asyst)
    // Key: victimSteamId, Value: damageDealt
    public Dictionary<ulong, float> DamageContributions = new Dictionary<ulong, float>();
}

// Singleton DontDestroyOnLoad do zarz¹dzania statystykami
public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance;

    // S³ownik wszystkich graczy i ich statystyk
    private Dictionary<ulong, PlayerStats> allPlayerStats = new Dictionary<ulong, PlayerStats>();

    // Ostateczne wyniki gry
    public int WinningTeam { get; private set; } = -1;
    private Dictionary<int, int> teamPlacements = new Dictionary<int, int>();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Wywo³ywane, gdy gracz (lokalny lub zdalny) jest tworzony
    public void InitializePlayer(CSteamID steamId, int team)
    {
        ulong id = steamId.m_SteamID;
        if (!allPlayerStats.ContainsKey(id))
        {
            allPlayerStats[id] = new PlayerStats
            {
                SteamId = id,
                Name = SteamFriends.GetFriendPersonaName(steamId),
                Team = team
            };
            Debug.Log($"StatsManager: Zarejestrowano gracza {allPlayerStats[id].Name} (Team {team})");
        }
    }

    // Rejestruje obra¿enia zadane innemu graczowi
    public void RecordPlayerDamage(CSteamID attackerId, CSteamID victimId, float damage, float victimMaxHealth)
    {
        if (!attackerId.IsValid() || attackerId == victimId) return;

        ulong attId = attackerId.m_SteamID;
        ulong vicId = victimId.m_SteamID;

        if (!allPlayerStats.ContainsKey(attId)) return; // Atakuj¹cy nie jest zarejestrowany?

        // 1. Zwiêksz ³¹czne obra¿enia atakuj¹cego
        allPlayerStats[attId].TotalDamageDealt += damage;

        // 2. ŒledŸ obra¿enia na potrzeby asyst
        if (!allPlayerStats[attId].DamageContributions.ContainsKey(vicId))
        {
            allPlayerStats[attId].DamageContributions[vicId] = 0;
        }
        allPlayerStats[attId].DamageContributions[vicId] += damage;
    }

    // Rejestruje obra¿enia zadane planecie
    public void RecordPlanetDamage(CSteamID attackerId, float damage)
    {
        if (!attackerId.IsValid()) return;
        ulong attId = attackerId.m_SteamID;

        if (!allPlayerStats.ContainsKey(attId)) return;
        allPlayerStats[attId].TotalDamageDealt += damage;
    }

    // Rejestruje zabójstwo gracza
    public void RecordKill(CSteamID killerId, CSteamID victimId, float victimMaxHealth)
    {
        ulong killId = killerId.m_SteamID;
        ulong vicId = victimId.m_SteamID;

        if (allPlayerStats.ContainsKey(killId))
        {
            // 1. Zwiêksz liczbê zabójstw
            allPlayerStats[killId].Kills++;
            Debug.Log($"StatsManager: {allPlayerStats[killId].Name} zdoby³ KILLA!");
        }

        // 2. SprawdŸ asysty
        // Próg 60% (3 z 5 to 60%)
        float assistThreshold = victimMaxHealth * 0.60f;

        foreach (var attackerStats in allPlayerStats.Values)
        {
            // Pomijamy zabójcê
            if (attackerStats.SteamId == killId) continue;

            if (attackerStats.DamageContributions.TryGetValue(vicId, out float damageDealt))
            {
                if (damageDealt >= assistThreshold)
                {
                    attackerStats.Assists++;
                    Debug.Log($"StatsManager: {attackerStats.Name} zdoby³ ASYSTÊ!");
                }
            }
        }

        // 3. Wyczyœæ wk³ad obra¿eñ dla tej ofiary u wszystkich
        foreach (var stats in allPlayerStats.Values)
        {
            if (stats.DamageContributions.ContainsKey(vicId))
            {
                stats.DamageContributions.Remove(vicId);
            }
        }
    }

    // Rejestruje zniszczenie planety
    public void RecordPlanetKill(CSteamID killerId)
    {
        if (!killerId.IsValid()) return;
        ulong killId = killerId.m_SteamID;

        if (allPlayerStats.ContainsKey(killId))
        {
            allPlayerStats[killId].PlanetsDestroyed++;
            Debug.Log($"StatsManager: {allPlayerStats[killId].Name} zniszczy³ PLANETÊ!");
        }
    }

    // Rejestruje œmieræ gracza
    public void RecordDeath(CSteamID victimId)
    {
        ulong vicId = victimId.m_SteamID;
        if (allPlayerStats.ContainsKey(vicId))
        {
            allPlayerStats[vicId].Deaths++;
        }
    }

    // Zapisuje ostateczne wyniki gry
    public void FinalizeStats(int winningTeam, List<int> placementList)
    {
        this.WinningTeam = winningTeam;

        // Wyczyœæ stare miejsca
        teamPlacements.Clear();

        // Lista `placementList` zawiera przegranych, od ostatniego miejsca (index 0)
        // do drugiego miejsca (index N-1)
        int currentPlace = placementList.Count + 1;
        foreach (int team in placementList)
        {
            teamPlacements[team] = currentPlace;
            currentPlace--;
        }

        // Zwyciêzca jest na 1. miejscu
        if (winningTeam > 0)
        {
            teamPlacements[winningTeam] = 1;
        }

        // Zaktualizuj statystyki ka¿dego gracza o jego miejsce
        foreach (var stats in allPlayerStats.Values)
        {
            if (teamPlacements.TryGetValue(stats.Team, out int place))
            {
                stats.Placement = place;
            }
        }
    }

    // Pobiera wszystkie statystyki (do u¿ycia w scenie Statistics)
    public List<PlayerStats> GetAllStats()
    {
        return allPlayerStats.Values.ToList();
    }
}