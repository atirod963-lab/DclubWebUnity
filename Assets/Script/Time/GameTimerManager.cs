using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameTimerManager : MonoBehaviour
{
    public static GameTimerManager Instance;

    [Header("Timer Settings")]
    [Tooltip("เวลาที่จะจับ (วินาที)")]
    public float startingTime = 60f; // เปลี่ยนชื่อเป็นเวลาตั้งต้น
    private float timeRemaining;

    [Header("UI Settings")]
    [Tooltip("พิมพ์ชื่อของ GameObject Text เวลาให้ตรงเป๊ะๆ")]
    public string timerTextName = "TimerText";
    private TextMeshProUGUI timerText;

    private bool isTimerRunning = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ทำงานทุกครั้งที่โหลด Scene ใหม่
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1. ค้นหา UI ตัวเลขบอกเวลาใน Scene ใหม่
        GameObject uiObj = GameObject.Find(timerTextName);
        if (uiObj != null)
        {
            timerText = uiObj.GetComponent<TextMeshProUGUI>();
        }

        // 2. รีเซ็ตเวลากลับไปที่ 60 วิ และสั่งให้พร้อมทำงาน
        timeRemaining = startingTime;
        isTimerRunning = true;
        UpdateTimerUI();
    }

    void Update()
    {
        // จะไม่หักลบเวลาตราบใดที่ Time.timeScale ยังเป็น 0 (ตอนที่ 3 2 1 กำลังทำงาน)
        if (isTimerRunning && timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerUI();

            if (timeRemaining <= 0)
            {
                timeRemaining = 0;
                isTimerRunning = false;
                UpdateTimerUI();
                LoadNextScene();
            }
        }
    }

    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            timerText.text = "Time: " + Mathf.CeilToInt(timeRemaining).ToString();
        }
    }

    void LoadNextScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int totalScenes = SceneManager.sceneCountInBuildSettings;
        int nextSceneIndex = currentSceneIndex + 1;

        if (nextSceneIndex < totalScenes)
        {
            Debug.Log("หมดเวลา! กำลังเปลี่ยนไป Scene ที่: " + nextSceneIndex);
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.Log("หมดเวลา! และไม่มี Scene ถัดไปให้โหลดแล้ว");
        }
    }
}