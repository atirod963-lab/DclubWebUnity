using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class JigsawGameManager : MonoBehaviourPunCallbacks
{
    public const string PROP_TEAM1_ROUND = "T1Round";
    public const string PROP_TEAM2_ROUND = "T2Round";
    public const string PROP_TEAM1_TIME = "T1Time";
    public const string PROP_TEAM2_TIME = "T2Time";
    public const string PROP_GAME_START_TS = "StartTS";
    public const int TOTAL_ROUNDS = 3;

    [Header("โหมดการเล่น")]
    public bool isSoloMode = false; // ติ๊กถูกถ้าด่านนี้เป็น Solo Mode

    [Header("Prefabs (ลาก Master Prefab มาใส่เรียงตามด่าน)")]
    // ⚠️ สำคัญ: Prefab ทุกอันต้องอยู่ในโฟลเดอร์ Resources เท่านั้น ถึงจะเสกผ่าน Photon ได้
    public GameObject[] soloPrefabs = new GameObject[3]; // ช่อง 0=ด่าน1, ช่อง 1=ด่าน2, ช่อง 2=ด่าน3
    public GameObject[] multiPrefabs = new GameObject[3];

    [Header("ข้อมูลทีม")]
    public int myTeam = 1;

    // =========================================================
    [Header("🔮 ตั้งค่ากระดาน & ภาพไกด์ลางๆ")]
    public float pieceSpacing = 1.2f;
    [Range(0f, 1f)]
    public float guideAlpha = 0.25f;
    public float guideScale = 0.55f;
    // =========================================================

    [Header("UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI countdownText;
    public GameObject conflictEffectPrefab;

    [Header("Board")]
    public Transform boardParent;
    public Transform pieceSpawnArea;

    private int currentRound = 1;
    private int piecesPlaced = 0;
    private int totalPieces = 0;
    private float elapsedTime = 0f;
    private bool isRunning = false;
    private bool isFinished = false;

    private int gameStartTimestamp = 0;

    private List<JigsawPiece> pieces = new List<JigsawPiece>();
    private List<GameObject> guidePieces = new List<GameObject>();

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            int startTS = PhotonNetwork.ServerTimestamp + 3000;
            var props = new Hashtable { { PROP_GAME_START_TS, startTS } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }

    void Update()
    {
        if (!isRunning || isFinished) return;

        elapsedTime += Time.deltaTime;
        UpdateTimerUI();
    }

    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        if (changedProps.ContainsKey(PROP_GAME_START_TS) && !isRunning)
        {
            gameStartTimestamp = (int)changedProps[PROP_GAME_START_TS];
            int msUntilStart = gameStartTimestamp - PhotonNetwork.ServerTimestamp;
            StartCoroutine(CountdownAndStart(Mathf.Max(0, msUntilStart / 1000f)));
        }
    }

    IEnumerator CountdownAndStart(float seconds)
    {
        float remaining = seconds;
        while (remaining > 0)
        {
            countdownText.text = Mathf.CeilToInt(remaining).ToString();
            remaining -= Time.deltaTime;
            yield return null;
        }
        countdownText.text = "GO!";
        yield return new WaitForSeconds(0.5f);
        countdownText.gameObject.SetActive(false);

        BeginRound(currentRound);
    }

    void BeginRound(int round)
    {
        currentRound = round;
        piecesPlaced = 0;
        pieces.Clear();

        roundText.text = $"ภาพที่ {round} / {TOTAL_ROUNDS}";

        CreateBoardGuide();

        if (PhotonNetwork.IsMasterClient) SpawnPieces();

        isRunning = true;
        SaveRoundToRoom(round);
    }

    // =========================================================
    // ระบบดึงข้อมูลตามโหมดและด่าน
    // =========================================================
    GameObject GetCurrentPrefab()
    {
        int idx = Mathf.Clamp(currentRound - 1, 0, 2);
        return isSoloMode ? soloPrefabs[idx] : multiPrefabs[idx];
    }

    int GetTotalPieces()
    {
        if (isSoloMode)
        {
            if (currentRound == 1) return 9;
            if (currentRound == 2) return 12;
            if (currentRound == 3) return 15;
        }
        else
        {
            if (currentRound == 1) return 3;
            if (currentRound == 2) return 6;
            if (currentRound == 3) return 9;
        }
        return 9;
    }

    int GetDecoyCount()
    {
        if (!isSoloMode) return 0; // Multi ไม่มีตัวหลอก
        if (currentRound == 1) return 2;
        if (currentRound == 2) return 4;
        if (currentRound == 3) return 6;
        return 0;
    }

    // =========================================================
    // สร้างภาพไกด์ลางๆ
    // =========================================================
    void CreateBoardGuide()
    {
        foreach (var g in guidePieces) { if (g != null) Destroy(g); }
        guidePieces.Clear();

        GameObject currentPrefab = GetCurrentPrefab();
        if (boardParent == null || currentPrefab == null) return;

        string sheetName = "Galactic pink Multi";
        var pieceScript = currentPrefab.GetComponent<JigsawPiece>();
        if (pieceScript != null && !string.IsNullOrEmpty(pieceScript.spriteSheetName))
        {
            sheetName = pieceScript.spriteSheetName;
        }

        Sprite[] allSlices = Resources.LoadAll<Sprite>(sheetName);
        int pieceCount = GetTotalPieces();
        if (allSlices == null || allSlices.Length < pieceCount) return;

        for (int i = 0; i < pieceCount; i++)
        {
            int col = i % 3; // คงที่ไว้ที่ 3 คอลัมน์ มันจะเรียงลงไปแถว 4, 5 เอง
            int row = i / 3;
            Vector2 targetPos = (Vector2)boardParent.position
                                + new Vector2(col * pieceSpacing - pieceSpacing, row * pieceSpacing - pieceSpacing);

            GameObject ghostObj = new GameObject($"GuideSlice_{i}");
            ghostObj.transform.position = targetPos;
            ghostObj.transform.SetParent(boardParent);
            ghostObj.transform.localScale = new Vector3(guideScale, guideScale, 1f);

            SpriteRenderer sr = ghostObj.AddComponent<SpriteRenderer>();
            sr.sprite = allSlices[i];
            sr.color = new Color(1f, 1f, 1f, guideAlpha);
            sr.sortingOrder = -10;

            guidePieces.Add(ghostObj);
        }
    }

    // =========================================================
    // เสกชิ้นส่วน (จริงและหลอก)
    // =========================================================
    void SpawnPieces()
    {
        GameObject currentPrefab = GetCurrentPrefab();
        totalPieces = GetTotalPieces();

        // 1. เสกชิ้นส่วนหลัก (ของจริง)
        for (int i = 0; i < totalPieces; i++)
        {
            int col = i % 3;
            int row = i / 3;
            Vector2 targetPos = (Vector2)boardParent.position
                                + new Vector2(col * pieceSpacing - pieceSpacing, row * pieceSpacing - pieceSpacing);

            Vector2 spawnPos = (Vector2)pieceSpawnArea.position
                                + new Vector2(Random.Range(-3f, 3f), Random.Range(-0.5f, 0.5f));

            GameObject obj = PhotonNetwork.Instantiate(currentPrefab.name, spawnPos, Quaternion.identity);

            JigsawPiece piece = obj.GetComponent<JigsawPiece>();
            piece.pieceIndex = i;
            piece.originalPosition = spawnPos;
            piece.targetPosition = targetPos;

            pieces.Add(piece);
        }

        // 2. เสกชิ้นส่วนหลอก (Decoys) เฉพาะถ้าเป็น Solo Mode
        int decoyCount = GetDecoyCount();
        for (int i = 0; i < decoyCount; i++)
        {
            GameObject decoyPrefab = GetRandomDecoyPrefab(currentPrefab);
            if (decoyPrefab == null) continue;

            Vector2 spawnPos = (Vector2)pieceSpawnArea.position
                                + new Vector2(Random.Range(-3f, 3f), Random.Range(-0.5f, 0.5f));

            // เสก Prefab รูปอื่นขึ้นมา
            GameObject obj = PhotonNetwork.Instantiate(decoyPrefab.name, spawnPos, Quaternion.identity);
            JigsawPiece piece = obj.GetComponent<JigsawPiece>();

            // สุ่มว่าจะเอาชิ้นไหนของรูปหลอกมาแสดง (สุ่มจาก 0 ถึง 8 เพื่อความปลอดภัย)
            piece.pieceIndex = Random.Range(0, 8);
            piece.originalPosition = spawnPos;
            // 🔥 ไฮไลท์: ตั้งเป้าหมายไว้ที่ๆ ไม่มีวันต่อถึง (9999) เพื่อให้มันต่อเข้ากรอบไม่ได้!
            piece.targetPosition = new Vector2(9999f, 9999f);

            pieces.Add(piece);
        }
    }

    // สุ่มหา Prefab อื่นที่ไม่ใช่รูปปัจจุบัน เอามาเป็นตัวหลอก
    GameObject GetRandomDecoyPrefab(GameObject excludePrefab)
    {
        List<GameObject> allPrefabs = new List<GameObject>();
        allPrefabs.AddRange(soloPrefabs);
        allPrefabs.AddRange(multiPrefabs);

        // ลบรูปที่กำลังต่ออยู่ออกไปจากกองสุ่ม
        allPrefabs.Remove(excludePrefab);
        allPrefabs.RemoveAll(item => item == null);

        if (allPrefabs.Count == 0) return null;
        return allPrefabs[Random.Range(0, allPrefabs.Count)];
    }

    public void OnPiecePlaced()
    {
        piecesPlaced++;
        // เช็คชนะโดยนับแค่จำนวนชิ้นจริง (totalPieces) พวกชิ้นหลอกจะไม่ถูกนับ
        if (piecesPlaced >= totalPieces) OnRoundComplete();
    }

    void OnRoundComplete()
    {
        if (currentRound >= TOTAL_ROUNDS) FinishGame();
        else
        {
            DestroyAllPieces();
            BeginRound(currentRound + 1);
        }
    }

    void FinishGame()
    {
        isRunning = false;
        isFinished = true;
        SaveTimeToRoom(elapsedTime);
        StartCoroutine(LoadSummaryScene());
    }

    IEnumerator LoadSummaryScene() { yield return new WaitForSeconds(2f); PhotonNetwork.LoadLevel("SummaryScene"); }
    void DestroyAllPieces() { if (!PhotonNetwork.IsMasterClient) return; foreach (var p in pieces) { if (p != null) PhotonNetwork.Destroy(p.gameObject); } pieces.Clear(); }
    void SaveRoundToRoom(int round) { string key = (myTeam == 1) ? PROP_TEAM1_ROUND : PROP_TEAM2_ROUND; var props = new Hashtable { { key, round } }; PhotonNetwork.CurrentRoom.SetCustomProperties(props); }
    void SaveTimeToRoom(float time) { string key = (myTeam == 1) ? PROP_TEAM1_TIME : PROP_TEAM2_TIME; var props = new Hashtable { { key, time } }; PhotonNetwork.CurrentRoom.SetCustomProperties(props); }
    void UpdateTimerUI() { int min = (int)(elapsedTime / 60); int sec = (int)(elapsedTime % 60); timerText.text = $"{min:00}:{sec:00}"; }
}