using UnityEngine;
using Photon.Pun;
using TMPro;
using System.Collections; // สำคัญมาก สำหรับทำเอฟเฟกต์ดีเลย์
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

        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Role"))
        {
            string role = (string)PhotonNetwork.LocalPlayer.CustomProperties["Role"];
            bool isHost = (role == "Spectator");

            if (playerViewPanel != null) playerViewPanel.SetActive(!isHost);
            if (hostViewPanel != null) hostViewPanel.SetActive(isHost);
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("GlobalGreenScore"))
            currentGreenScore = (int)PhotonNetwork.CurrentRoom.CustomProperties["GlobalGreenScore"];

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("GlobalRedScore"))
            currentRedScore = (int)PhotonNetwork.CurrentRoom.CustomProperties["GlobalRedScore"];

        UpdateScoreUI();
    }

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
            photonView.RPC("RPC_AddScoreInstant", RpcTarget.All, myTeam);
        }
    }

    [PunRPC]
    void RPC_AddScoreInstant(string team)
    {
        if (team == "Green")
        {
            currentGreenScore++;
            StartCoroutine(ScorePopTrick(greenScoreText, Color.green)); // เด้งสีเขียว
        }
        else if (team == "Red")
        {
            currentRedScore++;
            StartCoroutine(ScorePopTrick(redScoreText, Color.red)); // เด้งสีแดง
        }

        UpdateScoreUI();

        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable hash = new Hashtable();
            hash.Add("GlobalGreenScore", currentGreenScore);
            hash.Add("GlobalRedScore", currentRedScore);
            PhotonNetwork.CurrentRoom.SetCustomProperties(hash);
        }
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
            photonView.RPC("RPC_SubtractScoreInstant", RpcTarget.All, myTeam);
        }
    }

    [PunRPC]
    void RPC_SubtractScoreInstant(string team)
    {
        if (team == "Green")
        {
            currentGreenScore--;
            StartCoroutine(ScorePopTrick(greenScoreText, Color.gray)); // โดนหักแต้ม แฟลชสีเทา
        }
        else if (team == "Red")
        {
            currentRedScore--;
            StartCoroutine(ScorePopTrick(redScoreText, Color.gray)); // โดนหักแต้ม แฟลชสีเทา
        }

        UpdateScoreUI();

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


    IEnumerator ScorePopTrick(TextMeshProUGUI scoreText, Color flashColor)
    {
        if (scoreText == null) yield break;

        Vector3 originalScale = scoreText.transform.localScale;

        scoreText.color = flashColor;
        scoreText.transform.localScale = originalScale * 1.5f;

        yield return new WaitForSeconds(0.15f);

        scoreText.transform.localScale = originalScale;
        scoreText.color = Color.white;
    }
}