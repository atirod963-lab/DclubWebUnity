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
    private TextMeshProUGUI countdownText; // ไม่ต้องลากใส่ Inspector แล้ว

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

    // เมื่อสคริปต์นี้ถูกเปิดใช้งาน ให้รอฟังสัญญาณการเปลี่ยน Scene
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // เมื่อปิดสคริปต์ ให้เลิกรอรับสัญญาณ (ป้องกัน Error Memory Leak)
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ฟังก์ชันนี้จะทำงานอัตโนมัติ "ทุกครั้ง" ที่โหลด Scene ใหม่เสร็จ
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1. ค้นหา UI Text ตัวใหม่ใน Scene ปัจจุบัน
        GameObject uiObj = GameObject.Find(countdownTextName);
        if (uiObj != null)
        {
            countdownText = uiObj.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            Debug.LogWarning("หา UI นับเวลาไม่เจอ! อย่าลืมตั้งชื่อวัตถุว่า: " + countdownTextName);
        }

        // 2. หยุดเวลาในเกม และเริ่มนับ 3 2 1 ใหม่
        Time.timeScale = 0f;
        StopAllCoroutines(); // หยุดการนับถอยหลังของเก่า (ถ้ามีค้างอยู่)
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
    }
}