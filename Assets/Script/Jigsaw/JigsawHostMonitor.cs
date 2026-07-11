using UnityEngine;
using UnityEngine.UI; // 🌟 เพิ่มบรรทัดนี้เพื่อจัดการรูปภาพ
using TMPro;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class JigsawHostMonitor : MonoBehaviourPunCallbacks
{
    [Header("📊 UI ฝั่งทีม 1 (สีเขียว)")]
    public TextMeshProUGUI t1RoundText;
    public TextMeshProUGUI t1ProgressText;
    public Image t1PuzzleImage; // 🌟 ช่องใส่รูปของทีม 1

    [Header("📊 UI ฝั่งทีม 2 (สีแดง)")]
    public TextMeshProUGUI t2RoundText;
    public TextMeshProUGUI t2ProgressText;
    public Image t2PuzzleImage; // 🌟 ช่องใส่รูปของทีม 2

    [Header("🖼️ รูปภาพจิ๊กซอว์ทั้ง 3 ด่าน")]
    public Sprite[] puzzleSprites; // 🌟 ลากรูปภาพทั้ง 3 รูปมาใส่ตรงนี้ใน Inspector

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        // === อัปเดตฝั่งทีม 1 ===
        if (propertiesThatChanged.ContainsKey("T1Round"))
        {
            int round = (int)propertiesThatChanged["T1Round"];
            t1RoundText.text = $"ภาพที่: {round} / 3";
            // เปลี่ยนรูปภาพตามรอบ (ถ้า round = 1 ให้ใช้ index 0)
            if (t1PuzzleImage != null && round <= puzzleSprites.Length)
                t1PuzzleImage.sprite = puzzleSprites[round - 1];
        }
        if (propertiesThatChanged.ContainsKey("T1Progress"))
        {
            string p = propertiesThatChanged["T1Progress"].ToString();
            t1ProgressText.text = p;
            if (p == "FINISH!") GoToSummaryScene();
        }

        // === อัปเดตฝั่งทีม 2 ===
        if (propertiesThatChanged.ContainsKey("T2Round"))
        {
            int round = (int)propertiesThatChanged["T2Round"];
            t2RoundText.text = $"ภาพที่: {round} / 3";
            if (t2PuzzleImage != null && round <= puzzleSprites.Length)
                t2PuzzleImage.sprite = puzzleSprites[round - 1];
        }
        if (propertiesThatChanged.ContainsKey("T2Progress"))
        {
            string p = propertiesThatChanged["T2Progress"].ToString();
            t2ProgressText.text = p;
            if (p == "FINISH!") GoToSummaryScene();
        }
    }

    void GoToSummaryScene() => PhotonNetwork.LoadLevel("SummaryScene");
}