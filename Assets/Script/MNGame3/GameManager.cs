using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    public Slider energySlider;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI scoreText;
    public GameObject summaryPanel;
    public TextMeshProUGUI finalScoreText;

    [Header("Game Settings")]
    public float timeLimit = 60f;
    public int targetScore = 100; // แต้มที่ต้องการเพื่อเติมหลอดให้เต็ม

    [Header("Effect Prefab")]
    public GameObject floatingTextPrefab;
    public Canvas canvas; // ต้องใส่ Canvas เพื่อให้ตัวเลขลอยขึ้นมาใน UI Space

    private int currentScore = 0;
    private float timeRemaining;
    private bool isGameActive = false;

    void Start()
    {
        StartGame();
    }

    void Update()
    {
        if (!isGameActive) return;

        // ระบบนับถอยหลัง
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            DisplayTime(timeRemaining);

            // ตรวจสอบการคลิกเมาส์ซ้าย
            if (Input.GetMouseButtonDown(0))
            {
                AddScore();
            }
        }
        else
        {
            EndGame();
        }
    }

    void StartGame()
    {
        currentScore = 0;
        timeRemaining = timeLimit;
        isGameActive = true;

        energySlider.maxValue = targetScore;
        energySlider.value = 0;
        summaryPanel.SetActive(false);
        UpdateScoreUI();
    }

    void AddScore()
    {
        currentScore++;
        UpdateScoreUI();

        // สร้าง Effect +1 ตรงตำแหน่งที่คลิกเมาส์
        SpawnFloatingText();

        // ถ้าหลอดพลังงานเต็มแล้ว (จะเลือกให้จบเกมทันที หรือให้กดต่อจนหมดเวลาก็ได้)
        if (currentScore >= targetScore)
        {
            currentScore = targetScore; // ล็อคไม่ให้เกินหลอด
            // EndGame(); // เปิดคอมเมนต์นี้ถ้าอยากให้หลอดเต็มแล้วจบเกมเลย
        }
    }

    void UpdateScoreUI()
    {
        scoreText.text = "Score: " + currentScore;
        energySlider.value = currentScore;
    }

    void DisplayTime(float timeToDisplay)
    {
        if (timeToDisplay < 0) timeToDisplay = 0;
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);
        float fraction = Mathf.FloorToInt((timeToDisplay * 100) % 100);

        // แสดงผลแบบ วินาที : เสี้ยววินาที (เช่น 59:99) เพื่อความตื่นเต้น
        timeText.text = string.Format("{0:00}:{1:00}", seconds, fraction);
    }

    void SpawnFloatingText()
    {
        if (floatingTextPrefab != null && canvas != null)
        {
            // สร้างตำแหน่งบน Canvas ตามพิกัดของเมาส์
            GameObject textObj = Instantiate(floatingTextPrefab, canvas.transform);
            textObj.transform.position = Input.mousePosition;
        }
    }

    void EndGame()
    {
        isGameActive = false;
        timeRemaining = 0;
        DisplayTime(0);

        // เปิดหน้าต่างสรุปผล
        summaryPanel.SetActive(true);
        finalScoreText.text = "คุณทำได้ทั้งหมด\n" + currentScore + " คะแนน!";
    }

    // ฟังก์ชันสำหรับผูกกับปุ่ม Replay ในหน้าต่างสรุปผล
    public void RestartGame()
    {
        StartGame();
    }
}