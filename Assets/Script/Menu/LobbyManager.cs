using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using ExitGames.Client.Photon;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class LobbyManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    [Header("Room Settings")]
    [Tooltip("จำกัดจำนวนคนสูงสุดต่อ 1 ทีม (ค่าเริ่มต้นคือ 1 vs 1)")]
    public int maxPlayersPerTeam = 1;

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject hostDashboardPanel;
    public GameObject playerJoinPanel;

    [Header("Host UI")]
    public TextMeshProUGUI roomCodeText;
    public TextMeshProUGUI greenTeamListText;
    public TextMeshProUGUI redTeamListText;
    public Button startGameButton;

    [Header("Host Team Avatars (โชว์รูปคนในห้อง)")]
    public Image hostGreenAvatar;
    public Image hostRedAvatar;

    [Header("Avatar Selection UI")]
    public Image avatarDisplayImage;
    public Sprite[] avatarSprites;

    [Header("Player UI")]
    public TMP_InputField playerNameInput;
    public TMP_InputField roomCodeInput;
    public TextMeshProUGUI playerStatusText;
    public TextMeshProUGUI warningText;

    private int selectedAvatarIndex = 0;
    private bool isHost = false;

    private const byte KICK_EVENT_CODE = 199;

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();

        if (warningText != null) warningText.gameObject.SetActive(false);

        if (hostGreenAvatar != null) hostGreenAvatar.gameObject.SetActive(false);
        if (hostRedAvatar != null) hostRedAvatar.gameObject.SetActive(false);

        ShowPanel(mainMenuPanel);
        UpdateAvatarUI();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("เชื่อมต่อเซิร์ฟเวอร์ Photon สำเร็จ!");
        PhotonNetwork.JoinLobby();
    }

    IEnumerator ShowWarningRoutine(string msg)
    {
        warningText.text = msg;
        warningText.gameObject.SetActive(true);
        yield return new WaitForSeconds(2.5f);
        if (warningText != null) warningText.gameObject.SetActive(false);
    }

    public void OnClickNextAvatar()
    {
        if (avatarSprites == null || avatarSprites.Length == 0) return;
        selectedAvatarIndex++;
        if (selectedAvatarIndex >= avatarSprites.Length) selectedAvatarIndex = 0;
        UpdateAvatarUI();
    }

    public void OnClickPreviousAvatar()
    {
        if (avatarSprites == null || avatarSprites.Length == 0) return;
        selectedAvatarIndex--;
        if (selectedAvatarIndex < 0) selectedAvatarIndex = avatarSprites.Length - 1;
        UpdateAvatarUI();
    }

    void UpdateAvatarUI()
    {
        if (avatarDisplayImage != null && avatarSprites.Length > 0)
        {
            avatarDisplayImage.sprite = avatarSprites[selectedAvatarIndex];
        }
    }

    public void OnClickCreateHostRoom()
    {
        if (PhotonNetwork.NetworkClientState == ClientState.Joining) return;

        string pName = (playerNameInput != null && !string.IsNullOrWhiteSpace(playerNameInput.text)) ? playerNameInput.text.Trim() : "Host";
        PhotonNetwork.NickName = pName;

        isHost = true;
        string randomCode = Random.Range(1000, 10000).ToString();

        RoomOptions options = new RoomOptions { MaxPlayers = (byte)((maxPlayersPerTeam * 2) + 5) };
        PhotonNetwork.CreateRoom(randomCode, options);
    }

    public void OnClickGoToJoin()
    {
        isHost = false;
        ShowPanel(playerJoinPanel);
        if (playerStatusText != null) playerStatusText.text = "";
    }

    public void OnClickJoinRoomWithTeam(string teamName)
    {
        if (PhotonNetwork.NetworkClientState == ClientState.Joining || PhotonNetwork.NetworkClientState == ClientState.JoiningLobby) return;

        string code = roomCodeInput.text.Trim();

        if (string.IsNullOrEmpty(code))
        {
            if (playerStatusText != null) playerStatusText.text = "กรุณาใส่รหัสห้องก่อนกดเลือกทีม!";
            return;
        }

        string pName = (playerNameInput != null && !string.IsNullOrWhiteSpace(playerNameInput.text)) ? playerNameInput.text.Trim() : "Player_Temp";
        PhotonNetwork.NickName = pName;

        Hashtable props = new Hashtable {
            { "Team", teamName },
            { "Role", "Player" },
            { "Avatar", selectedAvatarIndex }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        if (playerStatusText != null) playerStatusText.text = "กำลังเข้าห้อง... (เช็คโควต้าทีม " + teamName + ")";
        PhotonNetwork.JoinRoom(code);
    }

    public void OnClickSinglePlayer()
    {
        string pName = (playerNameInput != null && !string.IsNullOrWhiteSpace(playerNameInput.text)) ? playerNameInput.text.Trim() : "Player 1";
        PlayerPrefs.SetString("OfflineName", pName);
        PlayerPrefs.SetInt("OfflineAvatar", selectedAvatarIndex);
        PlayerPrefs.Save();

        if (TouchManager2D.Instance != null) TouchManager2D.Instance.score = 0;
        SceneManager.LoadScene("MG1_1");
    }

    public override void OnLeftRoom()
    {
        ResetToMainMenu();
    }

    void ResetToMainMenu()
    {
        isHost = false;

        if (greenTeamListText != null) greenTeamListText.text = "Green Team:\n";
        if (redTeamListText != null) redTeamListText.text = "Red Team:\n";
        if (hostGreenAvatar != null) hostGreenAvatar.gameObject.SetActive(false);
        if (hostRedAvatar != null) hostRedAvatar.gameObject.SetActive(false);

        ShowPanel(mainMenuPanel);
    }

    public override void OnJoinedRoom()
    {
        if (!isHost && PhotonNetwork.NickName == "Player_Temp")
        {
            int playerIndex = 0;
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (p.CustomProperties.ContainsKey("Role") && (string)p.CustomProperties["Role"] == "Spectator") continue;
                if (p.ActorNumber <= PhotonNetwork.LocalPlayer.ActorNumber) playerIndex++;
            }
            PhotonNetwork.NickName = "Player " + playerIndex;

            Hashtable updateProp = new Hashtable { { "NameUpdated", true } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(updateProp);
        }

        Hashtable props = new Hashtable();

        if (isHost)
        {
            ShowPanel(hostDashboardPanel);
            if (roomCodeText != null) roomCodeText.text = PhotonNetwork.CurrentRoom.Name;

            props["Role"] = "Spectator";
            props["Team"] = "None";
            props["Avatar"] = 0;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
        else
        {
            string myTeam = (string)PhotonNetwork.LocalPlayer.CustomProperties["Team"];
            int currentTeamMembers = 0;

            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (p.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    if (p.CustomProperties.ContainsKey("Team") && (string)p.CustomProperties["Team"] == myTeam)
                    {
                        if (p.CustomProperties.ContainsKey("Role") && (string)p.CustomProperties["Role"] == "Player")
                        {
                            currentTeamMembers++;
                        }
                    }
                }
            }

            if (currentTeamMembers >= maxPlayersPerTeam)
            {
                StartCoroutine(ShowWarningRoutine($"ทีม {myTeam} เต็มแล้ว! กรุณาเลือกสีอื่นครับ"));
                PhotonNetwork.LeaveRoom();
                return;
            }

            if (playerStatusText != null)
            {
                playerStatusText.text = "เข้าห้องสำเร็จ! รอโฮสต์กดเริ่มเกม... (ทีม " + myTeam + ")";
            }
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        if (playerStatusText != null) playerStatusText.text = "ไม่พบห้องนี้! ตรวจสอบรหัสอีกครั้ง";
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) { UpdateHostUI(); }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateHostUI();

        if (otherPlayer.CustomProperties.ContainsKey("Role") && (string)otherPlayer.CustomProperties["Role"] == "Spectator")
        {
            if (!isHost)
            {
                if (playerStatusText != null) playerStatusText.text = "โฮสต์ปิดห้องไปแล้ว! กรุณาเข้าห้องใหม่";
                PhotonNetwork.LeaveRoom();
            }
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        UpdateHostUI();
    }

    public void OnClickKickGreenPlayer()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.ContainsKey("Team") && (string)p.CustomProperties["Team"] == "Green")
            {
                KickPlayerViaEvent(p.ActorNumber);
                break;
            }
        }
    }

    public void OnClickKickRedPlayer()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.ContainsKey("Team") && (string)p.CustomProperties["Team"] == "Red")
            {
                KickPlayerViaEvent(p.ActorNumber);
                break;
            }
        }
    }

    void KickPlayerViaEvent(int targetActorNumber)
    {
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        SendOptions sendOptions = new SendOptions { Reliability = true };
        PhotonNetwork.RaiseEvent(KICK_EVENT_CODE, targetActorNumber, raiseEventOptions, sendOptions);
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == KICK_EVENT_CODE)
        {
            int kickedActorNr = (int)photonEvent.CustomData;

            if (PhotonNetwork.LocalPlayer.ActorNumber == kickedActorNr)
            {
                if (playerStatusText != null) playerStatusText.text = "คุณถูกเตะออกจากห้องโดยโฮสต์!";
                PhotonNetwork.LeaveRoom();
            }
        }
    }

    void UpdateHostUI()
    {
        if (!isHost) return;

        string greenList = "Green Team:\n";
        string redList = "Red Team:\n";

        if (hostGreenAvatar != null) hostGreenAvatar.gameObject.SetActive(false);
        if (hostRedAvatar != null) hostRedAvatar.gameObject.SetActive(false);

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.ContainsKey("Role") && (string)p.CustomProperties["Role"] == "Player")
            {
                string team = p.CustomProperties.ContainsKey("Team") ? (string)p.CustomProperties["Team"] : "None";
                int avatarId = p.CustomProperties.ContainsKey("Avatar") ? (int)p.CustomProperties["Avatar"] : 0;

                if (team == "Green")
                {
                    greenList += p.NickName + "\n";
                    if (hostGreenAvatar != null && avatarId >= 0 && avatarId < avatarSprites.Length)
                    {
                        hostGreenAvatar.sprite = avatarSprites[avatarId];
                        hostGreenAvatar.gameObject.SetActive(true);
                    }
                }
                else if (team == "Red")
                {
                    redList += p.NickName + "\n";
                    if (hostRedAvatar != null && avatarId >= 0 && avatarId < avatarSprites.Length)
                    {
                        hostRedAvatar.sprite = avatarSprites[avatarId];
                        hostRedAvatar.gameObject.SetActive(true);
                    }
                }
            }
        }

        if (greenTeamListText != null) greenTeamListText.text = greenList;
        if (redTeamListText != null) redTeamListText.text = redList;

        if (startGameButton != null) startGameButton.interactable = (PhotonNetwork.PlayerList.Length > 1);
    }

    public void OnClickStartGame()
    {
        if (PhotonNetwork.IsMasterClient) PhotonNetwork.LoadLevel("MG1_1");
    }

    void ShowPanel(GameObject activePanel)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (hostDashboardPanel != null) hostDashboardPanel.SetActive(false);
        if (playerJoinPanel != null) playerJoinPanel.SetActive(false);
        if (activePanel != null) activePanel.SetActive(true);
    }

    public void OnClickBackToMain()
    {
        if (PhotonNetwork.InRoom)
        {
            if (playerStatusText != null) playerStatusText.text = "กำลังออกจากห้อง...";
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            ResetToMainMenu();
        }
    }
}