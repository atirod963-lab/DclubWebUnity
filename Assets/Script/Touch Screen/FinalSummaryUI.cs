using System.Collections;
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

    [Header("Social & Profile")]
    public Image winnerAvatarDisplay; // ช่องโชว์รูปคนชนะ
    // ---------------------------------------------------------
    // 🖼️ [เพิ่มใหม่] ช่องสำหรับลากกรอบโชว์รูปคนแพ้มาใส่
    public Image loserAvatarDisplay;
    // ---------------------------------------------------------
    public Sprite[] avatarSprites;    // ลากรูปเซ็ตเดิม เรียงให้ตรงกับหน้า Lobby
    public Button facebookShareButton;
    public string gameURL = "https://pongsatornthn-art.github.io/DClub-Multiplayer-Web01/";

    private string topWinnerName = "";
    private int topScore = 0;

    void Start()
    {
        // ซ่อนกรอบรูปไว้ก่อนตอนเริ่มซีนเพื่อความชื่นมื่น เผื่อเกิดข้อผิดพลาดจะได้ไม่ขึ้นกรอบขาว
        if (winnerAvatarDisplay != null) winnerAvatarDisplay.gameObject.SetActive(false);
        if (loserAvatarDisplay != null) loserAvatarDisplay.gameObject.SetActive(false);

        if (facebookShareButton != null) facebookShareButton.onClick.AddListener(ShareToFacebook);

        ShowFinalResults();
    }

    void ShowFinalResults()
    {
        // จัดเรียงรายชื่อผู้เล่นตามคะแนนจากมากไปน้อย (index 0 คือที่ 1 / index 1 คือที่ 2)
        var players = PhotonNetwork.PlayerList
            .OrderByDescending(p => GetScore(p)).ToList();

        if (players.Count >= 1)
            p1ScoreText.text = $"{players[0].NickName}: {GetScore(players[0])} pts";
        if (players.Count >= 2)
            p2ScoreText.text = $"{players[1].NickName}: {GetScore(players[1])} pts";

        // กรณีที่ 1: คะแนนเสมอกัน
        if (players.Count > 1 && GetScore(players[0]) == GetScore(players[1]))
        {
            winnerNameText.text = "🏆 เสมอกัน! 🏆";
            topWinnerName = "เสมอ";
            topScore = GetScore(players[0]);

            // ถ้าเสมอกัน ให้โชว์รูปทั้งคู่ในกรอบของตัวเองไปเลยครับ
            SetPlayerAvatar(players[0], winnerAvatarDisplay);
            SetPlayerAvatar(players[1], loserAvatarDisplay);
        }
        // กรณีที่ 2: มีคนชนะชัดเจน
        else if (players.Count > 0)
        {
            winnerNameText.text = $"🏆 {players[0].NickName} WIN! 🏆";
            topWinnerName = players[0].NickName;
            topScore = GetScore(players[0]);

            // 🥇 สั่งโชว์รูปคนชนะ (คนที่ได้ที่ 1)
            SetPlayerAvatar(players[0], winnerAvatarDisplay);

            // 🥈 สั่งโชว์รูปคนแพ้ (คนที่ได้ที่ 2)
            if (players.Count >= 2)
            {
                SetPlayerAvatar(players[1], loserAvatarDisplay);
            }
        }
    }

    // 🛠️ ฟังก์ชันตัวช่วยดึงรูปอวาตาร์มาใส่กรอบอ้างอิงตาม Custom Properties
    void SetPlayerAvatar(Player player, Image targetImage)
    {
        if (targetImage == null) return;

        if (player.CustomProperties.ContainsKey("Avatar"))
        {
            int avatarID = (int)player.CustomProperties["Avatar"];
            if (avatarID >= 0 && avatarID < avatarSprites.Length)
            {
                targetImage.sprite = avatarSprites[avatarID];
                targetImage.gameObject.SetActive(true); //เปิดการแสดงผลรูปภาพ
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