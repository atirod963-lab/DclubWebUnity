using UnityEngine;
using UnityEngine.UI; // ต้องมีบรรทัดนี้เพื่อใช้ Button และ Image
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

    [Header("Social & Profile (เพิ่มใหม่)")]
    public Image winnerAvatarDisplay; // ลากช่องโชว์รูปคนชนะมาใส่
    public Sprite[] avatarSprites;    // ลากรูปเซ็ตเดิม เรียงให้ตรงกับหน้า Lobby
    public Button facebookShareButton;
    public string gameURL = "https://pongsatornthn-art.github.io/DClub-Multiplayer-Web01/";

    private string topWinnerName = "";
    private int topScore = 0;

    void Start()
    {
        if (winnerAvatarDisplay != null) winnerAvatarDisplay.gameObject.SetActive(false);
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

            // ดึง ID รูปโปรไฟล์มาแสดงผล
            if (players[0].CustomProperties.ContainsKey("AvatarID") && winnerAvatarDisplay != null)
            {
                int avatarID = (int)players[0].CustomProperties["AvatarID"];
                if (avatarID >= 0 && avatarID < avatarSprites.Length)
                {
                    winnerAvatarDisplay.sprite = avatarSprites[avatarID];
                    winnerAvatarDisplay.gameObject.SetActive(true);
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

    // --- ระบบแชร์ Facebook ---
    void ShareToFacebook()
    {
        string shareMessage = "";
        if (topWinnerName == "เสมอ")
            shareMessage = $"ดุเดือดจัด! แข่ง 3 มินิเกมรวด เสมอกันไปที่ {topScore} แต้ม! ใครแน่จริงเข้ามาลองเลย!";
        else
            shareMessage = $"ผม {topWinnerName} เอาชนะมินิเกม 3 ด่านรวดด้วยคะแนน {topScore} แต้ม! ใครเจ๋งเข้ามาลองเลย!";

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