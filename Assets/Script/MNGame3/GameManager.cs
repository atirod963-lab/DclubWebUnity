using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI References")]
    public Image bottleFullImage;

    [Header("Bottle Shake")]
    public RectTransform bottleRect; // ลาก Image ของขวดมาใส่

    [Header("Game Settings")]
    public int targetScore = 100;

    [Header("Effect Prefab")]
    public GameObject floatingTextPrefab;
    public Canvas canvas;

    private int currentScore = 0;
    private bool isGameActive = false;

    // ค่าความแรงในการเขย่า
    private float shakePower = 0f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        currentScore = 0;
        UpdateBottleFill();
    }

    void Update()
    {
        if (!isGameActive)
            return;

        if (Input.GetMouseButtonDown(0) ||
            (Input.touchCount > 0 &&
             Input.GetTouch(0).phase == TouchPhase.Began))
        {
            AddScore();
        }

        UpdateBottleShake();
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

        // เพิ่มแรงหมุนทุกครั้งที่กด
        shakePower += 6f;
        shakePower = Mathf.Clamp(shakePower, 0f, 25f);

        UpdateBottleFill();
        SpawnFloatingText();

        if (currentScore >= targetScore)
        {
            currentScore = targetScore;
            isGameActive = false;
        }
    }

    void UpdateBottleFill()
    {
        if (bottleFullImage != null)
        {
            // 1.0 = เต็ม, 0.0 = หมด
            bottleFullImage.fillAmount =
                1f - ((float)currentScore / targetScore);
        }
    }

    void UpdateBottleShake()
    {
        if (bottleRect == null)
            return;

        // ค่อย ๆ ลดแรงหมุนเมื่อหยุดกด
        shakePower = Mathf.Lerp(
            shakePower,
            0f,
            Time.deltaTime * 3f);

        // หมุนซ้าย-ขวา
        float angle =
            Mathf.Sin(Time.time * 18f) *
            shakePower;

        bottleRect.localRotation =
            Quaternion.Euler(0f, 0f, angle);
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

            GameObject textObj =
                Instantiate(floatingTextPrefab, canvas.transform);

            textObj.transform.position = inputPosition;
        }
    }
}