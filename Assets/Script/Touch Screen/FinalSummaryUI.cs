using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Linq;
using System.Runtime.InteropServices;

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

    [DllImport("__Internal")]
    private static extern void DownloadScreenshotJS(byte[] byteData, int byteLength, string fileName);

    void Start()
    {
        if (winnerNameText != null) winnerNameText.text = "กำลังคำนวณผลลัพธ์...";
        if (p1ScoreText != null) p1ScoreText.text = "";
        if (p2ScoreText != null) p2ScoreText.text = "";

        if (winnerAvatarDisplay != null) winnerAvatarDisplay.gameObject.SetActive(false);
        if (loserAvatarDisplay != null) loserAvatarDisplay.gameObject.SetActive(false);

        if (facebookShareButton != null) facebookShareButton.onClick.AddListener(ShareToFacebook);

        StartCoroutine(WaitAndSyncBeforeResults());
    }

    IEnumerator WaitAndSyncBeforeResults()
    {
        yield return new WaitForSeconds(1.5f);
        ShowFinalResults();
    }

    void ShowFinalResults()
    {
        if (!PhotonNetwork.InRoom)
        {
            ShowSinglePlayerResults();
            return;
        }

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
            winnerNameText.text = $" {players[0].NickName} WIN! ";
            topWinnerName = players[0].NickName;
            topScore = GetScore(players[0]);

            SetPlayerAvatar(players[0], winnerAvatarDisplay);

            if (players.Count >= 2)
            {
                SetPlayerAvatar(players[1], loserAvatarDisplay);
            }
        }
    }

    void ShowSinglePlayerResults()
    {
        string offlineName = PlayerPrefs.GetString("OfflineName", "Player");
        int offlineAvatar = PlayerPrefs.GetInt("OfflineAvatar", 0);

        int offlineScore = 0;
        if (TouchManager2D.Instance != null) offlineScore = TouchManager2D.Instance.score;

        winnerNameText.text = $"🏆 {offlineName} WIN! 🏆";
        p1ScoreText.text = $"{offlineName}: {offlineScore} pts";
        if (p2ScoreText != null) p2ScoreText.text = "";

        topWinnerName = offlineName;
        topScore = offlineScore;

        if (winnerAvatarDisplay != null && offlineAvatar >= 0 && offlineAvatar < avatarSprites.Length)
        {
            winnerAvatarDisplay.sprite = avatarSprites[offlineAvatar];
            winnerAvatarDisplay.gameObject.SetActive(true);
        }

        if (loserAvatarDisplay != null) loserAvatarDisplay.gameObject.SetActive(false);
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

    public void OnClickTakeScreenshot()
    {
        StartCoroutine(CaptureScreenRoutine());
    }

    IEnumerator CaptureScreenRoutine()
    {
        yield return new WaitForEndOfFrame();

        int width = Screen.width;
        int height = Screen.height;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);

        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        Destroy(tex);

        string fileName = "DClub_Score_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";

#if UNITY_WEBGL && !UNITY_EDITOR
        DownloadScreenshotJS(bytes, bytes.Length, fileName);
#else
        string savePath = Application.dataPath + "/../" + fileName;
        System.IO.File.WriteAllBytes(savePath, bytes);
        Debug.Log("📸 แคปหน้าจอสำเร็จ! ไฟล์เซฟไว้ที่: " + savePath);
#endif
    }

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
        if (TouchManager2D.Instance != null)
        {
            Destroy(TouchManager2D.Instance.gameObject);
        }

        if (PhotonNetwork.InRoom)
        {
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