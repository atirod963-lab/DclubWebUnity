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
    public float singlePlayerIntermissionTime = 5f;

    [Header("Intermission UI (หน้าจอสรุปคะแนน)")]
    public GameObject intermissionPanel;
    public TextMeshProUGUI intermissionGreenText;
    public TextMeshProUGUI intermissionRedText;

    [Header("Button Controls")]
    public GameObject nextButton;

    [Header("UI to Hide (ซ่อนตอนพักเบรก)")]
    [Tooltip("ลากป้ายคะแนนส่วนตัว (Score: x) หรือ UI ที่เกะกะมาใส่ตรงนี้ เพื่อให้มันซ่อนตอนขึ้นหน้าสรุปผล")]
    public GameObject[] uiToHideWhenFinished;

    private bool isIntermission = false;
    private bool timerIsRunning = false;
    private bool isSinglePlayer = false;

    void Awake()
    {
        if (intermissionPanel != null)
            intermissionPanel.SetActive(false);
    }

    void Start()
    {
        UpdateTimerDisplay(timeRemaining);
        if (intermissionPanel != null)
            intermissionPanel.SetActive(false);

        if (nextButton != null)
            nextButton.SetActive(false);

        isSinglePlayer = PlayerPrefs.GetInt("IsSoloGame", 0) == 1;
        if (isSinglePlayer)
        {
            intermissionTime = singlePlayerIntermissionTime;
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
        SoundManager.Instance?.PlaySFX(SFXId.GameOver);
        if (TouchManager2D.Instance != null)
            TouchManager2D.Instance.isGameActive = false;

        if (uiToHideWhenFinished != null)
        {
            foreach (GameObject ui in uiToHideWhenFinished)
            {
                if (ui != null) ui.SetActive(false);
            }
        }

        if (!isSinglePlayer && PhotonNetwork.InRoom)
        {
            if (intermissionPanel != null)
            {
                intermissionPanel.SetActive(true);

                int gScore = 0;
                int rScore = 0;

                if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("GlobalGreenScore"))
                    gScore = (int)PhotonNetwork.CurrentRoom.CustomProperties["GlobalGreenScore"];

                if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("GlobalRedScore"))
                    rScore = (int)PhotonNetwork.CurrentRoom.CustomProperties["GlobalRedScore"];

                if (intermissionGreenText != null) intermissionGreenText.text = "Score: " + gScore;
                if (intermissionRedText != null) intermissionRedText.text = "Score: " + rScore;
            }

            if (nextButton != null)
            {
                bool isHost = false;
                if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Role"))
                {
                    string role = (string)PhotonNetwork.LocalPlayer.CustomProperties["Role"];
                    isHost = (role == "Spectator");
                }
                if (PhotonNetwork.IsMasterClient) isHost = true;

                nextButton.SetActive(isHost);
            }
        }
        else
        {
            if (intermissionPanel != null)
                intermissionPanel.SetActive(false);

            if (nextButton != null)
                nextButton.SetActive(true);
        }

        isIntermission = true;
    }

    public void LoadNextSceneManual()
    {
        if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient) return;

        isIntermission = false;
        LoadNextScene();
    }

    void LoadNextScene()
    {
        if (intermissionPanel != null)
            intermissionPanel.SetActive(false);

        string sceneToLoad = string.IsNullOrEmpty(nextSceneName) ? "SummaryScene" : nextSceneName;
        Debug.Log("Loading scene: " + sceneToLoad);

        if (!isSinglePlayer && PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
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