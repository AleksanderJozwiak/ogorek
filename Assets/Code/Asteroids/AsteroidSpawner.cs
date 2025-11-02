using UnityEngine;
using Steamworks;

public class AsteroidSpawner : MonoBehaviour
{
    [SerializeField] private float spawnRate = 2f;
    private float timer;

    void Update()
    {
        if (!LobbyManager.Instance.IsHost) return; 

        timer += Time.deltaTime;
        if (timer >= spawnRate)
        {
            timer = 0f;

            GameObject asteroid = PoolManager.Instance.PoolMap[PoolCategory.Asteroids].Get();
            asteroid.SetActive(true);

            Vector3 pos = asteroid.transform.position;
            Vector3 dir = asteroid.GetComponent<Asteroid>().Direction;

            AsteroidSpawnMessage msg = new AsteroidSpawnMessage
            {
                posX = pos.x,
                posY = pos.y,
                dirX = dir.x,
                dirY = dir.y
            };

            byte[] data = NetworkHelpers.StructToBytes(msg);
            byte[] packet = new byte[data.Length + 1];
            packet[0] = (byte)PacketType.AsteroidSpawn;
            System.Buffer.BlockCopy(data, 0, packet, 1, data.Length);


            foreach (CSteamID member in LobbyManager.Instance.GetAllLobbyMembers())
            {
                if (member != SteamUser.GetSteamID())
                    SteamNetworking.SendP2PPacket(member, packet, (uint)packet.Length, EP2PSend.k_EP2PSendReliable);
            }
        }
    }
}

