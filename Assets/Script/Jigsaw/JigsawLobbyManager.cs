using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class JigsawLobbyManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    [Header("🔥 สวิตช์ลับ Dev-Mode")]
    public bool devFastTrackMode = false;
    public bool devTestSolo = false;
    public string soloSceneName = "GameplaySolo";

    [Header("UI Panels (สำหรับสลับหน้าจอ)")]
    public GameObject mainMenuPanel;
    public GameObject createRoomPanel;
    public GameObject joinRoomPanel;

    [Header("UI Lobby Inputs")]
    public TMP_InputField playerNameInput;
    public TMP_InputField roomNameInput;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI roomCodeDisplay;
    public TextMeshProUGUI warningText;

    [Header("UI Buttons")]
    public Button createButton;
    public Button joinButton; // ปุ่มนี้อาจจะไม่ได้ใช้แล้วถ้าใช้ JoinGreen/Red เข้าห้องแทน
    public Button startGameButton;
    public Button singlePlayerButton;

    [Header("Avatar Selection")]
    public Image avatarDisplay;
    public Sprite[] avatarSprites;
    private int currentAvatarIndex = 0;

    [Header("🌟 Player Slots UI (T1 = ช่อง 0-2, T2 = ช่อง 3-5)")]
    public GameObject[] slotParents;
    public Image[] slotAvatars;
    public TextMeshProUGUI[] slotNames;
    public Button[] slotKickButtons;

    [Header("ชื่อซีน Gameplay ของแต่ละทีม")]
    public string team1SceneName = "GameplayTeam1";
    public string team2SceneName = "GameplayTeam2";
    public string hostMonitorSceneName = "HostMonitor";

    const string PROP_TEAM = "Team";
    const string PROP_ROLE = "Role";
    private const byte KICK_EVENT_CODE = 199;

    private Coroutine warningCoroutine;

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
        PhotonNetwork.AutomaticallySyncScene = false;
        ShowPanel(mainMenuPanel);

        if (createButton != null) createButton.onClick.AddListener(OnClickCreate);

        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnClickStart);
            startGameButton.gameObject.SetActive(false);
        }
        if (singlePlayerButton != null) singlePlayerButton.onClick.AddListener(OnClickSinglePlayer);

        if (warningText != null) warningText.gameObject.SetActive(false);

        UpdateAvatarDisplay();

        if (!PhotonNetwork.IsConnected)
        {
            SetStatus("กำลังเชื่อมต่อ Photon Server...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public void ShowPanel(GameObject activePanel)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (createRoomPanel != null) createRoomPanel.SetActive(false);
        if (joinRoomPanel != null) joinRoomPanel.SetActive(false);
        if (activePanel != null) activePanel.SetActive(true);
    }

    public void OnClickGoToJoinRoom() => ShowPanel(joinRoomPanel);

    public void OnClickBackToMain()
    {
        if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
        else ShowPanel(mainMenuPanel);
    }

    public override void OnLeftRoom()
    {
        ShowPanel(mainMenuPanel);
        SetStatus("เชื่อมต่อสำเร็จ ✓");
    }

    public void OnClickNextAvatar()
    {
        if (avatarSprites == null || avatarSprites.Length == 0) return;
        currentAvatarIndex = (currentAvatarIndex + 1) % avatarSprites.Length;
        UpdateAvatarDisplay();
    }

    public void OnClickPrevAvatar()
    {
        if (avatarSprites == null || avatarSprites.Length == 0) return;
        currentAvatarIndex--;
        if (currentAvatarIndex < 0) currentAvatarIndex = avatarSprites.Length - 1;
        UpdateAvatarDisplay();
    }

    void UpdateAvatarDisplay()
    {
        if (avatarDisplay != null && avatarSprites != null && avatarSprites.Length > 0)
        {
            avatarDisplay.sprite = avatarSprites[currentAvatarIndex];
        }
    }

    void SetupPlayerData(bool isHost = false)
    {
        if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text.Trim()))
            PhotonNetwork.NickName = playerNameInput.text.Trim();
        else if (string.IsNullOrEmpty(PhotonNetwork.NickName))
            PhotonNetwork.NickName = (isHost ? "Host_" : "Player_") + Random.Range(1000, 9999);

        Hashtable playerProps = new Hashtable();
        playerProps["AvatarID"] = currentAvatarIndex;
        playerProps[PROP_ROLE] = isHost ? "Spectator" : "Player";
        playerProps[PROP_TEAM] = isHost ? 0 : 1;

        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);
    }

    public override void OnConnectedToMaster()
    {
        if (devFastTrackMode)
        {
            SetStatus("[Dev Mode] กำลังแอบมุดเข้าห้องเทสต์...");
            PhotonNetwork.JoinOrCreateRoom("Dev_Bypass_Room", new RoomOptions { MaxPlayers = 7 }, null);
        }
        else
        {
            SetStatus("เชื่อมต่อสำเร็จ ✓");
            PhotonNetwork.JoinLobby();
        }
    }

    public void OnClickCreate()
    {
        if (playerNameInput == null || string.IsNullOrEmpty(playerNameInput.text.Trim())) { ShowWarning("กรุณาใส่ชื่อก่อนครับ!"); return; }
        SetupPlayerData(true);
        PhotonNetwork.CreateRoom(Random.Range(1000, 9999).ToString(), new RoomOptions { MaxPlayers = 7, IsVisible = true });
        SetStatus($"กำลังสร้างห้อง...");
    }


    public void OnClickJoinRoomWithTeam(string teamColor)
    {
        if (playerNameInput == null || string.IsNullOrEmpty(playerNameInput.text.Trim()))
        {
            ShowWarning("กรุณาใส่ชื่อก่อนครับ!");
            return;
        }


        int teamNumber = (teamColor == "Red") ? 2 : 1;

        if (PhotonNetwork.InRoom)
        {

            int teamCount = 0;
            foreach (var p in PhotonNetwork.PlayerList)
            {
                if (p.CustomProperties.ContainsKey(PROP_TEAM) && (int)p.CustomProperties[PROP_TEAM] == teamNumber)
                    teamCount++;
            }

            if (teamCount >= 3)
            {
                ShowWarning($"ทีม {teamNumber} เต็มแล้วครับ!");
                return;
            }

            Hashtable props = new Hashtable { { PROP_TEAM, teamNumber } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            return;
        }

        // 🌟 กรณีที่ 2: เพิ่งมาจากหน้าแรก กำลังจะมุดเข้าห้อง
        if (roomNameInput == null || string.IsNullOrEmpty(roomNameInput.text.Trim()))
        {
            ShowWarning("กรุณาใส่รหัสห้องด้วยครับ!");
            return;
        }

        PhotonNetwork.NickName = playerNameInput.text.Trim();

        // เซ็ตข้อมูลและแปะป้ายทีมให้ทันทีก่อนเข้าห้อง
        Hashtable playerProps = new Hashtable();
        playerProps["AvatarID"] = currentAvatarIndex;
        playerProps[PROP_ROLE] = "Player";
        playerProps[PROP_TEAM] = teamNumber;
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);

        PhotonNetwork.JoinRoom(roomNameInput.text.Trim());
        SetStatus($"กำลังเข้าห้อง...");
    }

    public void OnClickSinglePlayer()
    {
        if (playerNameInput == null || string.IsNullOrEmpty(playerNameInput.text.Trim())) { ShowWarning("กรุณาใส่ชื่อก่อนครับ!"); return; }
        PlayerPrefs.SetString("OfflineName", playerNameInput.text.Trim());
        PlayerPrefs.SetInt("OfflineAvatar", currentAvatarIndex);
        PlayerPrefs.Save();
        SceneManager.LoadScene(soloSceneName);
    }

    public override void OnJoinedRoom()
    {
        SetStatus($"เข้าห้อง {PhotonNetwork.CurrentRoom.Name} แล้ว");
        if (devFastTrackMode && devTestSolo) { PhotonNetwork.LoadLevel(soloSceneName); return; }

        bool isHost = (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(PROP_ROLE) && (string)PhotonNetwork.LocalPlayer.CustomProperties[PROP_ROLE] == "Spectator");

        if (isHost)
        {
            ShowPanel(createRoomPanel);
            if (startGameButton != null) startGameButton.gameObject.SetActive(true);
        }
        else
        {
            ShowPanel(joinRoomPanel);
            if (startGameButton != null) startGameButton.gameObject.SetActive(false);
            SetStatus($"รอโฮสต์กดเริ่มเกม... (ห้อง {PhotonNetwork.CurrentRoom.Name})");
        }
        UpdatePlayerList();
    }

    public override void OnJoinRoomFailed(short returnCode, string message) => ShowWarning("ไม่พบห้องนี้ หรือห้องเต็มแล้วครับ!");
    public override void OnCreateRoomFailed(short returnCode, string message) => ShowWarning("สร้างห้องไม่สำเร็จ ลองใหม่อีกครั้งครับ!");

    void OnClickStart()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int team1Count = 0;
        int team2Count = 0;

        foreach (var p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.ContainsKey(PROP_ROLE) && (string)p.CustomProperties[PROP_ROLE] == "Spectator")
                continue;

            int pTeam = p.CustomProperties.ContainsKey(PROP_TEAM) ? (int)p.CustomProperties[PROP_TEAM] : 1;
            if (pTeam == 1) team1Count++;
            else if (pTeam == 2) team2Count++;
        }

        // 🌟 แก้ไขให้เทสต์ 1v1 ได้ พร้อมใส่ข้อความแจ้งเตือน
        if (team1Count < 1 || team2Count < 1)
        {
            ShowWarning("ต้องมีผู้เล่นอย่างน้อยฝั่งละ 1 คนครับ!");
            return;
        }

        AssignTeamsAndStart();
    }

    void AssignTeamsAndStart()
    {
        var players = PhotonNetwork.PlayerList;
        foreach (var p in players)
        {
            bool isSpectator = (p.CustomProperties.ContainsKey(PROP_ROLE) && (string)p.CustomProperties[PROP_ROLE] == "Spectator");
            if (isSpectator)
            {
                photonView.RPC("RPC_AssignTeam", p, 0);
            }
            else
            {
                int teamNumber = p.CustomProperties.ContainsKey(PROP_TEAM) ? (int)p.CustomProperties[PROP_TEAM] : 1;
                photonView.RPC("RPC_AssignTeam", p, teamNumber);
            }
        }
    }

    [PunRPC]
    void RPC_AssignTeam(int teamNumber)
    {
        string scene = teamNumber == 0 ? hostMonitorSceneName : (teamNumber == 1 ? team1SceneName : team2SceneName);
        SetStatus($"ไปที่ซีน {scene}...");
        PhotonNetwork.LoadLevel(scene);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) { UpdatePlayerList(); }
    public override void OnPlayerLeftRoom(Player otherPlayer) { UpdatePlayerList(); }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey(PROP_TEAM) || changedProps.ContainsKey("AvatarID"))
        {
            UpdatePlayerList();
        }
    }

    void UpdatePlayerList()
    {
        if (!PhotonNetwork.InRoom) return;

        bool isHost = (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(PROP_ROLE) && (string)PhotonNetwork.LocalPlayer.CustomProperties[PROP_ROLE] == "Spectator");

        if (roomCodeDisplay != null)
            roomCodeDisplay.text = isHost ? $"Room Code: {PhotonNetwork.CurrentRoom.Name}" : "";

        for (int i = 0; i < slotParents.Length; i++)
            if (slotParents[i] != null) slotParents[i].SetActive(false);

        int t1Index = 0;
        int t2Index = 3;

        foreach (var p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.ContainsKey(PROP_ROLE) && (string)p.CustomProperties[PROP_ROLE] == "Spectator") continue;

            int pTeam = p.CustomProperties.ContainsKey(PROP_TEAM) ? (int)p.CustomProperties[PROP_TEAM] : 1;
            int targetSlot = -1;

            if (pTeam == 1 && t1Index <= 2) { targetSlot = t1Index; t1Index++; }
            else if (pTeam == 2 && t2Index <= 5) { targetSlot = t2Index; t2Index++; }

            if (targetSlot != -1 && slotParents.Length > targetSlot && slotParents[targetSlot] != null)
            {
                slotParents[targetSlot].SetActive(true);

                if (slotNames.Length > targetSlot && slotNames[targetSlot] != null)
                    slotNames[targetSlot].text = p.NickName;

                if (slotAvatars.Length > targetSlot && slotAvatars[targetSlot] != null)
                {
                    int avatarId = p.CustomProperties.ContainsKey("AvatarID") ? (int)p.CustomProperties["AvatarID"] : 0;
                    if (avatarSprites != null && avatarId >= 0 && avatarId < avatarSprites.Length)
                        slotAvatars[targetSlot].sprite = avatarSprites[avatarId];
                }

                if (slotKickButtons.Length > targetSlot && slotKickButtons[targetSlot] != null)
                {
                    slotKickButtons[targetSlot].gameObject.SetActive(isHost);
                    slotKickButtons[targetSlot].onClick.RemoveAllListeners();
                    int actorToKick = p.ActorNumber;
                    slotKickButtons[targetSlot].onClick.AddListener(() => KickPlayerViaEvent(actorToKick));
                }
            }
        }

        if (isHost && startGameButton != null)
        {
            int finalTeam1Count = t1Index;
            int finalTeam2Count = t2Index - 3;

            startGameButton.interactable = (finalTeam1Count >= 1 && finalTeam2Count >= 1);
        }
    }

    void KickPlayerViaEvent(int actorNr)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        SendOptions sendOptions = new SendOptions { Reliability = true };
        PhotonNetwork.RaiseEvent(KICK_EVENT_CODE, actorNr, raiseEventOptions, sendOptions);
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == KICK_EVENT_CODE)
        {
            int kickedActorNr = (int)photonEvent.CustomData;
            if (PhotonNetwork.LocalPlayer.ActorNumber == kickedActorNr) PhotonNetwork.LeaveRoom();
        }
    }

    void SetStatus(string msg) { if (statusText != null) statusText.text = msg; }

    public void ShowWarning(string message, float duration = 2f)
    {
        if (warningText == null) return;
        if (warningCoroutine != null) StopCoroutine(warningCoroutine);
        warningCoroutine = StartCoroutine(WarningRoutine(message, duration));
    }

    IEnumerator WarningRoutine(string message, float duration)
    {
        warningText.text = message;
        warningText.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        warningText.gameObject.SetActive(false);
        warningCoroutine = null;
    }
}