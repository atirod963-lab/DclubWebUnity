using UnityEngine;
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

    private bool team1Done = false;
    private bool team2Done = false;

    // -------------------------------------------------------
    void Start()
    {
        waitingPanel.SetActive(true);
        TryShowResult();
    }

    // -------------------------------------------------------
    //  อ่าน Room Props ตอนเข้า Scene (ถ้าอีกทีมจบไปก่อนแล้ว)
    // -------------------------------------------------------
    void TryShowResult()
    {
        var props = PhotonNetwork.CurrentRoom.CustomProperties;

        float t1 = props.ContainsKey(JigsawGameManager.PROP_TEAM1_TIME)
                   ? (float)props[JigsawGameManager.PROP_TEAM1_TIME] : -1f;
        float t2 = props.ContainsKey(JigsawGameManager.PROP_TEAM2_TIME)
                   ? (float)props[JigsawGameManager.PROP_TEAM2_TIME] : -1f;

        if (t1 >= 0) { ShowTeamTime(1, t1); team1Done = true; }
        if (t2 >= 0) { ShowTeamTime(2, t2); team2Done = true; }

        if (team1Done && team2Done)
            ShowWinner(t1, t2);
    }

    // -------------------------------------------------------
    //  PHOTON CALLBACK: รับ update เมื่ออีกทีมเล่นจบ
    // -------------------------------------------------------
    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        if (changedProps.ContainsKey(JigsawGameManager.PROP_TEAM1_TIME))
        {
            ShowTeamTime(1, (float)changedProps[JigsawGameManager.PROP_TEAM1_TIME]);
            team1Done = true;
        }
        if (changedProps.ContainsKey(JigsawGameManager.PROP_TEAM2_TIME))
        {
            ShowTeamTime(2, (float)changedProps[JigsawGameManager.PROP_TEAM2_TIME]);
            team2Done = true;
        }

        if (team1Done && team2Done)
        {
            float t1 = (float)PhotonNetwork.CurrentRoom.CustomProperties[JigsawGameManager.PROP_TEAM1_TIME];
            float t2 = (float)PhotonNetwork.CurrentRoom.CustomProperties[JigsawGameManager.PROP_TEAM2_TIME];
            ShowWinner(t1, t2);
        }
    }

    // -------------------------------------------------------
    //  DISPLAY HELPERS
    // -------------------------------------------------------
    void ShowTeamTime(int team, float seconds)
    {
        string formatted = FormatTime(seconds);
        if (team == 1) team1TimeText.text = $"ทีม 1:  {formatted}";
        else team2TimeText.text = $"ทีม 2:  {formatted}";
    }

    void ShowWinner(float t1, float t2)
    {
        waitingPanel.SetActive(false);

        if (t1 < t2)
            winnerText.text = "🏆 ทีม 1 ชนะ!";
        else if (t2 < t1)
            winnerText.text = "🏆 ทีม 2 ชนะ!";
        else
            winnerText.text = "เสมอกัน!";
    }

    string FormatTime(float seconds)
    {
        int m = (int)(seconds / 60);
        int s = (int)(seconds % 60);
        int ms = (int)((seconds - Mathf.Floor(seconds)) * 100);
        return $"{m:00}:{s:00}.{ms:00}";
    }
}
