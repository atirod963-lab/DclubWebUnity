using System.Collections;
using UnityEngine;
using TMPro;

public class GameStartManager : MonoBehaviour
{
    public static GameStartManager Instance;

    [Header("UI Settings")]
    public TextMeshProUGUI countdownText;

    [Header("Tutorial Settings (หน้าสอนเล่น)")]
    public GameObject tutorialPanel;
    public float tutorialDisplayTime = 5f;

    private bool countdownTickPlayed = false;
    private bool countdownStartPlayed = false;

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


        if (tutorialPanel != null)
        {

            if (countdownText != null) countdownText.gameObject.SetActive(false);

            tutorialPanel.SetActive(true);

            StartCoroutine(WaitAndHideTutorial());
        }
        else
        {

            StartCoroutine(StartCountdownRoutine());
        }
    }


    IEnumerator WaitAndHideTutorial()
    {
        yield return new WaitForSecondsRealtime(tutorialDisplayTime);

        if (tutorialPanel != null) tutorialPanel.SetActive(false);


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
            if (!countdownTickPlayed)
            {
                SoundManager.Instance?.PlaySFX(SFXId.CountdownTick);
                countdownTickPlayed = true;
            }
            yield return new WaitForSecondsRealtime(1f);
            count--;
        }

        if (countdownText != null) countdownText.text = "START!";
        if (!countdownStartPlayed)
        {
            SoundManager.Instance?.PlaySFX(SFXId.CountdownStart);
            countdownStartPlayed = true;
        }
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
