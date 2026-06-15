using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI References")]
    public Slider energySlider;

    [Header("Game Settings")]
    public int targetScore = 100;

    [Header("Effect Prefab")]
    public GameObject floatingTextPrefab;
    public Canvas canvas;

    private int currentScore = 0;

    private bool isGameActive = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (energySlider != null)
        {
            energySlider.maxValue = targetScore;
            energySlider.value = 0;
        }
        currentScore = 0;
    }

    void Update()
    {
        if (!isGameActive) return;

        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            AddScore();
        }
    }
    public void StartMiniGame()
    {
        isGameActive = true;
    }

    void AddScore()
    {

        if (TouchManager2D.Instance != null)
        {
            TouchManager2D.Instance.score += 1;
            TouchManager2D.Instance.UpdateScoreUI();
        }

        currentScore++;
        if (energySlider != null)
        {
            energySlider.value = currentScore;
        }

        SpawnFloatingText();

        if (currentScore >= targetScore)
        {
            currentScore = targetScore;
            isGameActive = false;
        }
    }

    void SpawnFloatingText()
    {
        if (floatingTextPrefab != null && canvas != null)
        {
            Vector3 inputPosition = Input.mousePosition;
            if (Input.touchCount > 0)
            {
                inputPosition = Input.GetTouch(0).position;
            }

            GameObject textObj = Instantiate(floatingTextPrefab, canvas.transform);
            textObj.transform.position = inputPosition;
        }
    }
}