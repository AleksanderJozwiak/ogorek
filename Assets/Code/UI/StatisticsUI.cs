using UnityEngine;
using TMPro;
using Steamworks;
using System.Collections.Generic;

public class StatisticsUI : MonoBehaviour
{
    [Header("UI Fields - Player")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text placeText;
    [SerializeField] private TMP_Text destroyedPlanetsText;
    [SerializeField] private TMP_Text killsText;
    [SerializeField] private TMP_Text finalKillsText; // NOWE POLE
    [SerializeField] private TMP_Text assistsText;
    [SerializeField] private TMP_Text deathsText;
    [SerializeField] private TMP_Text damageText;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (StatsManager.Instance == null) return;

        ulong localSteamId = SteamUser.GetSteamID().m_SteamID;
        List<PlayerStats> allStats = StatsManager.Instance.GetAllStats();
        PlayerStats localStats = allStats.Find(stats => stats.SteamId == localSteamId);

        if (localStats != null)
        {
            playerNameText.text = localStats.Name;
            placeText.text = GetOrdinal(localStats.Placement);
            destroyedPlanetsText.text = localStats.PlanetsDestroyed.ToString();

            killsText.text = localStats.Kills.ToString();
            // Wyœwietlamy Final Kills (jeœli nie masz miejsca w UI, mo¿esz po³¹czyæ to jako "Kills (Finals)")
            if (finalKillsText != null)
                finalKillsText.text = localStats.FinalKills.ToString();

            assistsText.text = localStats.Assists.ToString();
            deathsText.text = localStats.Deaths.ToString();
            damageText.text = localStats.TotalDamageDealt.ToString("F0");
        }
        else
        {
            playerNameText.text = "Error";
        }
    }

    private string GetOrdinal(int num)
    {
        if (num <= 0) return "-"; // Jeœli gracz nie ma miejsca (np. wyszed³), myœlnik

        switch (num % 100)
        {
            case 11: case 12: case 13: return num + "TH";
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