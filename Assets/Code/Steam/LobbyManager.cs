using System;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using System.Net.NetworkInformation;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    // max 18 players as requested
    private const int MAX_PLAYERS = 18;

    // Callbacks
    protected Callback<LobbyCreated_t> m_LobbyCreated;
    protected Callback<LobbyEnter_t> m_LobbyEntered;
    protected Callback<LobbyMatchList_t> m_LobbyMatchList;
    protected Callback<GameLobbyJoinRequested_t> m_GameLobbyJoinRequested;
    protected Callback<LobbyDataUpdate_t> m_LobbyDataUpdated;

    private List<CSteamID> foundLobbies = new List<CSteamID>();
    public CSteamID currentLobby = CSteamID.Nil;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!SteamManager.Instance.IsInitialized) return;

        m_LobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        m_LobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        m_LobbyMatchList = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);
        m_GameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        m_LobbyDataUpdated = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);
    }

    #region Create / Leave
    public void CreateLobby(bool friendsOnly = false)
    {
        ELobbyType type = friendsOnly ? ELobbyType.k_ELobbyTypeFriendsOnly : ELobbyType.k_ELobbyTypePublic;
        SteamMatchmaking.CreateLobby(type, MAX_PLAYERS);
        Debug.Log("Tworzenie lobby...");
    }

    public void LeaveLobby()
    {
        if (currentLobby != CSteamID.Nil)
        {
            SteamMatchmaking.LeaveLobby(currentLobby);
            string players = SteamMatchmaking.GetLobbyData(currentLobby, "players");
            SteamMatchmaking.SetLobbyData(currentLobby, "players", $"{int.Parse(players) - 1}");
            currentLobby = CSteamID.Nil;
            Debug.Log("Opuszczono lobby.");
        }
    }
    #endregion

    #region Callbacks
    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("Błąd tworzenia lobby: " + callback.m_eResult);
            return;
        }

        currentLobby = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log("Lobby utworzone: " + currentLobby);
        string nickname = SteamFriends.GetPersonaName();

        // Ustaw podstawowe dane lobby — będą widoczne w liście
        SteamMatchmaking.SetLobbyData(currentLobby, "name", $"Host_{nickname}");
        SteamMatchmaking.SetLobbyData(currentLobby, "host_name", SteamFriends.GetPersonaName());
        SteamMatchmaking.SetLobbyData(currentLobby, "version", Application.version);
        
        // id aby oddzielić od innych devów
        SteamMatchmaking.SetLobbyData(currentLobby, "game_key", "2abbddfd-1dbd-4eff-a4a9-07cabc02b32e");
        SteamMatchmaking.SetLobbyData(currentLobby, "players", "0");
        // Możesz dodać dowolne inne pola, np mapę, tryb itp.
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        currentLobby = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log("Dołączono do lobby: " + currentLobby + " (owner? " + SteamMatchmaking.GetLobbyOwner(currentLobby) + ")");
        // Możesz pobrać dane lobby:
        string name = SteamMatchmaking.GetLobbyData(currentLobby, "name");
        string players = SteamMatchmaking.GetLobbyData(currentLobby, "players");
        SteamMatchmaking.SetLobbyData(currentLobby, "players", $"{int.Parse(players) + 1}");
        Debug.Log("Lobby name: " + name);

        // np. wywołaj tutaj inicjalizację sieci (host/clients)
    }

    private void OnLobbyMatchList(LobbyMatchList_t callback)
    {
        foundLobbies.Clear();
        int count = (int)callback.m_nLobbiesMatching;
        Debug.Log("Znaleziono lobby: " + count);
        for (int i = 0; i < count; i++)
        {
            CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
            foundLobbies.Add(lobbyId);
        }
        // Wywołaj UI, aby wyświetlić foundLobbies i metadane
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        // Odbiorca zaproszenia — Steam wyśle to gdy kliknie "Join"
        Debug.Log("Otrzymano zaproszenie/żądanie dołączenia do lobby: " + callback.m_steamIDLobby);
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyDataUpdate(LobbyDataUpdate_t callback)
    {
        // Przydatne, gdy zmienia się metadata
        CSteamID lobby = new CSteamID(callback.m_ulSteamIDLobby);
        if (callback.m_bSuccess != 0)
        {
            Debug.Log("Lobby data updated: " + lobby);
        }
    }
    #endregion

    #region Search / Join / Invite
    public void RequestLobbyList()
    {
        SteamMatchmaking.AddRequestLobbyListStringFilter(
            "game_key", "2abbddfd-1dbd-4eff-a4a9-07cabc02b32e", ELobbyComparison.k_ELobbyComparisonEqual
        );

        SteamMatchmaking.RequestLobbyList();
        Debug.Log("RequestLobbyList wysłane z filtrem. (2abbddfd-1dbd-4eff-a4a9-07cabc02b32e)");
    }

    public List<CSteamID> GetFoundLobbies() => new List<CSteamID>(foundLobbies);

    public void JoinLobby(CSteamID lobbyId)
    {
        SteamMatchmaking.JoinLobby(lobbyId);
        Debug.Log("Próba dołączenia do lobby: " + lobbyId);
    }

    public void InviteFriendToLobby(CSteamID friendSteamId)
    {
        if (currentLobby == CSteamID.Nil)
        {
            Debug.LogWarning("Nie jesteś w lobby — nie można zaprosić.");
            return;
        }
        SteamMatchmaking.InviteUserToLobby(currentLobby, friendSteamId);
        Debug.Log("Wysłano zaproszenie do " + friendSteamId);
    }

    // Alternatywnie możesz otworzyć overlay z listą zaproszeń (jeżeli dostępne)
    public void OpenOverlayInviteDialog()
    {
        if (currentLobby == CSteamID.Nil) return;
        // Steam overlay ma funkcję invite dialog; jeśli nie działa w edytorze, trzeba testować po buildzie.
        SteamFriends.ActivateGameOverlayInviteDialog(currentLobby);
    }
    #endregion
}
