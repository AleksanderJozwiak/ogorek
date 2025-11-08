using UnityEngine;
using Steamworks;
using System.Collections.Generic;

public class PlanetSpawner : MonoBehaviour
{
    public List<Transform> teamParents;
    public GameObject[] teamBasePrefabs;

    [SerializeField] private Transform[] spawnPoints;

    void Start()
    {
        SpawnActiveTeamBases();
    }

    void SpawnActiveTeamBases()
    {

        if (!LobbyManager.Instance.IsHost) return;

        HashSet<int> activeTeams = new();
        List<int> availableSpots = new(spawnPoints.Length);
        for (int i = 0; i < spawnPoints.Length; i++) availableSpots.Add(i);

        int memberCount = SteamMatchmaking.GetNumLobbyMembers(LobbyManager.Instance.currentLobby);


        for (int i = 0; i < memberCount; i++)
        {
            CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(LobbyManager.Instance.currentLobby, i);
            string slot = SteamMatchmaking.GetLobbyMemberData(LobbyManager.Instance.currentLobby, member, "slot");

            if (!string.IsNullOrEmpty(slot))
            {
                string[] parts = slot.Split('_');
                if (parts.Length >= 1 && int.TryParse(parts[0], out int team))
                    activeTeams.Add(team);
            }
        }


        foreach (int team in activeTeams)
        {
            if (availableSpots.Count == 0) break;

            int randomIndex = Random.Range(0, availableSpots.Count);
            int chosenSpawn = availableSpots[randomIndex];
            availableSpots.RemoveAt(randomIndex);

            SpawnPlanet(team, chosenSpawn);


            SteamNetworkManager.SendPlanetSpawn(new PlanetSpawnMessage
            {
                team = team,
                spawnIndex = chosenSpawn 
            });
        }
    }

    public void SpawnPlanet(int team, int spawnIndex)
    {
        if (spawnIndex < 0 || spawnIndex >= teamParents.Count || team <= 0 || team > teamBasePrefabs.Length)
            return;

        Transform parent = teamParents[spawnIndex];
        GameObject prefab = teamBasePrefabs[team - 1];

        if (parent != null && prefab != null)
        {
            GameObject planet = Instantiate(prefab, parent.position, Quaternion.identity);
            planet.tag = $"Team_{team}";
            GameSpawnManager.Instance?.SetTeamBaseState(team, true);
        }
    }
}

