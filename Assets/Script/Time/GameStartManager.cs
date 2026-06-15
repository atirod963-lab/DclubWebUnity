using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameStartManager : MonoBehaviour
{
    public static GameStartManager Instance;

    [Header("UI Settings")]
    [Tooltip("¾ÔÁ¾ìª×èÍ¢Í§ GameObject Text ãËéµÃ§à»êÐæ à¾×èÍãËéÃÐºº¤é¹ËÒÍÑµâ¹ÁÑµÔ")]
    public string countdownTextName = "CountdownText";
    private TextMeshProUGUI countdownText; // äÁèµéÍ§ÅÒ¡ãÊè Inspector áÅéÇ

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

    // àÁ×èÍÊ¤ÃÔ»µì¹Õé¶Ù¡à»Ô´ãªé§Ò¹ ãËéÃÍ¿Ñ§ÊÑ­­Ò³¡ÒÃà»ÅÕèÂ¹ Scene
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // àÁ×èÍ»Ô´Ê¤ÃÔ»µì ãËéàÅÔ¡ÃÍÃÑºÊÑ­­Ò³ (»éÍ§¡Ñ¹ Error Memory Leak)
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ¿Ñ§¡ìªÑ¹¹Õé¨Ð·Ó§Ò¹ÍÑµâ¹ÁÑµÔ "·Ø¡¤ÃÑé§" ·ÕèâËÅ´ Scene ãËÁèàÊÃç¨
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1. ¤é¹ËÒ UI Text µÑÇãËÁèã¹ Scene »Ñ¨¨ØºÑ¹
        GameObject uiObj = GameObject.Find(countdownTextName);
        if (uiObj != null)
        {
            countdownText = uiObj.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            Debug.LogWarning("ËÒ UI ¹ÑºàÇÅÒäÁèà¨Í! ÍÂèÒÅ×ÁµÑé§ª×èÍÇÑµ¶ØÇèÒ: " + countdownTextName);
        }

        // 2. ËÂØ´àÇÅÒã¹à¡Á áÅÐàÃÔèÁ¹Ñº 3 2 1 ãËÁè
        Time.timeScale = 0f;
        StopAllCoroutines(); // ËÂØ´¡ÒÃ¹Ñº¶ÍÂËÅÑ§¢Í§à¡èÒ (¶éÒÁÕ¤éÒ§ÍÂÙè)
        StartCoroutine(StartCountdownRoutine());
    }

    IEnumerator StartCountdownRoutine()
    {
        if (countdownText != null) countdownText.gameObject.SetActive(true);

        int count = 3;
        while (count > 0)
        {
            if (countdownText != null) countdownText.text = count.ToString();
            yield return new WaitForSecondsRealtime(1f);
            count--;
        }

        if (countdownText != null) countdownText.text = "START!";
        yield return new WaitForSecondsRealtime(1f);

        if (countdownText != null) countdownText.gameObject.SetActive(false);

        Time.timeScale = 1f;
        
        
        

        //กูเพิ่มโค้ดไปตรงนี้ดิดหน่อยนะเพื่อน
        // ==========================================
        // ---> เพิ่มโค้ด 4 บรรทัดนี้ต่อท้ายลงไป <---
        // สั่งให้มินิเกมของเราเริ่มรับค่าการคลิกหน้าจอ
        // ==========================================
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartMiniGame();
        }
    }
}