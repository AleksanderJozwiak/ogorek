using System.Runtime.InteropServices;
using System;

public enum PacketType : byte
{
    PlayerState = 1,
    Shoot = 2,
    Planet = 3,
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