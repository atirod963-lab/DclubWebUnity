using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class JigsawLobbyManager : MonoBehaviourPunCallbacks
{
    [Header("🔥 สวิตช์ลับ Dev-Mode")]
    public bool devFastTrackMode = true;
    public bool devTestSolo = true;
    public string soloSceneName = "GameplaySolo";

    [Header("UI Lobby")]
    public TMP_InputField playerNameInput;
    public TMP_InputField roomNameInput;
    public Button createButton;
    public Button joinButton;
    public Button startButton;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI playersText;

    [Header("Avatar Selection (เลือกระบุรูปโปรไฟล์)")]
    public Image avatarDisplay; // ช่องโชว์รูปที่เลือกอยู่
    public Sprite[] avatarSprites; // ลากรูปทั้งหมดมาใส่ตรงนี้
    private int currentAvatarIndex = 0;

    [Header("ชื่อซีน Gameplay ของแต่ละทีม")]
    public string team1SceneName = "GameplayTeam1";
    public string team2SceneName = "GameplayTeam2";

    const string PROP_TEAM = "Team";

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = false;

        if (createButton != null) createButton.onClick.AddListener(OnClickCreate);
        if (joinButton != null) joinButton.onClick.AddListener(OnClickJoin);
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnClickStart);
            startButton.gameObject.SetActive(false);
        }

        // โชว์รูปอวตารเริ่มต้น
        UpdateAvatarDisplay();

        if (!PhotonNetwork.IsConnected)
        {
            SetStatus("กำลังเชื่อมต่อ Photon Server...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    // --- ฟังก์ชันเลื่อนรูป (เอาไปผูกกับปุ่มลูกศร ซ้าย-ขวา) ---
    public void OnClickNextAvatar()
    {
        currentAvatarIndex = (currentAvatarIndex + 1) % avatarSprites.Length;
        UpdateAvatarDisplay();
    }

    public void OnClickPrevAvatar()
    {
        currentAvatarIndex--;
        if (currentAvatarIndex < 0) currentAvatarIndex = avatarSprites.Length - 1;
        UpdateAvatarDisplay();
    }

    void UpdateAvatarDisplay()
    {
        if (avatarDisplay != null && avatarSprites.Length > 0)
        {
            avatarDisplay.sprite = avatarSprites[currentAvatarIndex];
        }
    }
    // ----------------------------------------------------

    void SetupPlayerData()
    {
        // 1. เซฟชื่อ
        if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text.Trim()))
            PhotonNetwork.NickName = playerNameInput.text.Trim();
        else if (string.IsNullOrEmpty(PhotonNetwork.NickName))
            PhotonNetwork.NickName = "Player_" + Random.Range(1000, 9999);

        // 2. เซฟ ID รูปอวตาร ส่งขึ้นเซิร์ฟเวอร์
        Hashtable playerProps = new Hashtable();
        playerProps["AvatarID"] = currentAvatarIndex;
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);
    }

    public override void OnConnectedToMaster()
    {
        if (devFastTrackMode)
        {
            SetStatus("[Dev Mode] ต่อเน็ตติดแล้ว! กำลังแอบมุดเข้าห้องเทสต์...");
            PhotonNetwork.JoinOrCreateRoom("Dev_Bypass_Room", new RoomOptions { MaxPlayers = 4 }, null);
        }
        else
        {
            SetStatus("เชื่อมต่อสำเร็จ ✓");
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnJoinedRoom()
    {
        SetStatus($"เข้าห้อง {PhotonNetwork.CurrentRoom.Name} แล้ว");
        if (devFastTrackMode && devTestSolo)
        {
            PhotonNetwork.LoadLevel(soloSceneName);
            return;
        }
        UpdatePlayerList();
        if (startButton != null) startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
    }

    void OnClickCreate()
    {
        SetupPlayerData(); // บันทึกชื่อและรูปก่อนสร้างห้อง
        string roomName = (roomNameInput != null && !string.IsNullOrEmpty(roomNameInput.text)) ? roomNameInput.text.Trim() : "Jigsaw_" + Random.Range(1000, 9999);
        PhotonNetwork.CreateRoom(roomName, new RoomOptions { MaxPlayers = 4, IsVisible = true });
        SetStatus($"กำลังสร้างห้อง...");
    }

    void OnClickJoin()
    {
        if (roomNameInput == null || string.IsNullOrEmpty(roomNameInput.text.Trim())) return;
        SetupPlayerData(); // บันทึกชื่อและรูปก่อนจอยห้อง
        PhotonNetwork.JoinRoom(roomNameInput.text.Trim());
        SetStatus($"กำลังเข้าห้อง...");
    }

    void OnClickStart()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom.PlayerCount < 2) return;
        AssignTeamsAndStart();
    }

    void AssignTeamsAndStart()
    {
        var players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            int teamNumber = (i < players.Length / 2) ? 1 : 2;
            photonView.RPC("RPC_AssignTeam", players[i], teamNumber);
        }
    }

    [PunRPC]
    void RPC_AssignTeam(int teamNumber)
    {
        Hashtable props = new Hashtable { { PROP_TEAM, teamNumber } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        string scene = (teamNumber == 1) ? team1SceneName : team2SceneName;
        SetStatus($"ทีม {teamNumber} — โหลดซีน {scene}...");
        PhotonNetwork.LoadLevel(scene);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) { UpdatePlayerList(); }
    public override void OnPlayerLeftRoom(Player otherPlayer) { UpdatePlayerList(); }

    void UpdatePlayerList()
    {
        if (playersText == null) return;
        string list = "ผู้เล่นในห้อง:\n";
        foreach (var p in PhotonNetwork.PlayerList) list += $" • {p.NickName}\n";
        playersText.text = list;
    }
    void SetStatus(string msg) { if (statusText != null) statusText.text = msg; }
}