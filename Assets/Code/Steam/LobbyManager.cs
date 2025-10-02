using System;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    private const int MAX_PLAYERS = 18;

    protected Callback<LobbyCreated_t> m_LobbyCreated;
    protected Callback<LobbyEnter_t> m_LobbyEntered;
    protected Callback<LobbyMatchList_t> m_LobbyMatchList;
    protected Callback<GameLobbyJoinRequested_t> m_GameLobbyJoinRequested;
    protected Callback<LobbyDataUpdate_t> m_LobbyDataUpdated;
    protected Callback<LobbyChatUpdate_t> m_LobbyChatUpdate;

    private List<CSteamID> foundLobbies = new List<CSteamID>();
    public CSteamID currentLobby = CSteamID.Nil;

    public event Action<List<CSteamID>> OnLobbyListUpdated;

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
        m_LobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
    }

    #region Create / Leave
    public void CreateLobby(bool friendsOnly = false)
    {
        ELobbyType type = friendsOnly ? ELobbyType.k_ELobbyTypeFriendsOnly : ELobbyType.k_ELobbyTypePublic;
        SteamMatchmaking.CreateLobby(type, MAX_PLAYERS);
        Debug.Log("Creating lobby...");
    }

    public void LeaveLobby()
    {
        if (currentLobby != CSteamID.Nil)
        {
            SteamMatchmaking.LeaveLobby(currentLobby);
            currentLobby = CSteamID.Nil;
            Debug.Log("Left lobby.");
        }
    }
    #endregion

    #region Callbacks
    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("Lobby creation error: " + callback.m_eResult);
            return;
        }

        currentLobby = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log("Lobby created: " + currentLobby);
        string nickname = SteamFriends.GetPersonaName();

        SteamMatchmaking.SetLobbyData(currentLobby, "name", $"Host_{nickname}");
        SteamMatchmaking.SetLobbyData(currentLobby, "host_name", nickname);
        SteamMatchmaking.SetLobbyData(currentLobby, "version", Application.version);
        SteamMatchmaking.SetLobbyData(currentLobby, "game_key", "2abbddfd-1dbd-4eff-a4a9-07cabc02b32e");

        RefreshTeamUI();
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        currentLobby = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log("Joined lobby: " + currentLobby);

        // Assign local player to first empty slot
        LobbyTeamManager teamManager = FindFirstObjectByType<LobbyTeamManager>();
        if (teamManager != null)
        {
            teamManager.AssignEmptySlotForLocalPlayer();
        }

        RefreshTeamUI();
    }

    private void OnLobbyMatchList(LobbyMatchList_t callback)
    {
        foundLobbies.Clear();
        int count = (int)callback.m_nLobbiesMatching;
        Debug.Log("Found lobbies: " + count);
        for (int i = 0; i < count; i++)
        {
            CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
            foundLobbies.Add(lobbyId);
        }
        OnLobbyListUpdated?.Invoke(new List<CSteamID>(foundLobbies));
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        Debug.Log("Join request received: " + callback.m_steamIDLobby);
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);

        MenuManager menuManager = FindFirstObjectByType<MenuManager>();
        if (menuManager != null)
            menuManager.OpenLobbyPanel();
    }

    private void OnLobbyDataUpdate(LobbyDataUpdate_t callback)
    {
        if (callback.m_ulSteamIDLobby != currentLobby.m_SteamID) return;

        string gameStart = SteamMatchmaking.GetLobbyData(currentLobby, "game_start");
        if (gameStart == "1")
        {
            Debug.Log("Game is starting!");
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
            return;
        }

        RefreshTeamUI();
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
    {
        if (callback.m_ulSteamIDLobby != currentLobby.m_SteamID) return;

        bool someoneLeft = (callback.m_rgfChatMemberStateChange & (uint)EChatMemberStateChange.k_EChatMemberStateChangeDisconnected) != 0 ||
                           (callback.m_rgfChatMemberStateChange & (uint)EChatMemberStateChange.k_EChatMemberStateChangeLeft) != 0;

        if (someoneLeft)
        {
            // Check if host is still present
            string hostName = SteamMatchmaking.GetLobbyData(currentLobby, "host_name");
            bool hostPresent = false;
            int memberCount = SteamMatchmaking.GetNumLobbyMembers(currentLobby);
            for (int i = 0; i < memberCount; i++)
            {
                CSteamID memberId = SteamMatchmaking.GetLobbyMemberByIndex(currentLobby, i);
                if (SteamFriends.GetFriendPersonaName(memberId) == hostName)
                {
                    hostPresent = true;
                    break;
                }
            }

            if (!hostPresent)
            {
                Debug.Log("Host has left the lobby. Leaving lobby...");
                LeaveLobby();

                MenuManager menuManager = FindFirstObjectByType<MenuManager>();
                if (menuManager != null)
                    menuManager.ShowLobbyBrowserAfterHostLeft();
            }
            else
            {
                RefreshTeamUI();
            }
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
        Debug.Log("Requested lobby list.");
    }

    public List<CSteamID> GetFoundLobbies() => new List<CSteamID>(foundLobbies);

    public void JoinLobby(CSteamID lobbyId)
    {
        SteamMatchmaking.JoinLobby(lobbyId);
        Debug.Log("Joining lobby: " + lobbyId);
    }

    public void InviteFriendToLobby(CSteamID friendSteamId)
    {
        if (currentLobby == CSteamID.Nil)
        {
            Debug.LogWarning("Not in lobby, cannot invite.");
            return;
        }
        SteamMatchmaking.InviteUserToLobby(currentLobby, friendSteamId);
        Debug.Log("Sent invite to " + friendSteamId);
    }

    public void OpenOverlayInviteDialog()
    {
        if (currentLobby == CSteamID.Nil) return;
        SteamFriends.ActivateGameOverlayInviteDialog(currentLobby);
    }
    #endregion

    public int GetLobbyPlayerCount(CSteamID lobbyId)
    {
        return SteamMatchmaking.GetNumLobbyMembers(lobbyId);
    }

    private void RefreshTeamUI()
    {
        LobbyTeamManager teamManager = FindFirstObjectByType<LobbyTeamManager>();
        if (teamManager != null)
            teamManager.RefreshTeamUI();
    }
}
