using Steamworks;
using UnityEngine;
using System.Runtime.InteropServices;
using System;

public static class SteamNetworkManager
{
    private static void SendMessage<T>(T msg, PacketType type) where T : struct
    {
        byte[] body = NetworkHelpers.StructToBytes(msg);
        byte[] buffer = new byte[body.Length + 1];
        buffer[0] = (byte)type;
        System.Buffer.BlockCopy(body, 0, buffer, 1, body.Length);

        int memberCount = SteamMatchmaking.GetNumLobbyMembers(LobbyManager.Instance.currentLobby);
        for (int i = 0; i < memberCount; i++)
        {
            CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(LobbyManager.Instance.currentLobby, i);
            if (member == SteamUser.GetSteamID()) continue;

            SteamNetworking.SendP2PPacket(member, buffer, (uint)buffer.Length, EP2PSend.k_EP2PSendUnreliable);
        }
    }

    public static void SendPlayerState(PlayerStateMessage msg) => SendMessage(msg, PacketType.PlayerState);
    public static void SendShoot(ShootMessage msg) => SendMessage(msg, PacketType.Shoot);
    public static void SendPlanetSpawn(PlanetSpawnMessage msg) => SendMessage(msg, PacketType.PlanetSpawn);
    public static void SendAsteroidSpawn(AsteroidSpawnMessage msg) => SendMessage(msg, PacketType.AsteroidSpawn);


}
