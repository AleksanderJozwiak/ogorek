using UnityEngine;
using Steamworks;

public class SteamManager : MonoBehaviour
{
    public static SteamManager Instance { get; private set; }
    public bool IsInitialized { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        try
        {
            if (!SteamAPI.Init())
            {
                Debug.LogError("SteamAPI.Init() failed. Upewnij siê, ¿e Steam dzia³a i steam_appid.txt = 480.");
                IsInitialized = false;
                return;
            }
            IsInitialized = true;
            Debug.Log("SteamAPI zainicjalizowane. SteamID: " + SteamUser.GetSteamID());
        }
        catch (System.Exception e)
        {
            Debug.LogError("Steam Init exception: " + e);
            IsInitialized = false;
        }
    }

    private void Update()
    {
        if (IsInitialized) SteamAPI.RunCallbacks();
    }

    private void OnApplicationQuit()
    {
        if (IsInitialized) SteamAPI.Shutdown();
    }
}
