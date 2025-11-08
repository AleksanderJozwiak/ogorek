using UnityEngine;
using Steamworks;

public class SteamNetworkingManager : MonoBehaviour
{
    public static SteamNetworkingManager Instance;

    protected Callback<LobbyChatUpdate_t> m_LobbyChatUpdate;
    protected Callback<P2PSessionRequest_t> m_P2PSessionRequest;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        m_LobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        m_P2PSessionRequest = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
    }

    void OnP2PSessionRequest(P2PSessionRequest_t request)
    {
        SteamNetworking.AcceptP2PSessionWithUser(request.m_steamIDRemote);
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
    {
        if (callback.m_ulSteamIDLobby != LobbyManager.Instance.currentLobby.m_SteamID)
            return;

        if ((callback.m_rgfChatMemberStateChange & (uint)EChatMemberStateChange.k_EChatMemberStateChangeLeft) != 0 ||
            (callback.m_rgfChatMemberStateChange & (uint)EChatMemberStateChange.k_EChatMemberStateChangeDisconnected) != 0 ||
            (callback.m_rgfChatMemberStateChange & (uint)EChatMemberStateChange.k_EChatMemberStateChangeKicked) != 0)
        {
            CSteamID userChangedId = new CSteamID(callback.m_ulSteamIDUserChanged);

            Debug.Log($"Gracz {userChangedId} opuœci³ lobby.");

            if (RemotePlayerManager.Instance != null)
            {
                RemotePlayerManager.Instance.RemoveRemotePlayer(userChangedId.m_SteamID);
            }

            if (GameSpawnManager.Instance != null)
            {
                GameSpawnManager.Instance.HandlePlayerLeave(userChangedId);
            }
        }
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

                    case PacketType.AsteroidSpawn:
                    {
                        AsteroidSpawnMessage msg = NetworkHelpers.BytesToStruct<AsteroidSpawnMessage>(data);
                        GameObject asteroid = PoolManager.Instance.PoolMap[PoolCategory.Asteroids].Get();
                        asteroid.transform.position = new Vector3(msg.posX, msg.posY, 0f);
                        asteroid.GetComponent<Asteroid>().SetDirection(new Vector3(msg.dirX, msg.dirY, 0f));
                        asteroid.SetActive(true);
                        break;
                    }

                    case PacketType.GameEnd:
                    {
                        // Klient otrzymuje wiadomoœæ i przechodzi do sceny statystyk
                        // Mo¿esz tu zapisaæ dane o zwyciêzcy, jeœli s¹ potrzebne
                        // GameEndMessage msg = NetworkHelpers.BytesToStruct<GameEndMessage>(data);
                        Debug.Log("Otrzymano GameEnd. £adowanie sceny Statistics...");
                        LobbyManager.Instance.LoadScene("Statistics");
                        break;
                    }
                }
            }
        }
    }

    public void BroadcastPacket(byte[] packet, EP2PSend sendType)
    {
        foreach (CSteamID member in LobbyManager.Instance.GetAllLobbyMembers())
        {
            if (member == SteamUser.GetSteamID()) continue; // Nie wysy³aj do siebie
            SteamNetworking.SendP2PPacket(member, packet, (uint)packet.Length, sendType);
        }
    }
}