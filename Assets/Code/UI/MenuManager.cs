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
    public CanvasGroup PauseMenuPanel;

    public Toggle friendsOnly;

    private Coroutine refreshRoutine;

    private void Start()
    {
        if (LobbyBrowserPanel == null || LobbyPanel == null) return;
        StartCoroutine(WaitForLobbyManager());
        if (LobbyManager.Instance != null)
            refreshRoutine = StartCoroutine(AutoRefreshLobbyList());
    }

    private IEnumerator WaitForLobbyManager()
    {
        while (LobbyManager.Instance == null)
            yield return null;

        LobbyManager.Instance.OnLobbyListUpdated += RefreshLobbyList;
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
            yield return new WaitForSeconds(2.5f);
        }
    }

    public void TogglePauseMenu()
    {
        ToggleCanvasGroup(PauseMenuPanel, out bool isVisible);
        if (isVisible)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDisable()
    {
        if (refreshRoutine != null)
        {
            StopCoroutine(refreshRoutine);
            refreshRoutine = null;
        }
        if (LobbyManager.Instance != null)
            LobbyManager.Instance.OnLobbyListUpdated -= RefreshLobbyList;
    }

    public void ToggleCanvasGroup(CanvasGroup canvasGroup) => ToggleCanvasGroup(canvasGroup, out _);

    public void ToggleCanvasGroup(CanvasGroup canvasGroup, out bool isVisible)
    {
        if (canvasGroup.alpha == 0)
            canvasGroup.alpha = 1;
        else
            canvasGroup.alpha = 0;

        canvasGroup.interactable = canvasGroup.alpha == 1;
        canvasGroup.blocksRaycasts = canvasGroup.alpha == 1;

        isVisible = canvasGroup.alpha == 1;
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


    public void HidePanel(CanvasGroup canvasGroup)
    {
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
    private void HideAllPanels()
    {
        HidePanel(MainMenuPanel);
        HidePanel(LobbyPanel);
        HidePanel(LobbyBrowserPanel);
    }

    public void ShowLobbyBrowserAfterHostLeft()
    {
        HideAllPanels();
        ToggleCanvasGroup(LobbyBrowserPanel);
        LobbyManager.Instance.RequestLobbyList();
        var list = LobbyManager.Instance.GetFoundLobbies();
        RefreshLobbyList(list);
    }

    public void OpenLobbyPanel()
    {
        HideAllPanels();
        ToggleCanvasGroup(LobbyPanel);
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
            int players = LobbyManager.Instance.GetLobbyPlayerCount(lobbyId);

            var lobbName = texts.FirstOrDefault(x => x.name == "HostName");
            var playerCount = texts.FirstOrDefault(x => x.name == "PlayerCount");
            lobbName.text = $"{name}";
            playerCount.text = $"{players}/18";

            btn.onClick.AddListener(() =>
            {
                LobbyManager.Instance.JoinLobby(lobbyId);
                HideAllPanels();
                ToggleCanvasGroup(LobbyPanel);
            });
        }
    }

    public void OpenMainMenu()
    {
        LobbyManager.Instance.LeaveLobby();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
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
