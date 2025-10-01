using Steamworks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Lobby Browser")]
    public Transform lobbyListContainer;
    public GameObject lobbyEntryPrefab;

    public CanvasGroup MainMenuPanel;
    public CanvasGroup LobbyBrowserPanel;
    public CanvasGroup LobbyPanel;

    public Toggle friendsOnly;

    private Coroutine refreshRoutine;

    private void OnEnable()
    {
        refreshRoutine = StartCoroutine(AutoRefreshLobbyList());
    }

    private void OnDisable()
    {
        if (refreshRoutine != null)
        {
            StopCoroutine(refreshRoutine);
            refreshRoutine = null;
        }
    }

    private IEnumerator AutoRefreshLobbyList()
    {
        while (true)
        {
            if (LobbyBrowserPanel.alpha > 0.9f && SteamManager.Instance.IsInitialized)
            {
                LobbyManager.Instance.RequestLobbyList();
                var list = LobbyManager.Instance.GetFoundLobbies();
                RefreshLobbyList(list);
            }
            yield return new WaitForSeconds(5f);
        }
    }

    public void ToogleCanvasGroup(CanvasGroup canvasGroup)
    {
        if (canvasGroup.alpha == 0)
            canvasGroup.alpha = 1;
        else
            canvasGroup.alpha = 0;

        if (canvasGroup.interactable == false)
            canvasGroup.interactable = true;
        else
            canvasGroup.interactable = false;

        if (canvasGroup.blocksRaycasts == false)
            canvasGroup.blocksRaycasts = true;
        else
            canvasGroup.blocksRaycasts = false;
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void CreateLobby()
    {
        LobbyManager.Instance.CreateLobby(friendsOnly.isOn);
    }

    private void RefreshLobbyList(List<CSteamID> lobbies)
    {
        foreach (Transform child in lobbyListContainer) Destroy(child.gameObject);

        foreach (var lobbyId in lobbies)
        {
            var go = Instantiate(lobbyEntryPrefab, lobbyListContainer);
            var texts = go.GetComponentsInChildren<TMP_Text>();
            var btn = go.GetComponent<Button>();

            string name = SteamMatchmaking.GetLobbyData(lobbyId, "name");
            string players = SteamMatchmaking.GetLobbyData(lobbyId, "players");

            var lobbName = texts.FirstOrDefault(x => x.name == "HostName");
            var playerCount = texts.FirstOrDefault(x => x.name == "PlayerCount");
            lobbName.text = $"{name}";
            playerCount.text = $"{players}/18";

            btn.onClick.AddListener(() => LobbyManager.Instance.JoinLobby(lobbyId));
        }
    }

    void Update()
    {
        if (SteamManager.Instance != null && SteamManager.Instance.IsInitialized)
        {
            if (Input.GetKeyDown(KeyCode.C))
                LobbyManager.Instance.CreateLobby();


            if (Input.GetKeyDown(KeyCode.F))
                LobbyManager.Instance.RequestLobbyList();


            if (Input.GetKeyDown(KeyCode.O))
            {
                // Opens the main friends list overlay
                SteamFriends.ActivateGameOverlay("Friends");
                Debug.Log("Tried opening Steam overlay (Friends).");
            }

            if (Input.GetKeyDown(KeyCode.I) && LobbyManager.Instance != null)
            {
                // Opens invite dialog (only works if you're in a lobby)
                if (LobbyManager.Instance.currentLobby != CSteamID.Nil)
                    SteamFriends.ActivateGameOverlayInviteDialog(LobbyManager.Instance.currentLobby);
            }
        }
    }

}
