using UnityEngine;
using TMPro;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class GameTimer : MonoBehaviour
{
    [Header("Timer Settings (เวลาเล่นเกม)")]
    public float timeRemaining = 30f;
    public TextMeshProUGUI timerText;

    [Header("Scene Transition")]
    public string nextSceneName;

    [Header("Intermission Settings (เวลาพักเบรก)")]
    public float intermissionTime = 30f;

    [Header("Staff Control (ปุ่มเปลี่ยนด่าน)")]
    public GameObject nextStageButton;

    private bool isIntermission = false;
    private bool timerIsRunning = false;

    void Start()
    {
        UpdateTimerDisplay(timeRemaining);

        if (nextStageButton != null)
        {
            nextStageButton.SetActive(false);
        }
    }

    public void StartTimer()
    {
        timerIsRunning = true;
    }

    void Update()
    {
        if (timerIsRunning)
        {
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
        else if (isIntermission)
        {
            if (intermissionTime > 0)
            {
                intermissionTime -= Time.deltaTime;
                UpdateIntermissionDisplay(intermissionTime);
            }
            else
            {
                intermissionTime = 0;
                isIntermission = false;
                LoadNextScene();
            }
        }
    }

    void UpdateTimerDisplay(float timeToDisplay)
    {
        float seconds = Mathf.CeilToInt(timeToDisplay);
        if (timerText != null)
            timerText.text = "Time   " + seconds;
    }

    void UpdateIntermissionDisplay(float timeToDisplay)
    {
        float seconds = Mathf.CeilToInt(timeToDisplay);
        if (timerText != null)
            timerText.text = "Next Game In   " + seconds;
    }

    void OnTimerFinished()
    {
        Debug.Log("Game Finished - Starting Intermission Countdown.");

        SoundManager.Instance?.PlaySFX(SFXId.GameOver);
        if (TouchManager2D.Instance != null)
            TouchManager2D.Instance.isGameActive = false;

        isIntermission = true;

        if (PhotonNetwork.IsMasterClient && nextStageButton != null)
        {
            nextStageButton.SetActive(true);
        }
    }

    public void LoadNextSceneManual()
    {
        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient) return;

        isIntermission = false;
        LoadNextScene();
    }

    void LoadNextScene()
    {
        string sceneToLoad = string.IsNullOrEmpty(nextSceneName) ? "SummaryScene" : nextSceneName;
        Debug.Log("Loading scene: " + sceneToLoad);

        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel(sceneToLoad);
            }
        }
        else
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}