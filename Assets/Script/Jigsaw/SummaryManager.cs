using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using ExitGames.Client.Photon;

public class SummaryManager : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    public TextMeshProUGUI team1TimeText;
    public TextMeshProUGUI team2TimeText;
    public TextMeshProUGUI winnerText;
    public GameObject waitingPanel;

    [Header("Social & Profile (อัปเดตใหม่)")]
    public Button facebookShareButton;
    public TextMeshProUGUI winnerNamesText;
    public string gameURL = "https://pongsatornthn-art.github.io/DClub-Multiplayer-Web01/";

    [Header("แสดงรูปอวตารทีมที่ชนะ")]
    public Image[] winnerAvatarDisplays; // ช่องแสดงรูป (ทีมละ 2 คน ก็สร้างไว้ 2 ช่อง)
    public Sprite[] avatarSprites; // ลากรูปทั้งหมดมาใส่ (ต้องเรียงลำดับให้เป๊ะกับหน้า Lobby)

    private bool team1Done = false;
    private bool team2Done = false;
    private float finalTimeT1 = 0f;
    private float finalTimeT2 = 0f;
    private int winningTeam = 0;

    void Start()
    {
        waitingPanel.SetActive(true);
        if (facebookShareButton != null) facebookShareButton.gameObject.SetActive(false);
        if (winnerNamesText != null) winnerNamesText.text = "";

        // ปิดรูปอวตารไปก่อน จนกว่าจะรู้ผล
        foreach (var img in winnerAvatarDisplays) { if (img != null) img.gameObject.SetActive(false); }

        TryShowResult();
    }

    void TryShowResult()
    {
        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        float t1 = props.ContainsKey("Team1Time") ? (float)props["Team1Time"] : -1f;
        float t2 = props.ContainsKey("Team2Time") ? (float)props["Team2Time"] : -1f;

        if (t1 >= 0) { ShowTeamTime(1, t1); team1Done = true; finalTimeT1 = t1; }
        if (t2 >= 0) { ShowTeamTime(2, t2); team2Done = true; finalTimeT2 = t2; }

        if (team1Done && team2Done) ShowWinner(finalTimeT1, finalTimeT2);
    }

    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        if (changedProps.ContainsKey("Team1Time"))
        {
            finalTimeT1 = (float)changedProps["Team1Time"];
            ShowTeamTime(1, finalTimeT1);
            team1Done = true;
        }
        if (changedProps.ContainsKey("Team2Time"))
        {
            finalTimeT2 = (float)changedProps["Team2Time"];
            ShowTeamTime(2, finalTimeT2);
            team2Done = true;
        }

        if (team1Done && team2Done) ShowWinner(finalTimeT1, finalTimeT2);
    }

    void ShowTeamTime(int team, float seconds)
    {
        string formatted = FormatTime(seconds);
        if (team == 1) team1TimeText.text = $"ทีม 1:  {formatted}";
        else team2TimeText.text = $"ทีม 2:  {formatted}";
    }

    void ShowWinner(float t1, float t2)
    {
        waitingPanel.SetActive(false);

        if (t1 < t2) { winnerText.text = "🏆 ทีม 1 ชนะ!"; winningTeam = 1; }
        else if (t2 < t1) { winnerText.text = "🏆 ทีม 2 ชนะ!"; winningTeam = 2; }
        else { winnerText.text = "เสมอกัน!"; winningTeam = 0; }

        string winnerNames = "";
        int avatarIndexUI = 0;

        foreach (var p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.ContainsKey("Team") && (int)p.CustomProperties["Team"] == winningTeam)
            {
                winnerNames += p.NickName + " ";

                // ระบบดึงรูปอวตารมาโชว์
                if (p.CustomProperties.ContainsKey("AvatarID") && avatarIndexUI < winnerAvatarDisplays.Length)
                {
                    int avatarID = (int)p.CustomProperties["AvatarID"];
                    if (avatarID >= 0 && avatarID < avatarSprites.Length)
                    {
                        winnerAvatarDisplays[avatarIndexUI].sprite = avatarSprites[avatarID];
                        winnerAvatarDisplays[avatarIndexUI].gameObject.SetActive(true);
                        avatarIndexUI++;
                    }
                }
            }
        }

        if (winningTeam != 0 && winnerNamesText != null) winnerNamesText.text = "MVP: " + winnerNames;
        if (facebookShareButton != null) facebookShareButton.gameObject.SetActive(true);
    }

    string FormatTime(float seconds)
    {
        int m = (int)(seconds / 60);
        int s = (int)(seconds % 60);
        int ms = (int)((seconds - Mathf.Floor(seconds)) * 100);
        return $"{m:00}:{s:00}.{ms:00}";
    }
}