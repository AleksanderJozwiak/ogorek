using UnityEngine;
using Steamworks;

public class SteamNetworkingManager : MonoBehaviour
{
    public static SteamNetworkingManager Instance;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        while (SteamNetworking.IsP2PPacketAvailable(out uint msgSize))
        {
            byte[] buffer = new byte[msgSize];
            if (SteamNetworking.ReadP2PPacket(buffer, msgSize, out uint bytesRead, out CSteamID remoteId))
            {
                PacketType type = (PacketType)buffer[0];
                byte[] data = new byte[buffer.Length - 1];
                System.Buffer.BlockCopy(buffer, 1, data, 0, data.Length);

                switch (type)
                {
                    case PacketType.PlayerState:
                    {
                        var state = NetworkHelpers.BytesToStruct<PlayerStateMessage>(data);
                        RemotePlayerManager.Instance?.UpdateRemotePlayer(state);
                        break;
                    }

                    case PacketType.Shoot:
                    {
                        var shoot = NetworkHelpers.BytesToStruct<ShootMessage>(data);
                        RemotePlayerManager.Instance?.SpawnRemoteBullet(shoot);
                        break;
                    }

                    case PacketType.TeamBaseDestroyed:
                    {
                        TeamBaseMessage msg = NetworkHelpers.BytesToStruct<TeamBaseMessage>(data);
                        Debug.Log($"Received TeamBaseDestroyed: Team {msg.teamNumber}, Alive: {msg.baseAlive}");
                        GameSpawnManager.Instance?.SetTeamBaseState(msg.teamNumber, msg.baseAlive);
                        break;
                    }

                    case PacketType.PlayerHit:
                    {
                        PlayerHitMessage hit = NetworkHelpers.BytesToStruct<PlayerHitMessage>(data);
                        RemotePlayerManager.Instance?.ShowHitEffect(hit);
                        break;
                    }
                }
            }
        }
    }
}