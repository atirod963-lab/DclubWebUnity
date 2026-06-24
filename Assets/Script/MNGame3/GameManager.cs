using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI References")]
    public Image bottleFullImage; // ลาก BottleFull Image เข้ามา

    [Header("Game Settings")]
    public int targetScore = 100;

    [Header("Effect Prefab")]
    public GameObject floatingTextPrefab;
    public Canvas canvas;

    private int currentScore = 0;
    private bool isGameActive = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        currentScore = 0;
        UpdateBottleFill(); // เซ็ตขวดให้เต็มตอนเริ่ม
    }

    void Update()
    {
        if (!isGameActive) return;

        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            AddScore();
        }
    }

    public void StartMiniGame()
    {
        isGameActive = true;
    }

    void AddScore()
    {
        if (TouchManager2D.Instance != null)
        {
            TouchManager2D.Instance.score += 1;
            TouchManager2D.Instance.UpdateScoreUI();
        }

        currentScore++;

        UpdateBottleFill(); // อัปเดตขวดทุกครั้งที่กด

        SpawnFloatingText();

        if (currentScore >= targetScore)
        {
            currentScore = targetScore;
            isGameActive = false;
        }
    }

    void UpdateBottleFill()
    {
        if (bottleFullImage != null)
        {
            // 1.0 = เต็ม, 0.0 = หมด
            bottleFullImage.fillAmount = 1f - ((float)currentScore / (float)targetScore);
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