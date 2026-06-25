using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using ExitGames.Client.Photon; // จำเป็นสำหรับการใช้ Hashtable

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public TMP_InputField nameInputField;
    public TMP_InputField roomCodeInput;
    public TextMeshProUGUI showCodeText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI playerBarText;

    [Header("Panels")]
    public GameObject createRoomPanel;
    public GameObject joinRoomPanel;

    [Header("Buttons")]
    public Button createRoomButton;
    public Button joinRoomButton;
    public Button startGameButton;

    [Header("Avatar Selection (เพิ่มใหม่)")]
    public Image avatarDisplay; // ช่องโชว์รูปที่เลือกอยู่
    public Sprite[] avatarSprites; // ลากรูปทั้งหมดมาใส่ตรงนี้
    private int currentAvatarIndex = 0;

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        SetStatus("Connecting...");
        SetButtonsInteractable(false);
        if (startGameButton != null) startGameButton.gameObject.SetActive(false);

        UpdateAvatarDisplay(); // โชว์รูปแรกทันทีที่เปิดเกม

        PhotonNetwork.ConnectUsingSettings();
    }

    // --- ระบบปุ่มลูกศรเลือกรูป ---
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
        if (avatarDisplay != null && avatarSprites.Length > 0)
        {
            avatarDisplay.sprite = avatarSprites[currentAvatarIndex];
        }
    }
    // -------------------------

    public override void OnConnectedToMaster()
    {
        SetStatus("Connected!");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        SetStatus("Ready!");
        SetButtonsInteractable(true);
    }

    public override void OnJoinedRoom()
    {
        if (joinRoomPanel != null) joinRoomPanel.SetActive(false);
        if (createRoomPanel != null) createRoomPanel.SetActive(true);
        if (showCodeText != null) showCodeText.text = "Room Code: " + PhotonNetwork.CurrentRoom.Name;

        SetStatus(PhotonNetwork.IsMasterClient ? "You are the Host" : "Joined! Waiting for host...");
        UpdatePlayerListBar();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        SetStatus($"Player {newPlayer.NickName} joined! 🎉");
        UpdatePlayerListBar();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        SetStatus($"Player {otherPlayer.NickName} left 😢");
        UpdatePlayerListBar();
    }

    void UpdatePlayerListBar()
    {
        if (playerBarText == null) return;

        string player1Name = "Waiting...";
        string player2Name = "Waiting...";

        Player[] currentPlayers = PhotonNetwork.PlayerList;

        if (currentPlayers.Length > 0) player1Name = currentPlayers[0].NickName;
        if (currentPlayers.Length > 1) player2Name = currentPlayers[1].NickName;

        playerBarText.text = $"[ {player1Name} ]   VS   [ {player2Name} ]";

        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        }
    }

    public void OnClickCreateRoom()
    {
        SavePlayerNameAndAvatar();
        if (!PhotonNetwork.IsConnectedAndReady || !PhotonNetwork.InLobby) return;

        string randomCode = Random.Range(1000, 10000).ToString();
        RoomOptions roomOptions = new RoomOptions() { MaxPlayers = 2 };
        PhotonNetwork.CreateRoom(randomCode, roomOptions);
        SetStatus("Creating Room...");
    }

    public void OnClickJoinRoom()
    {
        SavePlayerNameAndAvatar();
        string joinCode = roomCodeInput != null ? roomCodeInput.text.Trim() : "";
        if (string.IsNullOrEmpty(joinCode))
        {
            SetStatus("Please enter room code!");
            return;
        }
        SetStatus("Joining Room...");
        PhotonNetwork.JoinRoom(joinCode);
    }

    public void OnClickStartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("MG1_1");
        }
    }

    // อัปเดตฟังก์ชันนี้เพื่อเซฟทั้งชื่อและ ID รูปภาพ
    void SavePlayerNameAndAvatar()
    {
        string pName = nameInputField != null ? nameInputField.text.Trim() : "";
        PhotonNetwork.NickName = !string.IsNullOrEmpty(pName) ? pName : "Player_" + Random.Range(100, 1000);

        Hashtable props = new Hashtable();
        props["AvatarID"] = currentAvatarIndex;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    void SetStatus(string msg) { if (statusText != null) statusText.text = msg; }
    void SetButtonsInteractable(bool state)
    {
        if (createRoomButton != null) createRoomButton.interactable = state;
        if (joinRoomButton != null) joinRoomButton.interactable = state;
    }
}