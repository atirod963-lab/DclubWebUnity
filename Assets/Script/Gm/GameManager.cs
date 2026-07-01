using UnityEngine;
using Photon.Pun;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    [Header("UI Views")]
    public GameObject playerViewPanel;
    public GameObject hostViewPanel;

    [Header("Host Score UI")]
    public TextMeshProUGUI greenScoreText;
    public TextMeshProUGUI redScoreText;

    // ตัวแปรเก็บคะแนนของด่านนี้
    private int currentGreenScore = 0;
    private int currentRedScore = 0;

    public bool isGameActive = true;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (!PhotonNetwork.InRoom) return;

        // 1. เปิด/ปิด หน้าจอตามบทบาท
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Role"))
        {
            string role = (string)PhotonNetwork.LocalPlayer.CustomProperties["Role"];
            bool isHost = (role == "Spectator");

            if (playerViewPanel != null) playerViewPanel.SetActive(!isHost);
            if (hostViewPanel != null) hostViewPanel.SetActive(isHost);
        }

        // 2. โหลดคะแนนสะสมจากด่านที่แล้วมาโชว์ตอนเริ่มเกม
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("GlobalGreenScore"))
            currentGreenScore = (int)PhotonNetwork.CurrentRoom.CustomProperties["GlobalGreenScore"];

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("GlobalRedScore"))
            currentRedScore = (int)PhotonNetwork.CurrentRoom.CustomProperties["GlobalRedScore"];

        UpdateScoreUI();
    }

    // สคริปต์อาหาร (TouchManager2D) จะเรียกใช้ฟังก์ชันนี้
    public void AddScoreToMyTeam()
    {
        if (!isGameActive || !PhotonNetwork.InRoom) return;

        string myTeam = "None";
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Team"))
        {
            myTeam = (string)PhotonNetwork.LocalPlayer.CustomProperties["Team"];
        }

        if (myTeam == "Green" || myTeam == "Red")
        {
            // ส่งคำสั่งอัปเดตไปที่หน้าจอของทุกคนแบบ "ทันที" (รวมถึงหน้าโฮสต์ด้วย)
            photonView.RPC("RPC_AddScoreInstant", RpcTarget.All, myTeam);
        }
    }

    // ฟังก์ชันนี้ทำงานบนหน้าจอของทุกคนพร้อมกัน
    [PunRPC]
    void RPC_AddScoreInstant(string team)
    {
        // 1. บวกคะแนนและเปลี่ยนตัวเลขบนหน้าจอทันที!
        if (team == "Green") currentGreenScore++;
        else if (team == "Red") currentRedScore++;

        UpdateScoreUI();

        // 2. ให้โฮสต์ทำหน้าที่แอบเซฟคะแนนล่าสุดลงกระดานดำ (เพื่อเอาไปใช้ด่านหน้า)
        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable hash = new Hashtable();
            hash.Add("GlobalGreenScore", currentGreenScore);
            hash.Add("GlobalRedScore", currentRedScore);
            PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
        }
    }

    void UpdateScoreUI()
    {
        if (greenScoreText != null) greenScoreText.text = "Score: " + currentGreenScore;
        if (redScoreText != null) redScoreText.text = "Score: " + currentRedScore;
    }
    public void SubtractScoreToMyTeam()
    {
        if (!isGameActive || !PhotonNetwork.InRoom) return;

        string myTeam = "None";
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Team"))
        {
            myTeam = (string)PhotonNetwork.LocalPlayer.CustomProperties["Team"];
        }

        if (myTeam == "Green" || myTeam == "Red")
        {
            // ส่งคำสั่งลบคะแนนไปบอกทุกคน (รวมถึง Host)
            photonView.RPC("RPC_SubtractScoreInstant", RpcTarget.All, myTeam);
        }
    }

    [PunRPC]
    void RPC_SubtractScoreInstant(string team)
    {
        if (team == "Green") currentGreenScore--;
        else if (team == "Red") currentRedScore--;

        UpdateScoreUI();

        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable hash = new Hashtable();
            hash.Add("GlobalGreenScore", currentGreenScore);
            hash.Add("GlobalRedScore", currentRedScore);
            PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
        }
    }
}