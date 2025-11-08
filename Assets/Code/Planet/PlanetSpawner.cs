using UnityEngine;
using Steamworks;
using System.Collections.Generic;

public class PlanetSpawner : MonoBehaviour
{
    public List<Transform> teamParents; 
    public GameObject[] teamBasePrefabs;

    void Start()
    {
        SpawnActiveTeamBases();
    }

    void SpawnActiveTeamBases()
    {
        HashSet<int> activeTeams = new();

        int memberCount = SteamMatchmaking.GetNumLobbyMembers(LobbyManager.Instance.currentLobby);

        for (int i = 0; i < memberCount; i++)
        {
            CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(LobbyManager.Instance.currentLobby, i);
            string slot = SteamMatchmaking.GetLobbyMemberData(LobbyManager.Instance.currentLobby, member, "slot");

            if (!string.IsNullOrEmpty(slot))
            {
                string[] parts = slot.Split('_');
                if (parts.Length >= 1 && int.TryParse(parts[0], out int team))
                {
                    activeTeams.Add(team);
                }
            }
        }

        foreach (int team in activeTeams)
        {
            int teamIndex = team - 1;

            if (teamIndex >= 0 && teamIndex < teamParents.Count)
            {
                Transform parent = teamParents[teamIndex];
                GameObject prefab = teamBasePrefabs[teamIndex];

                if (parent != null && prefab != null)
                {
                    GameObject gameObject = Instantiate(prefab, parent.position, Quaternion.identity);
                    gameObject.tag = $"Team_{team}";
                    GameSpawnManager.Instance.SetTeamBaseState(team, true);
                }
            }
        }
    }
}

