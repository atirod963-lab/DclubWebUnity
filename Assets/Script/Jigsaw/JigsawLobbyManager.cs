using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class JigsawLobbyManager : MonoBehaviourPunCallbacks
{
    // =======================================================
    [Header("🔥 สวิตช์ลับ Dev-Mode (ไม่ต้องง้อ UI เพื่อน)")]
    public bool devFastTrackMode = true;
    [Tooltip("ติ๊กถูก = เทสต์ Solo (คนเดียว) / เอาออก = เทสต์ Multi 2 คน (ต้องบิลด์ .exe เปิด 2 จอ)")]
    public bool devTestSolo = true;
    public string soloSceneName = "GameplaySolo"; // ใส่ชื่อซีน Solo ของคุณให้ตรงเป๊ะ
    // =======================================================

    [Header("UI (ปล่อย None ไว้งั้นแหละ)")]
    public TMP_InputField roomNameInput;
    public Button createButton;
    public Button joinButton;
    public Button startButton;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI playersText;

    [Header("ชื่อซีน Gameplay ของแต่ละทีม")]
    public string team1SceneName = "GameplayTeam1";
    public string team2SceneName = "GameplayTeam2";

    const string PROP_TEAM = "Team";

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = false;

        // ครอบเช็ค null ไว้หมดแล้ว ต่อให้ช่อง UI ข้างบนเป็น None โค้ดก็จะไม่พัง!
        if (createButton != null) createButton.onClick.AddListener(OnClickCreate);
        if (joinButton != null) joinButton.onClick.AddListener(OnClickJoin);
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnClickStart);
            startButton.gameObject.SetActive(false);
        }

        if (!PhotonNetwork.IsConnected)
        {
            SetStatus("กำลังเชื่อมต่อ Photon Server...");
            PhotonNetwork.ConnectUsingSettings();
        }
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
            SetStatus("เชื่อมต่อสำเร็จ ✓ พร้อมสร้าง/เข้าห้องจิ๊กซอว์");
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnJoinedRoom()
    {
        SetStatus($"เข้าห้อง {PhotonNetwork.CurrentRoom.Name} แล้ว");

        if (devFastTrackMode)
        {
            if (devTestSolo)
            {
                Debug.Log("🚀 [Dev Mode] ยิงตรงเข้าสู่โหมด Solo!");
                PhotonNetwork.LoadLevel(soloSceneName);
            }
            else
            {
                Debug.Log("⏳ [Dev Mode] รอผู้เล่นคนที่ 2 มาร่วมวง...");
                if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && PhotonNetwork.IsMasterClient)
                {
                    Debug.Log("คนครบ 2 คนแล้ว! สั่งรันระบบ Assign ทีม!");
                    AssignTeamsAndStart();
                }
            }
            return;
        }

        UpdatePlayerList();
        if (startButton != null) startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
    }

    // -------------------------------------------------------
    //  ฟังก์ชันดั้งเดิมของเพื่อนคุณ (เก็บไว้ให้ครบห้ามหายแม้แต่ตัวเดียว)
    // -------------------------------------------------------
    void OnClickCreate()
    {
        string roomName = (roomNameInput != null && !string.IsNullOrEmpty(roomNameInput.text)) ? roomNameInput.text.Trim() : "Jigsaw_" + Random.Range(1000, 9999);
        PhotonNetwork.CreateRoom(roomName, new RoomOptions { MaxPlayers = 4, IsVisible = true });
        SetStatus($"กำลังสร้างห้อง: {roomName}");
    }

    void OnClickJoin()
    {
        if (roomNameInput == null || string.IsNullOrEmpty(roomNameInput.text.Trim())) return;
        PhotonNetwork.JoinRoom(roomNameInput.text.Trim());
        SetStatus($"กำลังเข้าห้อง: {roomNameInput.text.Trim()}");
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
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { PROP_TEAM, teamNumber } });
        string scene = (teamNumber == 1) ? team1SceneName : team2SceneName;
        SetStatus($"ทีม {teamNumber} — โหลดซีน {scene}...");
        PhotonNetwork.LoadLevel(scene);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (devFastTrackMode && !devTestSolo && PhotonNetwork.CurrentRoom.PlayerCount == 2 && PhotonNetwork.IsMasterClient)
        {
            AssignTeamsAndStart();
        }
        UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer) => UpdatePlayerList();
    public override void OnCreateRoomFailed(short returnCode, string message) => SetStatus($"สร้างห้องพลาด: {message}");
    public override void OnJoinRoomFailed(short returnCode, string message) => SetStatus($"เข้าห้องพลาด: {message}");

    void UpdatePlayerList()
    {
        if (playersText == null) return;
        string list = "ผู้เล่นในห้อง:\n";
        foreach (var p in PhotonNetwork.PlayerList) list += $" • {p.NickName ?? "Player_" + p.ActorNumber}\n";
        playersText.text = list;
    }

    void SetStatus(string msg) { if (statusText != null) statusText.text = msg; }
}