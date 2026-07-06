using UnityEngine;
using TMPro;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class TouchManager2D : MonoBehaviour
{
    public static TouchManager2D Instance;

    [Header("Score Data")]
    public int score = 0;

    [Header("UI Search Settings")]
    public string scoreTextName = "ScoreText";
    public string timerTextName = "TimerText";

    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI timerText;

    public bool isGameActive = false;

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
        FindUIElements();
        UpdateScoreUI();
    }

    void FindUIElements()
    {
        GameObject sObj = GameObject.Find(scoreTextName);
        GameObject tObj = GameObject.Find(timerTextName);

        if (sObj != null)
        {
            if (scoreText != null && scoreText.gameObject != sObj)
            {
                Destroy(scoreText.gameObject);
            }
            scoreText = sObj.GetComponent<TextMeshProUGUI>();
        }

        if (tObj != null)
        {
            if (timerText != null && timerText.gameObject != tObj)
            {
                Destroy(timerText.gameObject);
            }
            timerText = tObj.GetComponent<TextMeshProUGUI>();
        }
    }

    void Start()
    {
        FindUIElements();
        UpdateScoreUI();
    }

    void Update()
    {
        if (!isGameActive) return;

        // 🛑 [ดักโฮสต์] ถ้าเป็น Spectator ให้เด้งออกจากระบบสแกนนิ้วไปเลย! ยืนดูได้อย่างเดียว
        if (PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Role"))
        {
            if ((string)PhotonNetwork.LocalPlayer.CustomProperties["Role"] == "Spectator") return;
        }

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began) CheckTouch2D(touch.position);
        }
        else if (Input.GetMouseButtonDown(0))
        {
            CheckTouch2D(Input.mousePosition);
        }
    }

    void CheckTouch2D(Vector3 screenPosition)
    {
        if (Camera.main == null) return;

        float distanceToCamera = Mathf.Abs(Camera.main.transform.position.z);
        screenPosition.z = distanceToCamera;

        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
        Vector2 touchPosition2D = new Vector2(worldPosition.x, worldPosition.y);

        Collider2D hitCollider = Physics2D.OverlapPoint(touchPosition2D);

        if (hitCollider != null)
        {
            if (hitCollider.CompareTag("Healthy Food") || hitCollider.CompareTag("Hoop") || hitCollider.CompareTag("Water"))
            {
                score += 1;
                UpdateScoreUI();
                if (GameManager.Instance != null) GameManager.Instance.AddScoreToMyTeam();

                if (hitCollider.CompareTag("Hoop"))
                {
                    SoundManager.Instance?.PlaySFX(SFXId.HoopShoot);
                    HoopController hoop = hitCollider.GetComponent<HoopController>();
                    if (hoop != null)
                    {
                        if (hoop.hitEffectPrefab != null)
                        {
                            Instantiate(hoop.hitEffectPrefab, hitCollider.transform.position, Quaternion.identity);
                        }
                        hoop.MoveToRandomPosition();
                    }
                }
                else
                {
                    SoundManager.Instance?.PlaySFX(SFXId.CorrectTap);
                    Destroy(hitCollider.gameObject);
                }
            }
            else if (hitCollider.CompareTag("Junk Food"))
            {
                score -= 1;
                UpdateScoreUI();
                if (GameManager.Instance != null) GameManager.Instance.SubtractScoreToMyTeam();

                SoundManager.Instance?.PlaySFX(SFXId.WrongTap);
                Destroy(hitCollider.gameObject);
            }
        }
    }

    public void UpdateScoreUI()
    {
        if (scoreText == null || !scoreText.gameObject.activeInHierarchy)
        {
            GameObject sObj = GameObject.Find(scoreTextName);
            if (sObj != null) scoreText = sObj.GetComponent<TextMeshProUGUI>();
        }

        if (scoreText != null)
            scoreText.text = "Score: " + score;

        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            ExitGames.Client.Photon.Hashtable hash = new ExitGames.Client.Photon.Hashtable();
            hash.Add("Score", score);
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        }
    }
}