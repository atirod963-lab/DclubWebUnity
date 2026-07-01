using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using ExitGames.Client.Photon;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject hostDashboardPanel;
    public GameObject playerJoinPanel;
    // ลบ playerTeamPanel ออกไปแล้ว เพราะเรารวมหน้ากัน!

    [Header("Host UI")]
    public TextMeshProUGUI roomCodeText;
    public TextMeshProUGUI greenTeamListText;
    public TextMeshProUGUI redTeamListText;
    public Button startGameButton;

    [Header("Player UI")]
    public TMP_InputField playerNameInput;
    public TMP_InputField roomCodeInput;
    public TextMeshProUGUI playerStatusText;
    public TextMeshProUGUI warningText;

    private bool isHost = false;

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();

        if (warningText != null) warningText.gameObject.SetActive(false);
        ShowPanel(mainMenuPanel);
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("เชื่อมต่อเซิร์ฟเวอร์ Photon สำเร็จ!");
        PhotonNetwork.JoinLobby();
    }

    bool CheckNameInput()
    {
        if (playerNameInput == null || string.IsNullOrWhiteSpace(playerNameInput.text))
        {
            if (warningText != null) StartCoroutine(ShowWarningRoutine());
            return false;
        }
        PhotonNetwork.NickName = playerNameInput.text.Trim();
        return true;
    }

    IEnumerator ShowWarningRoutine()
    {
        warningText.gameObject.SetActive(true);
        yield return new WaitForSeconds(2f);
        warningText.gameObject.SetActive(false);
    }

    // ==========================================
    // ฝั่ง HOST
    // ==========================================
    public void OnClickCreateHostRoom()
    {
        if (!CheckNameInput()) return;

        isHost = true;
        string randomCode = Random.Range(1000, 10000).ToString();
        RoomOptions options = new RoomOptions { MaxPlayers = 20 };
        PhotonNetwork.CreateRoom(randomCode, options);
    }

    // ==========================================
    // ฝั่ง PLAYER (ฉบับรวบยอด!)
    // ==========================================
    public void OnClickGoToJoin()
    {
        if (!CheckNameInput()) return;

        isHost = false;
        ShowPanel(playerJoinPanel);
        if (playerStatusText != null) playerStatusText.text = ""; // ล้างข้อความเก่า
    }

    // ฟังก์ชันใหม่: กรอกรหัสปุ๊บ เข้าห้องพร้อมเลือกทีมเลย
    public void OnClickJoinRoomWithTeam(string teamName)
    {
        string code = roomCodeInput.text.Trim();

        if (string.IsNullOrEmpty(code))
        {
            if (playerStatusText != null) playerStatusText.text = "กรุณาใส่รหัสห้องก่อนกดเลือกทีม!";
            return;
        }

        // เซ็ตทีมและบทบาทรอไว้เลย ตั้งแต่ยังไม่เข้าห้อง
        Hashtable props = new Hashtable { { "Team", teamName }, { "Role", "Player" } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        if (playerStatusText != null) playerStatusText.text = "กำลังเข้าห้อง... (ทีม " + teamName + ")";

        PhotonNetwork.JoinRoom(code);
    }

    public void OnClickSinglePlayer() { SceneManager.LoadScene("MG1_1"); }
    public void OnClickBackToMain() { ShowPanel(mainMenuPanel); }

    // ==========================================
    // ระบบทำงานอัตโนมัติเมื่อเข้าห้องสำเร็จ
    // ==========================================
    public override void OnJoinedRoom()
    {
        Hashtable props = new Hashtable();

        if (isHost)
        {
            ShowPanel(hostDashboardPanel);
            if (roomCodeText != null) roomCodeText.text = PhotonNetwork.CurrentRoom.Name;

            props["Role"] = "Spectator";
            props["Team"] = "None";
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
        else
        {
            // ของ Player ไม่ต้องเปลี่ยนหน้าแล้ว ให้อยู่หน้าเดิมแต่ขึ้นข้อความบอกว่าสำเร็จ
            if (playerStatusText != null)
            {
                string myTeam = (string)PhotonNetwork.LocalPlayer.CustomProperties["Team"];
                playerStatusText.text = "เข้าห้องสำเร็จ! รอโฮสต์กดเริ่มเกม... (ทีม " + myTeam + ")";
            }
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        // แถบแจ้งเตือนเผื่อผู้เล่นกรอกรหัสผิดห้อง
        if (playerStatusText != null) playerStatusText.text = "ไม่พบห้องนี้! ตรวจสอบรหัสอีกครั้ง";
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) { UpdateHostUI(); }
    public override void OnPlayerLeftRoom(Player otherPlayer) { UpdateHostUI(); }
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) { UpdateHostUI(); }

    void UpdateHostUI()
    {
        if (!isHost) return;

        string greenList = "Green Team:\n";
        string redList = "Red Team:\n";

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.ContainsKey("Role") && (string)p.CustomProperties["Role"] == "Player")
            {
                string team = p.CustomProperties.ContainsKey("Team") ? (string)p.CustomProperties["Team"] : "None";
                if (team == "Green") greenList += p.NickName + "\n";
                else if (team == "Red") redList += p.NickName + "\n";
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
}