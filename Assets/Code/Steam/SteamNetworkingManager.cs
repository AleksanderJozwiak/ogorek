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
        uint msgSize;
        while (SteamNetworking.IsP2PPacketAvailable(out msgSize))
        {
            byte[] buffer = new byte[msgSize];
            uint bytesRead;
            CSteamID remoteId;
            if (SteamNetworking.ReadP2PPacket(buffer, msgSize, out bytesRead, out remoteId))
            {
                // First byte = message type
                PacketType type = (PacketType)buffer[0];
                byte[] data = new byte[buffer.Length - 1];
                System.Buffer.BlockCopy(buffer, 1, data, 0, data.Length);

                switch (type)
                {
                    case PacketType.PlayerState:
                        var state = NetworkHelpers.BytesToStruct<PlayerStateMessage>(data);
                        RemotePlayerManager.Instance.UpdateRemotePlayer(state);
                        break;

                    case PacketType.Shoot:
                        var shoot = NetworkHelpers.BytesToStruct<ShootMessage>(data);
                        RemotePlayerManager.Instance.SpawnRemoteBullet(shoot);
                        break;
                }
            }
        }
    }

}
