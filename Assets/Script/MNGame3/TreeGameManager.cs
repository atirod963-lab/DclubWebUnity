using UnityEngine;
using System.Collections;
using Photon.Pun;

public class TreeGameManager : MonoBehaviour
{
    public static TreeGameManager Instance;

    [Header("Player Visuals")]
    public SpriteRenderer playerSpriteRenderer;
    public Sprite idleSprite;
    public Sprite plantingSprite;

    [Header("Background / Ground Movement")]
    public Transform groundTransform;
    public float moveSpeed = 5f;
    public float moveDuration = 0.2f;

    [Header("Tree Settings")]
    public GameObject treePrefab;
    public Transform treeSpawnPoint;

    [Tooltip("ระยะห่างขั้นต่ำระหว่างต้นไม้แต่ละต้น")]
    public float minTreeDistance = 0.5f;
    [Tooltip("ช่วงราคาการสุ่มเยื้องตำแหน่ง")]
    public Vector2 randomOffsetRange = new Vector2(0.2f, 0.1f);

    private Vector3 lastTreeLocalPosition = new Vector3(999f, 999f, 999f);

    [Header("Game Settings")]
    public int targetScore = 100;

    [Header("Effect Prefab")]
    public GameObject floatingTextPrefab;
    public Canvas canvas;

    private int currentScore = 0;

    [Tooltip("เปิดให้เห็นใน Inspector เพื่อช่วยเช็คสถานะการเริ่มเกม")]
    public bool isGameActive = false;

    private Vector3 targetGroundPos;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        currentScore = 0;
        if (groundTransform != null) targetGroundPos = groundTransform.position;
        if (playerSpriteRenderer != null && idleSprite != null) playerSpriteRenderer.sprite = idleSprite;
    }

    void Update()
    {
        if (!isGameActive) return;

        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            AddScore();
        }

        if (groundTransform != null)
        {
            groundTransform.position = Vector3.Lerp(groundTransform.position, targetGroundPos, Time.deltaTime * moveSpeed);
        }
    }

    public void StartMiniGame()
    {
        isGameActive = true;
    }

    // 🔓 ปลดล็อคเป็น public แล้ว บอทเรียกใช้ได้!
    public void AddScore()
    {
        bool isHost = false;
        if (PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Role"))
        {
            string role = (string)PhotonNetwork.LocalPlayer.CustomProperties["Role"];
            if (role == "Spectator") isHost = true;
        }

        if (!isHost)
        {
            if (TouchManager2D.Instance != null)
            {
                TouchManager2D.Instance.score += 1;
                TouchManager2D.Instance.UpdateScoreUI();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScoreToMyTeam();
            }
        }

        StartCoroutine(PlantingAnimationRoutine());
        SpawnTree();

        if (groundTransform != null) targetGroundPos += new Vector3(-1.5f, 0f, 0f);

        SpawnFloatingText();
        SoundManager.Instance?.PlaySFX(SFXId.TreePlant);

        if (!isHost)
        {
            currentScore++;
            if (currentScore >= targetScore)
            {
                currentScore = targetScore;
                isGameActive = false;
            }
        }
    }

    void SpawnTree()
    {
        if (treePrefab == null || treeSpawnPoint == null || groundTransform == null) return;

        float randomX = Random.Range(-randomOffsetRange.x, randomOffsetRange.x);
        float randomY = Random.Range(-randomOffsetRange.y, randomOffsetRange.y);
        Vector3 worldSpawnPos = treeSpawnPoint.position + new Vector3(randomX, randomY, 0f);

        Vector3 localSpawnPos = groundTransform.InverseTransformPoint(worldSpawnPos);
        float distanceSinceLastTree = Vector3.Distance(localSpawnPos, lastTreeLocalPosition);

        if (lastTreeLocalPosition.x > 900f || distanceSinceLastTree >= minTreeDistance)
        {
            GameObject newTree = Instantiate(treePrefab, worldSpawnPos, Quaternion.identity);
            newTree.transform.SetParent(groundTransform);
            lastTreeLocalPosition = localSpawnPos;
            Destroy(newTree, 3f);
        }
    }

    IEnumerator PlantingAnimationRoutine()
    {
        if (playerSpriteRenderer != null && plantingSprite != null)
        {
            playerSpriteRenderer.sprite = plantingSprite;
            yield return new WaitForSeconds(moveDuration);
            playerSpriteRenderer.sprite = idleSprite;
        }
    }

    void SpawnFloatingText()
    {
        if (floatingTextPrefab != null)
        {
            // 1. รับตำแหน่งจากเมาส์หรือการทัช (Screen Space)
            Vector3 screenPosition = Input.mousePosition;
            if (Input.touchCount > 0) screenPosition = Input.GetTouch(0).position;

            // 2. แปลงพิกัดหน้าจอเป็นพิกัดในโลกของเกม (World Space)
            if (Camera.main != null)
            {
                // ใส่ค่า Z ให้ห่างจากกล้องนิดหน่อย เพื่อให้กล้องมองเห็น
                screenPosition.z = Mathf.Abs(Camera.main.transform.position.z);
                Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);

                // ล็อคค่า Z ให้อยู่ที่ 0 (หรือแกนเดียวกับเกม 2D ของคุณ)
                worldPosition.z = 0f;

                // 3. เสก Prefab ลงในตำแหน่งที่คำนวณได้ (ไม่ต้องใช้ Canvas แล้ว)
                GameObject textObj = Instantiate(floatingTextPrefab, worldPosition, Quaternion.identity);

                // (Optional) ถ้าตัว Prefab รูปภาพไม่มีสคริปต์ลบตัวเอง ให้เปิดคอมเมนต์บรรทัดล่างนี้เพื่อให้มันหายไปหลังผ่านไป 1-2 วินาที
                // Destroy(textObj, 1.5f); 
            }
        }
    }
}