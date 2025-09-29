using UnityEngine;
using Steamworks;

public class SteamP2PNetwork : MonoBehaviour
{
    public static SteamP2PNetwork Instance;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // ✅ Don't touch Steam until SteamManager says it's initialized
        if (!SteamManager.Instance || !SteamManager.Instance.IsInitialized)
            return;

        uint msgSize;
        // Safe: only call once Steam is initialized
        while (SteamNetworking.IsP2PPacketAvailable(out msgSize))
        {
            byte[] buffer = new byte[msgSize];
            CSteamID remote;
            if (SteamNetworking.ReadP2PPacket(buffer, msgSize, out uint bytesRead, out remote))
            {
                HandlePacket(remote, buffer, (int)bytesRead);
            }
        }
    }

    private void HandlePacket(CSteamID from, byte[] data, int length)
    {
        if (length < 1) return;
        byte msgType = data[0];
        Debug.Log($"Got packet from {from}, type={msgType}, length={length}");
    }

    public static bool SendPacketTo(CSteamID target, byte[] payload, bool reliable = true, int channel = 0)
    {
        EP2PSend sendType = reliable ? EP2PSend.k_EP2PSendReliable : EP2PSend.k_EP2PSendUnreliable;
        // niektóre przykłady/wersje używają kolejności: (steamID, data, length, sendType, channel)
        return SteamNetworking.SendP2PPacket(target, payload, (uint)payload.Length, sendType, channel);
    }
}


