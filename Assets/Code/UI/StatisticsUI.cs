using UnityEngine;
using TMPro;
using Steamworks;
using System.Collections.Generic;
using System.Linq;

public class StatisticsUI : MonoBehaviour
{
    [Header("UI Fields - Player")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text placeText;
    [SerializeField] private TMP_Text destroyedPlanetsText;
    [SerializeField] private TMP_Text killsText;
    [SerializeField] private TMP_Text assistsText;
    [SerializeField] private TMP_Text deathsText;
    [SerializeField] private TMP_Text damageText;

    //[Header("UI Fields - Game")]
    //[SerializeField] private TMP_Text winningTeamText; // Do wyœwietlenia, która dru¿yna wygra³a

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 1. SprawdŸ, czy StatsManager istnieje
        if (StatsManager.Instance == null)
        {
            Debug.LogError("StatsManager nie zosta³ znaleziony! Nie mo¿na wyœwietliæ statystyk.");
            // Opcjonalnie: Poka¿ b³¹d w UI
            playerNameText.text = "ERROR";
            placeText.text = "N/A";
            //winningTeamText.text = "STATS NOT FOUND";
            return;
        }

        // 2. Pobierz ID lokalnego gracza
        ulong localSteamId = SteamUser.GetSteamID().m_SteamID;

        // 3. Pobierz wszystkie statystyki
        List<PlayerStats> allStats = StatsManager.Instance.GetAllStats();
        if (allStats == null || allStats.Count == 0)
        {
            Debug.LogError("StatsManager nie ma ¿adnych statystyk do wyœwietlenia.");
            return;
        }

        // 4. ZnajdŸ statystyki lokalnego gracza
        PlayerStats localStats = allStats.Find(stats => stats.SteamId == localSteamId);

        // 5. Wype³nij pola tekstowe statystykami lokalnego gracza
        if (localStats != null)
        {
            playerNameText.text = localStats.Name;
            placeText.text = GetOrdinal(localStats.Placement); // U¿yj helpera do formatowania "7TH"
            destroyedPlanetsText.text = localStats.PlanetsDestroyed.ToString();
            killsText.text = localStats.Kills.ToString();
            assistsText.text = localStats.Assists.ToString();
            deathsText.text = localStats.Deaths.ToString();
            damageText.text = localStats.TotalDamageDealt.ToString("F0"); // "F0" formatuje liczbê bez miejsc po przecinku
        }
        else
        {
            Debug.LogError($"Nie znaleziono statystyk dla lokalnego gracza {localSteamId}");
            playerNameText.text = "STATS NOT FOUND";
        }

        //// 6. Wyœwietl informacjê o zwyciêzcy gry
        //int winningTeam = StatsManager.Instance.WinningTeam;
        //if (winningTeam > 0)
        //{
        //    winningTeamText.text = $"TEAM {winningTeam} WINS!";
        //}
        //else if (winningTeam == -1) // -1 to nasz kod na remis
        //{
        //    winningTeamText.text = "DRAW!";
        //}
        //else
        //{
        //    winningTeamText.text = "GAME OVER";
        //}
    }

    private string GetOrdinal(int num)
    {
        if (num == 0) num = 1;
        if (num <= 0) return num.ToString(); 

        switch (num % 100)
        {
            case 11:
            case 12:
            case 13:
                return num + "TH";
        }

        switch (num % 10)
        {
            case 1: return num + "ST";
            case 2: return num + "ND";
            case 3: return num + "RD";
            default: return num + "TH";
        }
    }
}