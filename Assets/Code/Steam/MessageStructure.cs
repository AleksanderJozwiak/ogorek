using System.Runtime.InteropServices;
using System;

public enum PacketType : byte
{
    PlayerState = 1,
    Shoot = 2,
    TeamBaseDestroyed = 3,
    PlayerHit = 4,
    AsteroidSpawn = 5,
    GameEnd = 6,
}

[System.Serializable]
public struct GameEndMessage
{
    public int winningTeam;
}

[System.Serializable]
public struct PlayerStateMessage
{
    public ulong steamId;
    public float posX, posY;
    public float rot;
    public float velX, velY;
    public bool emmitingTrail;
    public bool isAlive;
}

[System.Serializable]
public struct ShootMessage
{
    public ulong steamId;
    public float posX, posY;
    public float rot;
    public float dirX, dirY;
}

[System.Serializable]
public struct TeamBaseMessage
{
    public int teamNumber;
    public bool baseAlive;
}

[System.Serializable]
public struct PlayerHitMessage
{
    public ulong steamId;
    public float damage;
}

[System.Serializable]
public struct AsteroidSpawnMessage
{
    public float posX;
    public float posY;
    public float dirX;
    public float dirY;
}

public class NetworkHelpers
{
    public static byte[] StructToBytes<T>(T str) where T : struct
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);
        Marshal.FreeHGlobal(ptr);
        return arr;
    }

    public static T BytesToStruct<T>(byte[] arr) where T : struct
    {
        T str = default;
        int size = Marshal.SizeOf(str);
        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.Copy(arr, 0, ptr, size);
        str = Marshal.PtrToStructure<T>(ptr);
        Marshal.FreeHGlobal(ptr);
        return str;
    }
}