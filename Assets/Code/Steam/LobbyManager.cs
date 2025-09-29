using System;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;

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
    public void CreateLobby(string lobbyName, bool friendsOnly = false)
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
            Debug.LogError("B��d tworzenia lobby: " + callback.m_eResult);
            return;
        }

        currentLobby = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log("Lobby utworzone: " + currentLobby);

        // Ustaw podstawowe dane lobby � b�d� widoczne w li�cie
        SteamMatchmaking.SetLobbyData(currentLobby, "name", "SpaceWars Lobby");
        SteamMatchmaking.SetLobbyData(currentLobby, "host_name", SteamFriends.GetPersonaName());
        SteamMatchmaking.SetLobbyData(currentLobby, "version", Application.version);
        SteamMatchmaking.SetLobbyData(currentLobby, "players", "1");
        // Mo�esz doda� dowolne inne pola, np map�, tryb itp.
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        currentLobby = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log("Do��czono do lobby: " + currentLobby + " (owner? " + SteamMatchmaking.GetLobbyOwner(currentLobby) + ")");
        // Mo�esz pobra� dane lobby:
        string name = SteamMatchmaking.GetLobbyData(currentLobby, "name");
        Debug.Log("Lobby name: " + name);

        // np. wywo�aj tutaj inicjalizacj� sieci (host/clients)
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
        // Wywo�aj UI, aby wy�wietli� foundLobbies i metadane
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        // Odbiorca zaproszenia � Steam wy�le to gdy kliknie "Join"
        Debug.Log("Otrzymano zaproszenie/��danie do��czenia do lobby: " + callback.m_steamIDLobby);
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyDataUpdate(LobbyDataUpdate_t callback)
    {
        // Przydatne, gdy zmienia si� metadata
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
        // Mo�esz doda� filtry przez SteamMatchmaking.AddRequestLobbyListFilter... je�li chcesz
        SteamMatchmaking.RequestLobbyList();
        Debug.Log("RequestLobbyList wys�ane.");
    }

    public List<CSteamID> GetFoundLobbies() => new List<CSteamID>(foundLobbies);

    public void JoinLobby(CSteamID lobbyId)
    {
        SteamMatchmaking.JoinLobby(lobbyId);
        Debug.Log("Pr�ba do��czenia do lobby: " + lobbyId);
    }

    public void InviteFriendToLobby(CSteamID friendSteamId)
    {
        if (currentLobby == CSteamID.Nil)
        {
            Debug.LogWarning("Nie jeste� w lobby � nie mo�na zaprosi�.");
            return;
        }
        SteamMatchmaking.InviteUserToLobby(currentLobby, friendSteamId);
        Debug.Log("Wys�ano zaproszenie do " + friendSteamId);
    }

    // Alternatywnie mo�esz otworzy� overlay z list� zaprosze� (je�eli dost�pne)
    public void OpenOverlayInviteDialog()
    {
        if (currentLobby == CSteamID.Nil) return;
        // Steam overlay ma funkcj� invite dialog; je�li nie dzia�a w edytorze, trzeba testowa� po buildzie.
        SteamFriends.ActivateGameOverlayInviteDialog(currentLobby);
    }
    #endregion
}
