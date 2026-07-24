using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using UnityEngine.SceneManagement;
using System.Collections;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[System.Serializable]
public class PlayerSummarySlot
{
    public GameObject slotRoot;
    public Image avatarImage;
    public TextMeshProUGUI nameText;
}

public class SummaryManager : MonoBehaviourPunCallbacks
{
    [Header("UI เวลาและผลลัพธ์")]
    public TextMeshProUGUI winTimeText;
    public TextMeshProUGUI winnerText;
    public GameObject waitingPanel;

    [Header("🌟 โซนทีมชนะ (โพเดียม)")]
    public PlayerSummarySlot[] winnerSlots;

    [Header("🌟 โซนทีมแพ้ (แนวตั้ง)")]
    public PlayerSummarySlot[] loserSlots;

    [Header("รูปอวตารทั้งหมด")]
    public Sprite[] avatarSprites;

    [Header("Social & ปุ่มต่างๆ")]
    public Button facebookShareButton;
    public Button captureScreenButton;
    public Button returnToLobbyButton;
    public Button viewPuzzleButton;
    public GameObject finishedPuzzlePanel;
    public Button closePuzzlePanelButton;
    public string gameURL = "https://pongsatornthn-art.github.io/DClub-Multiplayer-Web01/";

    private bool team1Done = false;
    private bool team2Done = false;

    private float finalTimeT1 = 9999f;
    private float finalTimeT2 = 9999f;
    private int winningTeam = 0;

    void Start()
    {
        waitingPanel.SetActive(true);
        if (facebookShareButton != null) facebookShareButton.gameObject.SetActive(false);
        if (finishedPuzzlePanel != null) finishedPuzzlePanel.SetActive(false);

        HideAllSlots(winnerSlots);
        HideAllSlots(loserSlots);

        if (facebookShareButton != null) facebookShareButton.onClick.AddListener(ShareToFacebook);
        if (captureScreenButton != null) captureScreenButton.onClick.AddListener(CaptureScreen);
        if (returnToLobbyButton != null) returnToLobbyButton.onClick.AddListener(ReturnToLobby);
        if (viewPuzzleButton != null) viewPuzzleButton.onClick.AddListener(OpenPuzzleView);
        if (closePuzzlePanelButton != null) closePuzzlePanelButton.onClick.AddListener(ClosePuzzleView);

        TryShowResult();
    }

    void TryShowResult()
    {
        if (PlayerPrefs.GetInt("IsSoloGame", 0) == 1)
        {
            float soloTime = PlayerPrefs.GetFloat("SoloFinalTime", 0f);
            ShowSoloResult(soloTime);
            return;
        }

        if (PhotonNetwork.CurrentRoom != null)
        {
            var props = PhotonNetwork.CurrentRoom.CustomProperties;
            float t1 = props.ContainsKey("Team1Time") ? (float)props["Team1Time"] : -1f;
            float t2 = props.ContainsKey("Team2Time") ? (float)props["Team2Time"] : -1f;

            if (t1 >= 0) { team1Done = true; finalTimeT1 = t1; }
            if (t2 >= 0) { team2Done = true; finalTimeT2 = t2; }

            if (team1Done || team2Done) ShowWinner(finalTimeT1, finalTimeT2);
        }
    }

    void ShowSoloResult(float time)
    {
        waitingPanel.SetActive(false);

        winnerText.text = "🎉 ยินดีด้วย! คุณต่อสำเร็จแล้ว!";
        if (winTimeText != null) winTimeText.text = "เวลาของคุณ: " + FormatTime(time);

        if (winnerSlots.Length > 0)
        {
            string myName = PhotonNetwork.NickName;
            int myAvatar = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("AvatarID") ?
                           (int)PhotonNetwork.LocalPlayer.CustomProperties["AvatarID"] : 0;

            SetupSlot(winnerSlots[0], myName, myAvatar);
        }

        if (facebookShareButton != null) facebookShareButton.gameObject.SetActive(true);
    }

    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        if (changedProps.ContainsKey("Team1Time")) { finalTimeT1 = (float)changedProps["Team1Time"]; team1Done = true; }
        if (changedProps.ContainsKey("Team2Time")) { finalTimeT2 = (float)changedProps["Team2Time"]; team2Done = true; }

        if (team1Done || team2Done) ShowWinner(finalTimeT1, finalTimeT2);
    }

    void ShowWinner(float t1, float t2)
    {
        waitingPanel.SetActive(false);

        if (t1 < t2) { winnerText.text = "🏆 ทีม 1 ชนะ!"; winningTeam = 1; if (winTimeText != null) winTimeText.text = "เวลา: " + FormatTime(t1); }
        else if (t2 < t1) { winnerText.text = "🏆 ทีม 2 ชนะ!"; winningTeam = 2; if (winTimeText != null) winTimeText.text = "เวลา: " + FormatTime(t2); }
        else { winnerText.text = "เสมอกัน!"; winningTeam = 0; if (winTimeText != null) winTimeText.text = "เวลา: " + FormatTime(t1); }

        int winIndex = 0;
        int loseIndex = 0;

        foreach (var p in PhotonNetwork.PlayerList)
        {
            // 🌟 [เพิ่มใหม่] ถ้าคนนี้คือ MasterClient (คนสร้างห้อง/Host) ให้ข้ามไปเลย ไม่ต้องเอามาโชว์
            if (p.IsMasterClient) continue;

            int playerTeam = p.CustomProperties.ContainsKey("Team") ? (int)p.CustomProperties["Team"] : -1;
            int avatarID = p.CustomProperties.ContainsKey("AvatarID") ? (int)p.CustomProperties["AvatarID"] : 0;

            // 🌟 [เพิ่มใหม่] กรองให้โชว์เฉพาะคนที่อยู่ทีม 1 หรือ 2 เท่านั้น
            if (playerTeam == 1 || playerTeam == 2)
            {
                if (playerTeam == winningTeam || (winningTeam == 0 && playerTeam == 1))
                {
                    if (winIndex < winnerSlots.Length)
                    {
                        SetupSlot(winnerSlots[winIndex], p.NickName, avatarID);
                        winIndex++;
                    }
                }
                else
                {
                    if (loseIndex < loserSlots.Length)
                    {
                        SetupSlot(loserSlots[loseIndex], p.NickName, avatarID);
                        loseIndex++;
                    }
                }
            }
        }

        if (facebookShareButton != null) facebookShareButton.gameObject.SetActive(true);
    }

    void SetupSlot(PlayerSummarySlot slot, string pName, int avatarID)
    {
        if (slot.slotRoot == null) return;
        slot.slotRoot.SetActive(true);
        if (slot.nameText != null) slot.nameText.text = pName;
        if (slot.avatarImage != null && avatarID >= 0 && avatarID < avatarSprites.Length)
        {
            slot.avatarImage.sprite = avatarSprites[avatarID];
        }
    }

    void HideAllSlots(PlayerSummarySlot[] slots)
    {
        foreach (var slot in slots)
        {
            if (slot.slotRoot != null) slot.slotRoot.SetActive(false);
        }
    }

    string FormatTime(float seconds)
    {
        int m = (int)(seconds / 60);
        int s = (int)(seconds % 60);
        int ms = (int)((seconds - Mathf.Floor(seconds)) * 100);
        return $"{m:00}:{s:00}.{ms:00}";
    }

    public void ShareToFacebook() { Application.OpenURL("https://www.facebook.com/sharer/sharer.php?u=" + UnityEngine.Networking.UnityWebRequest.EscapeURL(gameURL)); }
    public void CaptureScreen() { StartCoroutine(TakeScreenshotRoutine()); }
    IEnumerator TakeScreenshotRoutine() { yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot("Winner_Summary_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png"); }
    public void ReturnToLobby() { PhotonNetwork.LeaveRoom(); }
    public override void OnLeftRoom() { SceneManager.LoadScene("menu_Jigsaw"); }
    public void OpenPuzzleView() { if (finishedPuzzlePanel != null) finishedPuzzlePanel.SetActive(true); }
    public void ClosePuzzleView() { if (finishedPuzzlePanel != null) finishedPuzzlePanel.SetActive(false); }
}