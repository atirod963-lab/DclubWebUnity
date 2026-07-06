using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Linq;
using System.Runtime.InteropServices; // 🌟 [เพิ่มใหม่] เพื่อให้คุยกับไฟล์ .jslib ได้

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

    // ---------------------------------------------------------
    // 📸 [เพิ่มใหม่] ประกาศเรียกใช้สะพานเชื่อมกับเว็บ (jslib)
    // ---------------------------------------------------------
    [DllImport("__Internal")]
    private static extern void DownloadScreenshotJS(byte[] byteData, int byteLength, string fileName);

    void Start()
    {
        if (winnerAvatarDisplay != null) winnerAvatarDisplay.gameObject.SetActive(false);
        if (loserAvatarDisplay != null) loserAvatarDisplay.gameObject.SetActive(false);

        if (facebookShareButton != null) facebookShareButton.onClick.AddListener(ShareToFacebook);

        ShowFinalResults();
    }

    void ShowFinalResults()
    {
        var players = PhotonNetwork.PlayerList.OrderByDescending(p => GetScore(p)).ToList();

        if (players.Count >= 1)
            p1ScoreText.text = $"{players[0].NickName}: {GetScore(players[0])} pts";
        if (players.Count >= 2)
            p2ScoreText.text = $"{players[1].NickName}: {GetScore(players[1])} pts";

        if (players.Count > 1 && GetScore(players[0]) == GetScore(players[1]))
        {
            winnerNameText.text = "🏆 เสมอกัน! 🏆";
            topWinnerName = "เสมอ";
            topScore = GetScore(players[0]);

            SetPlayerAvatar(players[0], winnerAvatarDisplay);
            SetPlayerAvatar(players[1], loserAvatarDisplay);
        }
        else if (players.Count > 0)
        {
            winnerNameText.text = $"🏆 {players[0].NickName} WIN! 🏆";
            topWinnerName = players[0].NickName;
            topScore = GetScore(players[0]);

            SetPlayerAvatar(players[0], winnerAvatarDisplay);

            if (players.Count >= 2)
            {
                SetPlayerAvatar(players[1], loserAvatarDisplay);
            }
        }
    }

    void SetPlayerAvatar(Player player, Image targetImage)
    {
        if (targetImage == null) return;
        if (player.CustomProperties.ContainsKey("Avatar"))
        {
            int avatarID = (int)player.CustomProperties["Avatar"];
            if (avatarID >= 0 && avatarID < avatarSprites.Length)
            {
                targetImage.sprite = avatarSprites[avatarID];
                targetImage.gameObject.SetActive(true);
            }
        }
    }

    int GetScore(Player player)
    {
        if (player.CustomProperties.TryGetValue("Score", out object score))
            return (int)score;
        return 0;
    }

    // ==========================================
    // 📸 [เพิ่มใหม่] ระบบแคปหน้าจอ (Screenshot)
    // ==========================================
    public void OnClickTakeScreenshot()
    {
        StartCoroutine(CaptureScreenRoutine());
    }

    IEnumerator CaptureScreenRoutine()
    {
        // ปิดปุ่มแชร์หรือปุ่มเมนูตรงนี้ได้ (ถ้าปอตั้งตัวแปรไว้) เพื่อให้ภาพออกมาคลีนๆ
        // yield return new WaitForSeconds(0.1f); // รอสักนิดถ้ามี UI กำลังปิด

        yield return new WaitForEndOfFrame(); // สำคัญมาก! ต้องรอให้ภาพเรนเดอร์จบเฟรมก่อนค่อยแคป

        int width = Screen.width;
        int height = Screen.height;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);

        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        Destroy(tex);

        string fileName = "DClub_Score_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";

#if UNITY_WEBGL && !UNITY_EDITOR
        // ถ้าเล่นบนเว็บ สั่งให้เบราว์เซอร์ดาวน์โหลดรูปลงคอม
        DownloadScreenshotJS(bytes, bytes.Length, fileName);
#else
        // ถ้าเทสต์ใน Unity ให้เซฟไฟล์ไว้ในโฟลเดอร์โปรเจกต์
        string savePath = Application.dataPath + "/../" + fileName;
        System.IO.File.WriteAllBytes(savePath, bytes);
        Debug.Log("📸 แคปหน้าจอสำเร็จ! ไฟล์เซฟไว้ที่: " + savePath);
#endif
    }
    // ==========================================

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