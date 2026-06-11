using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimingGameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public float targetTime = 10.0f; // เวลาเริ่มต้น
    private float currentTime;
    private bool isPlaying = false;

    [Header("UI Elements")]
    public TextMeshProUGUI timerText; // แสดงเวลาบนจอ (ใส่หรือไม่ใส่ก็ได้)
    public GameObject summaryPanel;   // หน้าต่างสรุปผล
    public TextMeshProUGUI resultText;// ข้อความสรุปผล
    public Button resetButton;        // ปุ่มรีเซ็ต

    void Start()
    {
        // ผูกฟังก์ชัน ResetGame เข้ากับปุ่มเมื่อเริ่มต้น
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetGame);
        }

        // เริ่มเกมทันที
        ResetGame();
    }

    void Update()
    {
        // ถ้าเกมไม่ได้เล่นอยู่ ให้ข้าม Update ไปเลย
        if (!isPlaying) return;

        // นับเวลาถอยหลัง
        currentTime -= Time.deltaTime;

        // อัปเดตข้อความเวลาบนหน้าจอ (โชว์ทศนิยม 2 ตำแหน่ง)
        if (timerText != null)
        {
            timerText.text = currentTime.ToString("F2");
        }

        // ตรวจสอบการแตะหน้าจอ (รองรับทั้งคลิกเมาส์ใน Editor และการแตะบนมือถือ)
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            EndGame();
        }
    }

    void EndGame()
    {
        isPlaying = false;

        // เปิดหน้าต่างสรุปผล
        summaryPanel.SetActive(true);

        // ตรวจสอบค่าความคลาดเคลื่อน
        // currentTime > 0 แปลว่ากดก่อนเวลาหมด (เร็วไป)
        // currentTime < 0 แปลว่ากดหลังเวลาหมด (ช้าไป)

        string sign = currentTime > 0 ? "+" : "";

        // แสดงผลลัพธ์ ทศนิยม 2 ตำแหน่ง
        resultText.text = $"คุณแตะคลาดเคลื่อนไป\n{sign}{currentTime.ToString("F2")} วินาที";
    }

    public void ResetGame()
    {
        // รีเซ็ตเวลาและสถานะกลับไปตอนเริ่มต้น
        currentTime = targetTime;
        summaryPanel.SetActive(false);
        isPlaying = true;
    }
}