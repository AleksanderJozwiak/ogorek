using UnityEngine;
using Steamworks;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints; // assign in Inspector

    void Start()
    {
        int spawnIndex = Random.Range(0, spawnPoints.Length);
        Instantiate(playerPrefab, spawnPoints[spawnIndex].position, Quaternion.identity);
    }
}
