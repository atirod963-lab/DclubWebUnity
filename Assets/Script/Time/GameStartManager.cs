using System.Collections;
using UnityEngine;
using TMPro;

public class GameStartManager : MonoBehaviour
{
    public static GameStartManager Instance;

    [Header("UI Settings")]
    public TextMeshProUGUI countdownText;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (countdownText == null)
            Debug.LogError("หา UI ไม่เจอ! อย่าลืมลากป้าย Text มาใส่ช่องด้วยนะครับ");

        if (TouchManager2D.Instance != null)
            TouchManager2D.Instance.isGameActive = false;

        Time.timeScale = 0f;
        StopAllCoroutines();
        StartCoroutine(StartCountdownRoutine());
    }

    IEnumerator StartCountdownRoutine()
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
        }

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

        if (TouchManager2D.Instance != null)
            TouchManager2D.Instance.isGameActive = true;

        if (GameManager.Instance != null)
            GameManager.Instance.StartMiniGame();

        GameTimer timer = FindObjectOfType<GameTimer>();
        if (timer != null)
            timer.StartTimer();
    }
}