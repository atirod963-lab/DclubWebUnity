using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Linq;

public class FinalSummaryUI : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public TextMeshProUGUI winnerNameText;
    public TextMeshProUGUI p1ScoreText;
    public TextMeshProUGUI p2ScoreText;

    [Header("Social & Profile")]
    public Image winnerAvatarDisplay;
    public Image loserAvatarDisplay;
    public Sprite[] avatarSprites;
    public Button facebookShareButton;
    public string gameURL = "https://pongsatornthn-art.github.io/DClub-Multiplayer-Web01/";

    private string topWinnerName = "";
    private int topScore = 0;

    void Start()
    {
        if (winnerAvatarDisplay != null) winnerAvatarDisplay.gameObject.SetActive(false);
        if (loserAvatarDisplay != null) loserAvatarDisplay.gameObject.SetActive(false);

        if (facebookShareButton != null) facebookShareButton.onClick.AddListener(ShareToFacebook);

        ShowFinalResults();
    }

    void ShowFinalResults()
    {
        var players = PhotonNetwork.PlayerList
            .OrderByDescending(p => GetScore(p)).ToList();

        if (players.Count >= 1)
            p1ScoreText.text = $"{players[0].NickName}: {GetScore(players[0])} pts";
        if (players.Count >= 2)
            p2ScoreText.text = $"{players[1].NickName}: {GetScore(players[1])} pts";

        if (players.Count > 1 && GetScore(players[0]) == GetScore(players[1]))
        {
            winnerNameText.text = "🏆 เสมอกัน! 🏆";
            topWinnerName = "เสมอ";
            topScore = GetScore(players[0]);
        }
        else if (players.Count > 0)
        {
            winnerNameText.text = $"🏆 {players[0].NickName} WIN! 🏆";
            topWinnerName = players[0].NickName;
            topScore = GetScore(players[0]);
        }
        if (players.Count > 0 && winnerAvatarDisplay != null)
        {
            if (players[0].CustomProperties.ContainsKey("AvatarID"))
            {
                int avatarID = (int)players[0].CustomProperties["AvatarID"];
                if (avatarID >= 0 && avatarID < avatarSprites.Length)
                {
                    winnerAvatarDisplay.sprite = avatarSprites[avatarID];
                    winnerAvatarDisplay.gameObject.SetActive(true);
                }
            }
        }

        if (players.Count > 1 && loserAvatarDisplay != null)
        {
            if (players[1].CustomProperties.ContainsKey("AvatarID"))
            {
                int avatarID = (int)players[1].CustomProperties["AvatarID"];
                if (avatarID >= 0 && avatarID < avatarSprites.Length)
                {
                    loserAvatarDisplay.sprite = avatarSprites[avatarID];
                    loserAvatarDisplay.gameObject.SetActive(true);
                }
            }
        }
    }

    int GetScore(Player player)
    {
        if (player.CustomProperties.TryGetValue("Score", out object score))
            return (int)score;
        return 0;
    }

    void ShareToFacebook()
    {
        var players = PhotonNetwork.PlayerList
            .OrderByDescending(p => GetScore(p)).ToList();

        string shareMessage = "";

        if (players.Count >= 2)
        {
            string p1Name = players[0].NickName;
            int p1Score = GetScore(players[0]);
            string p2Name = players[1].NickName;
            int p2Score = GetScore(players[1]);

            if (p1Score == p2Score)
            {
                shareMessage = $"ดุเดือดจัด! แข่ง 3 มินิเกมรวด ผลออกมาเสมอ! ทั้ง {p1Name} และ {p2Name} ทำคะแนนเท่ากันที่ {p1Score} แต้ม! ใครแน่จริงเข้ามาลองเลย!";
            }
            else
            {
                shareMessage = $"จบเกมแล้ว! 🏆 {p1Name} เอาชนะไปด้วยคะแนน {p1Score} แต้ม! เบียด {p2Name} ที่ทำได้ {p2Score} แต้ม ไปแบบสุดมันส์! มาร่วมสนุกกับ DClub Minigames กัน!";
            }
        }
        else if (players.Count == 1)
        {
            string p1Name = players[0].NickName;
            int p1Score = GetScore(players[0]);
            shareMessage = $"ผม {p1Name} เล่นเคลียร์ 3 มินิเกมรวด ทำคะแนนไปได้ {p1Score} แต้ม! ใครเจ๋งเข้ามาลองเลย!";
        }

        string facebookShareURL = "https://www.facebook.com/sharer/sharer.php?u=" + gameURL + "&quote=" + System.Uri.EscapeUriString(shareMessage);
        Application.OpenURL(facebookShareURL);
    }
    // -------------------------

    public void OnClickBackToLobby()
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
                PhotonNetwork.LoadLevel("menu");
            else
                PhotonNetwork.LeaveRoom();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("menu");
        }
    }

    public override void OnLeftRoom()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("menu");
    }
}