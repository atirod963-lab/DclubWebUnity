using UnityEngine;
using TMPro;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class GameTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    public float timeRemaining = 30f;
    public TextMeshProUGUI timerText;

    [Header("Scene Transition")]
    public string nextSceneName;

    private bool timerIsRunning = false;

    void Start()
    {
        UpdateTimerDisplay(timeRemaining);
    }

    public void StartTimer()
    {
        timerIsRunning = true;
    }

    void Update()
    {
        if (!timerIsRunning) return;

        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerDisplay(timeRemaining);
        }
        else
        {
            timeRemaining = 0;
            timerIsRunning = false;
            UpdateTimerDisplay(0);
            OnTimerFinished();
        }
    }

    void UpdateTimerDisplay(float timeToDisplay)
    {
        float seconds = Mathf.CeilToInt(timeToDisplay);
        if (timerText != null)
            timerText.text = "Time: " + seconds;
    }

    void OnTimerFinished()
    {
        if (TouchManager2D.Instance != null)
            TouchManager2D.Instance.isGameActive = false;

        string sceneToLoad = string.IsNullOrEmpty(nextSceneName) ? "SummaryScene" : nextSceneName;

        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
                PhotonNetwork.LoadLevel(sceneToLoad);
        }
        else
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}