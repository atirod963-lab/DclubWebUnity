using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // ทำเป็น Singleton เพื่อให้สคริปต์นับถอยหลังเรียกใช้ง่ายๆ

    [Header("UI References")]
    public Slider energySlider;

    [Header("Game Settings")]
    public int targetScore = 100;

    [Header("Effect Prefab")]
    public GameObject floatingTextPrefab;
    public Canvas canvas;

    private int currentScore = 0;

    // ❌ เปลี่ยนจาก true เป็น false เพื่อให้เริ่มซีนมา "ยังไม่เริ่มรับการคลิก"
    private bool isGameActive = false;

    void Awake()
    {
        // สร้าง Instance ของตัวเองเพื่อให้สคริปต์อื่นเรียกใช้ได้
        Instance = this;
    }

    void Start()
    {
        if (energySlider != null)
        {
            energySlider.maxValue = targetScore;
            energySlider.value = 0;
        }
        currentScore = 0;
    }

    void Update()
    {
        // ถ้า gameยังไม่เริ่ม (isGameActive == false) โค้ดจะหยุดตรงนี้ทันที จะคลิกยังไงก็ไม่มีอะไรเด้ง
        if (!isGameActive) return;

        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            AddScore();
        }
    }

    // ==========================================
    // [ฟังก์ชันเพิ่มใหม่] สำหรับให้สคริปต์นับถอยหลังมาเรียกใช้เมื่อนับเสร็จ
    // ==========================================
    public void StartMiniGame()
    {
        isGameActive = true;
    }

    void AddScore()
    {
        if (currentScore >= targetScore) return;

        currentScore++;

        if (energySlider != null)
        {
            energySlider.value = currentScore;
        }

        SpawnFloatingText();

        if (TouchManager2D.Instance != null)
        {
            TouchManager2D.Instance.score += 1;
            if (TouchManager2D.Instance.scoreText != null)
            {
                TouchManager2D.Instance.scoreText.text = "Score: " + TouchManager2D.Instance.score;
            }
        }

        if (currentScore >= targetScore)
        {
            currentScore = targetScore;
            isGameActive = false;
        }
    }

    void SpawnFloatingText()
    {
        if (floatingTextPrefab != null && canvas != null)
        {
            Vector3 inputPosition = Input.mousePosition;
            if (Input.touchCount > 0)
            {
                inputPosition = Input.GetTouch(0).position;
            }

            GameObject textObj = Instantiate(floatingTextPrefab, canvas.transform);
            textObj.transform.position = inputPosition;
        }
    }
}