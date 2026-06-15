using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameTimerManager : MonoBehaviour
{
    public static GameTimerManager Instance;

    [Header("Timer Settings")]
    [Tooltip("àÇÅÒ·Õè¨Ð¨Ñº (ÇÔ¹Ò·Õ)")]
    public float startingTime = 60f; // à»ÅÕèÂ¹ª×èÍà»ç¹àÇÅÒµÑé§µé¹
    private float timeRemaining;

    [Header("UI Settings")]
    [Tooltip("¾ÔÁ¾ìª×èÍ¢Í§ GameObject Text àÇÅÒãËéµÃ§à»êÐæ")]
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

    // ·Ó§Ò¹·Ø¡¤ÃÑé§·ÕèâËÅ´ Scene ãËÁè
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1. ¤é¹ËÒ UI µÑÇàÅ¢ºÍ¡àÇÅÒã¹ Scene ãËÁè
        GameObject uiObj = GameObject.Find(timerTextName);
        if (uiObj != null)
        {
            timerText = uiObj.GetComponent<TextMeshProUGUI>();
        }

        // 2. ÃÕà«çµàÇÅÒ¡ÅÑºä»·Õè 60 ÇÔ áÅÐÊÑè§ãËé¾ÃéÍÁ·Ó§Ò¹
        timeRemaining = startingTime;
        isTimerRunning = true;
        UpdateTimerUI();
    }

    void Update()
    {
        // ¨ÐäÁèËÑ¡ÅºàÇÅÒµÃÒºã´·Õè Time.timeScale ÂÑ§à»ç¹ 0 (µÍ¹·Õè 3 2 1 ¡ÓÅÑ§·Ó§Ò¹)
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
            Debug.Log("ËÁ´àÇÅÒ! ¡ÓÅÑ§à»ÅÕèÂ¹ä» Scene ·Õè: " + nextSceneIndex);
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.Log("ËÁ´àÇÅÒ! áÅÐäÁèÁÕ Scene ¶Ñ´ä»ãËéâËÅ´áÅéÇ");
        }
    }
}