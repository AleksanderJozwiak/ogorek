using Steamworks;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyTeamManager : MonoBehaviour
{
    [Header("Team Hierarchy")]
    public Transform playerTeamsRoot;
    public Button readyButton;
    public Button startButton;

    public Sprite readySprite;
    public Sprite notReadySprite;

    private int currentTeam = -1;
    private int currentSlot = -1;
    private bool isReady = false;

    private void Start()
    {
        SetupTeamButtons();
        RefreshTeamUI();

        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(OnHostStartGame);
    }

    #region Setup Buttons
    private void SetupTeamButtons()
    {
        for (int teamIndex = 0; teamIndex < playerTeamsRoot.childCount; teamIndex++)
        {
            Transform team = playerTeamsRoot.GetChild(teamIndex);
            int teamNum = teamIndex + 1;

            for (int slotIndex = 0; slotIndex < team.childCount; slotIndex++)
            {
                Transform slot = team.GetChild(slotIndex);
                int slotNum = slotIndex + 1;

                Button btn = slot.GetComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => AssignPlayerToSlot(teamNum, slotNum));
            }
        }
    }
    #endregion

    #region Slot Assignment
    public void AssignPlayerToSlot(int teamNum, int slotNum)
    {
        if (LobbyManager.Instance.currentLobby == CSteamID.Nil) return;

        currentTeam = teamNum;
        currentSlot = slotNum;

        // Update metadata for local player
        string slotMeta = $"{teamNum}_{slotNum}";
        SteamMatchmaking.SetLobbyMemberData(LobbyManager.Instance.currentLobby, "slot", slotMeta);

        // Reset ready status when changing slot
        isReady = false;
        SteamMatchmaking.SetLobbyMemberData(LobbyManager.Instance.currentLobby, "ready", "0");

        RefreshTeamUI();
    }
    public void AssignEmptySlotForLocalPlayer()
    {
        if (LobbyManager.Instance.currentLobby == CSteamID.Nil) return;

        int memberCount = SteamMatchmaking.GetNumLobbyMembers(LobbyManager.Instance.currentLobby);

        for (int teamIndex = 0; teamIndex < playerTeamsRoot.childCount; teamIndex++)
        {
            Transform team = playerTeamsRoot.GetChild(teamIndex);
            for (int slotIndex = 0; slotIndex < team.childCount; slotIndex++)
            {
                // Check if slot is taken
                bool slotTaken = false;
                for (int i = 0; i < memberCount; i++)
                {
                    CSteamID memberId = SteamMatchmaking.GetLobbyMemberByIndex(LobbyManager.Instance.currentLobby, i);
                    string memberSlot = SteamMatchmaking.GetLobbyMemberData(LobbyManager.Instance.currentLobby, memberId, "slot");

                    if (!string.IsNullOrEmpty(memberSlot))
                    {
                        string[] split = memberSlot.Split('_');
                        int t = int.Parse(split[0]);
                        int s = int.Parse(split[1]);
                        if (t - 1 == teamIndex && s - 1 == slotIndex)
                        {
                            slotTaken = true;
                            break;
                        }
                    }
                }

                if (!slotTaken)
                {
                    // Assign this slot to local player
                    currentTeam = teamIndex + 1;
                    currentSlot = slotIndex + 1;
                    SteamMatchmaking.SetLobbyMemberData(
                        LobbyManager.Instance.currentLobby,
                        "slot",
                        $"{currentTeam}_{currentSlot}"
                    );

                    // Reset ready
                    isReady = false;
                    SteamMatchmaking.SetLobbyMemberData(
                        LobbyManager.Instance.currentLobby,
                        "ready",
                        "0"
                    );

                    RefreshTeamUI();
                    return;
                }
            }
        }
    }

    #endregion

    #region UI Refresh
    public void RefreshTeamUI()
    {
        if (LobbyManager.Instance.currentLobby == CSteamID.Nil) return;

        // Reset UI
        for (int teamIndex = 0; teamIndex < playerTeamsRoot.childCount; teamIndex++)
        {
            Transform team = playerTeamsRoot.GetChild(teamIndex);
            for (int slotIndex = 0; slotIndex < team.childCount; slotIndex++)
            {
                Transform slot = team.GetChild(slotIndex);
                TMP_Text text = slot.GetComponentInChildren<TMP_Text>();
                Image readyImg = slot.GetComponentsInChildren<Image>().FirstOrDefault(x => x.name == "ReadyMarker");
                Button btn = slot.GetComponent<Button>();

                text.text = "Empty";
                readyImg.sprite = notReadySprite;
                btn.interactable = true;
            }
        }

        // Track ready states
        int memberCount = SteamMatchmaking.GetNumLobbyMembers(LobbyManager.Instance.currentLobby);
        int readyCount = 0;

        for (int i = 0; i < memberCount; i++)
        {
            CSteamID memberId = SteamMatchmaking.GetLobbyMemberByIndex(LobbyManager.Instance.currentLobby, i);
            string slotMeta = SteamMatchmaking.GetLobbyMemberData(LobbyManager.Instance.currentLobby, memberId, "slot");
            string readyStr = SteamMatchmaking.GetLobbyMemberData(LobbyManager.Instance.currentLobby, memberId, "ready");

            if (string.IsNullOrEmpty(slotMeta)) continue;

            string[] split = slotMeta.Split('_');
            int teamNum = int.Parse(split[0]);
            int slotNum = int.Parse(split[1]);

            Transform slotTransform = playerTeamsRoot.GetChild(teamNum - 1).GetChild(slotNum - 1);
            TMP_Text text = slotTransform.GetComponentInChildren<TMP_Text>();
            Image readyImg = slotTransform.GetComponentsInChildren<Image>().FirstOrDefault(x => x.name == "ReadyMarker");
            Button btn = slotTransform.GetComponent<Button>();

            text.text = SteamFriends.GetFriendPersonaName(memberId);
            readyImg.sprite = readyStr == "1" ? readySprite : notReadySprite;

            btn.interactable = false;

            if (readyStr == "1")
                readyCount++;
        }

        // === Host start button logic ===
        bool allReady = readyCount == memberCount && memberCount > 0;

        // Check if I'm host
        string hostName = SteamMatchmaking.GetLobbyData(LobbyManager.Instance.currentLobby, "host_name");
        bool isHost = LobbyManager.Instance.IsHost;

        if (isHost)
        {
            if (allReady)
            {
                readyButton.gameObject.SetActive(false);
                startButton.gameObject.SetActive(true);
            }
            else
            {
                readyButton.gameObject.SetActive(true);
                startButton.gameObject.SetActive(false);
            }
        }

        // Non-hosts should only see ready button
        if (!isHost)
        {
            readyButton.gameObject.SetActive(true);
            startButton.gameObject.SetActive(false);
        }

        // Update local ready button text
        UpdateReadyButtonText();
    }

    #endregion

    #region Ready
    public void ToggleReady()
    {
        if (currentTeam == -1 || currentSlot == -1)
        {
            Debug.LogWarning("Local player not in a slot.");
            return;
        }

        isReady = !isReady;
        string readyVal = isReady ? "1" : "0";
        SteamMatchmaking.SetLobbyMemberData(LobbyManager.Instance.currentLobby, "ready", readyVal);

        RefreshTeamUI();
    }

    private void UpdateReadyButtonText()
    {
        TMP_Text btnText = readyButton.GetComponentInChildren<TMP_Text>();
        btnText.text = isReady ? "Unready" : "Ready";
    }
    #endregion

    private void OnHostStartGame()
    {
        if (LobbyManager.Instance.currentLobby == CSteamID.Nil) return;

        Debug.Log("Host starting game...");

        // Set lobby data so all players know game is starting
        SteamMatchmaking.SetLobbyData(LobbyManager.Instance.currentLobby, "game_start", "1");

        // You could also load your scene directly here for host
        LobbyManager.Instance.LoadScene("GameScene");
    }

}
