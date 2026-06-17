using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;


public class JigsawLobbyManager : MonoBehaviourPunCallbacks 
{
    [Header("UI")]
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

    // -------------------------------------------------------
    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = false;
        createButton.onClick.AddListener(OnClickCreate);
        joinButton.onClick.AddListener(OnClickJoin);
        startButton.onClick.AddListener(OnClickStart);
        startButton.gameObject.SetActive(false);

        if (!PhotonNetwork.IsConnected)
        {
            SetStatus("กำลังเชื่อมต่อ...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    // -------------------------------------------------------
    //  BUTTON HANDLERS
    // -------------------------------------------------------
    void OnClickCreate()
    {
        string roomName = roomNameInput.text.Trim();
        if (string.IsNullOrEmpty(roomName)) roomName = "Jigsaw_" + Random.Range(1000, 9999);

        var options = new RoomOptions { MaxPlayers = 4, IsVisible = true };
        PhotonNetwork.CreateRoom(roomName, options);
        SetStatus($"กำลังสร้างห้อง: {roomName}");
    }

    void OnClickJoin()
    {
        string roomName = roomNameInput.text.Trim();
        if (string.IsNullOrEmpty(roomName)) return;
        PhotonNetwork.JoinRoom(roomName);
        SetStatus($"กำลังเข้าห้อง: {roomName}");
    }

    void OnClickStart()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            SetStatus("ต้องมีผู้เล่นอย่างน้อย 2 คน!");
            return;
        }

        AssignTeamsAndStart();
    }

    // -------------------------------------------------------
    //  TEAM ASSIGNMENT
    // -------------------------------------------------------
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
        var props = new Hashtable { { PROP_TEAM, teamNumber } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        string scene = (teamNumber == 1) ? team1SceneName : team2SceneName;
        SetStatus($"ทีม {teamNumber} — โหลดซีน {scene}...");
        PhotonNetwork.LoadLevel(scene);
    }

    // -------------------------------------------------------
    //  PHOTON CALLBACKS
    // -------------------------------------------------------
    public override void OnConnectedToMaster()
    {
        SetStatus("เชื่อมต่อสำเร็จ ✓  พร้อมสร้าง/เข้าห้องจิ๊กซอว์");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedRoom()
    {
        SetStatus($"เข้าห้อง {PhotonNetwork.CurrentRoom.Name} แล้ว");
        UpdatePlayerList();
        bool isMaster = PhotonNetwork.IsMasterClient;
        startButton.gameObject.SetActive(isMaster);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerList();
        SetStatus($"{newPlayer.NickName} เข้าร่วมแล้ว");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
        => SetStatus($"สร้างห้องไม่สำเร็จ: {message}");

    public override void OnJoinRoomFailed(short returnCode, string message)
        => SetStatus($"เข้าห้องไม่สำเร็จ: {message}");

    // -------------------------------------------------------
    //  HELPERS
    // -------------------------------------------------------
    void UpdatePlayerList()
    {
        string list = "ผู้เล่นในห้อง:\n";
        foreach (var p in PhotonNetwork.PlayerList)
            list += $"  • {p.NickName ?? "Player_" + p.ActorNumber}\n";
        playersText.text = list;
    }

    void SetStatus(string msg) => statusText.text = msg;
}