using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameStartManager : MonoBehaviour
{
    public static GameStartManager Instance;

    [Header("UI Settings")]
    [Tooltip("พิมพ์ชื่อของ GameObject Text ให้ตรงเป๊ะๆ เพื่อให้ระบบค้นหาอัตโนมัติ")]
    public string countdownTextName = "CountdownText";
    private TextMeshProUGUI countdownText;

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

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameObject uiObj = GameObject.Find(countdownTextName);
        if (uiObj != null)
        {
            countdownText = uiObj.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            Debug.LogWarning("หา UI นับเวลาไม่เจอ! อย่าลืมตั้งชื่อวัตถุว่า: " + countdownTextName);
        }

        // สั่งปิดการแตะหน้าจอทันทีที่เข้า Scene ใหม่
        if (TouchManager2D.Instance != null)
        {
            TouchManager2D.Instance.isGameActive = false;
        }

        Time.timeScale = 0f;
        StopAllCoroutines();
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

        // เมื่อนับถอยหลังเสร็จ สั่งเปิดให้ผู้เล่นแตะหน้าจอได้!
        if (TouchManager2D.Instance != null)
        {
            TouchManager2D.Instance.isGameActive = true;
        }

        // สั่งให้มินิเกมของเราเริ่มรับค่าการคลิกหน้าจอ
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartMiniGame();
        }
    }
}