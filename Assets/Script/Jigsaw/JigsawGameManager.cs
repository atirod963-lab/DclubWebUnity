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

    public const string PROP_TEAM1_PROGRESS = "T1Progress";
    public const string PROP_TEAM2_PROGRESS = "T2Progress";

    public const string PROP_GAME_START_TS = "StartTS";
    public const int TOTAL_ROUNDS = 3;

    [Header("โหมดการเล่น")]
    public bool isSoloMode = false;

    [Header("Prefabs (ลาก Master Prefab มาใส่เรียงตามด่าน)")]
    public GameObject[] soloPrefabs = new GameObject[3];
    public GameObject[] multiPrefabs = new GameObject[3];

    [Header("ข้อมูลทีม")]
    public int myTeam = 1;

    [Header("🔮 ตั้งค่ากระดาน & ภาพไกด์ลางๆ")]
    [Range(0f, 1f)]
    public float guideAlpha = 0.25f;
    public float guideScale = 0.55f;

    [Header("UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI countdownText;
    public GameObject conflictEffectPrefab;

    [Header("Board")]
    public Transform boardParent;
    public Transform pieceSpawnArea;

    // 🔥 [เพิ่มใหม่] ตั้งค่าระยะขอบกันล้นจอ
    [Header("📍 Spawn Settings")]
    [Tooltip("ระยะห่างจากขอบจอซ้าย-ขวา (เพิ่มค่านี้ถ้าชิ้นส่วนยังชิดขอบเกินไป)")]
    public float spawnPaddingX = 1.0f;
    [Tooltip("ระยะกระจายตัวแนวตั้ง (แกน Y)")]
    public float spawnSpreadY = 1.0f;

    [Header("Penalty Safety")]
    [Tooltip("กันไม่ให้ TriggerPenaltyReset ถูกยิงซ้ำถี่ๆ จากการเช็ค ValidateDrop ทุกเฟรมระหว่างลาก")]
    public float penaltyCooldownSeconds = 1.0f;

    private int currentRound = 1;
    private int piecesPlaced = 0;
    private int totalPieces = 0;
    private float elapsedTime = 0f;
    private bool isRunning = false;
    private bool isFinished = false;
    private bool isPenaltyOnCooldown = false;

    private int gameStartTimestamp = 0;

    private List<JigsawPiece> pieces = new List<JigsawPiece>();
    private List<GameObject> guidePieces = new List<GameObject>();


    private Sprite[] cachedSlices;
    private string cachedSheetName;

    public bool[] isPiecePlaced;

    void Start()
    {

        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Team"))
        {
            myTeam = (int)PhotonNetwork.LocalPlayer.CustomProperties["Team"];
        }

        if (isSoloMode)
        {
            StartCoroutine(CountdownAndStart(3f));
        }
        else
        {
            if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PROP_GAME_START_TS))
            {
                gameStartTimestamp = (int)PhotonNetwork.CurrentRoom.CustomProperties[PROP_GAME_START_TS];
                int msUntilStart = gameStartTimestamp - PhotonNetwork.ServerTimestamp;
                StartCoroutine(CountdownAndStart(Mathf.Max(0, msUntilStart / 1000f)));
            }
            else
            {
                int startTS = PhotonNetwork.ServerTimestamp + 3000;
                var props = new Hashtable { { PROP_GAME_START_TS, startTS } };
                if (PhotonNetwork.CurrentRoom != null)
                    PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            }
        }
    }

    void Update()
    {
        if (!isRunning || isFinished) return;

        elapsedTime += Time.deltaTime;
        UpdateTimerUI();
    }

    public void ShowConflictEffect(Vector3 position)
    {
        if (conflictEffectPrefab != null)
        {
            GameObject effect = Instantiate(conflictEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 1f);
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        if (changedProps.ContainsKey(PROP_GAME_START_TS) && !isRunning && !isSoloMode)
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

        totalPieces = GetTotalPieces();
        isPiecePlaced = new bool[totalPieces];

        // สไปรต์ชีทอาจเปลี่ยนไปตามด่าน ต้องโหลดใหม่และรีเซ็ตแคชทุกครั้งที่ขึ้นรอบใหม่
        cachedSlices = null;
        cachedSheetName = null;
        LoadSlicesForCurrentPrefab();

        roundText.text = $"ภาพที่ {round} / {TOTAL_ROUNDS}";

        CreateBoardGuide();
        UpdateProgressToRoom();

        SpawnPieces();

        isRunning = true;
        SaveRoundToRoom(round);
    }

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
        if (!isSoloMode) return 0;
        if (currentRound == 1) return 2;
        if (currentRound == 2) return 4;
        if (currentRound == 3) return 6;
        return 0;
    }

    // โหลดสไปรต์ชีทของ prefab ปัจจุบันเพียงครั้งเดียวต่อรอบ แล้ว cache เอาไว้ใช้ซ้ำ
    Sprite[] LoadSlicesForCurrentPrefab()
    {
        GameObject currentPrefab = GetCurrentPrefab();
        if (currentPrefab == null) return cachedSlices;

        string sheetName = "Galactic pink Multi";
        var pieceScript = currentPrefab.GetComponent<JigsawPiece>();
        if (pieceScript != null && !string.IsNullOrEmpty(pieceScript.spriteSheetName)) sheetName = pieceScript.spriteSheetName;

        if (cachedSlices != null && cachedSheetName == sheetName) return cachedSlices;

        cachedSlices = Resources.LoadAll<Sprite>(sheetName);
        cachedSheetName = sheetName;
        return cachedSlices;
    }

    public Vector2 CalculateTargetPosition(int index, int totalPieces, Sprite sliceSprite)
    {
        float pieceWidth = sliceSprite.bounds.size.x * guideScale;
        float pieceHeight = sliceSprite.bounds.size.y * guideScale;

        int cols = 3;
        // ใช้ CeilToInt กันไว้เผื่อ totalPieces ไม่ลงตัวพอดีกับ cols ในอนาคต
        int rows = Mathf.CeilToInt(totalPieces / (float)cols);

        int col = index % cols;
        int row = index / cols;

        float posX = (col - (cols - 1) / 2f) * pieceWidth;
        float posY = (row - (rows - 1) / 2f) * pieceHeight;

        return (Vector2)boardParent.position + new Vector2(posX, posY);
    }

    public int ValidateDrop(Vector2 dropPos, int pieceIndex, float snapDist, out Vector2 snappedPos)
    {
        snappedPos = dropPos;
        if (boardParent == null) return 0;

        int total = GetTotalPieces();
        Sprite[] allSlices = LoadSlicesForCurrentPrefab();
        if (allSlices == null || allSlices.Length == 0) return 0;

        for (int i = 0; i < total; i++)
        {
            Vector2 slotPos = CalculateTargetPosition(i, total, allSlices[0]);

            if (Vector2.Distance(dropPos, slotPos) <= snapDist)
            {
                snappedPos = slotPos;
                if (i == pieceIndex)
                {
                    // 🌟 เช็คว่าช่องนี้มีคนวางไปหรือยัง ถ้ามีคนวางแล้วให้ยิงคำสั่ง Penalty
                    if (isPiecePlaced != null && pieceIndex < isPiecePlaced.Length && isPiecePlaced[pieceIndex])
                    {
                        TriggerPenaltyReset();
                        return -1;
                    }
                    return 1;
                }
                return -1;
            }
        }
        return 0;
    }

    public void TriggerPenaltyReset()
    {
        if (isPenaltyOnCooldown) return;
        isPenaltyOnCooldown = true;
        StartCoroutine(ResetPenaltyCooldown());

        if (isSoloMode)
        {
            RPC_PenaltyReset(myTeam);
        }
        else
        {
            // 🌟 ยิงคำสั่งไปหาทุกคน พร้อมแนบหมายเลขทีมของเรา (myTeam) ไปด้วย
            photonView.RPC("RPC_PenaltyReset", RpcTarget.All, myTeam);
        }
    }

    IEnumerator ResetPenaltyCooldown()
    {
        yield return new WaitForSeconds(penaltyCooldownSeconds);
        isPenaltyOnCooldown = false;
    }

    [PunRPC]
    void RPC_PenaltyReset(int penalizedTeam)
    {
        // 🌟 ป้องกันไม่ให้ทีมอื่นโดนลูกหลง: ถ้ารหัสทีมที่โดนลงโทษ ไม่ใช่ทีมเรา ให้ข้ามคำสั่งนี้ไปเลย!
        if (!isSoloMode && myTeam != penalizedTeam) return;

        ShowConflictEffect(boardParent.position);

        // ทำลายชิ้นส่วนและเริ่มรอบเดิมใหม่เพื่อล้างค่ากระดานทั้งหมด
        DestroyAllPieces();
        BeginRound(currentRound);

        if (roundText != null)
        {
            StartCoroutine(ShowPenaltyText());
        }
    }

    IEnumerator ShowPenaltyText()
    {
        string originalText = $"ภาพที่ {currentRound} / {TOTAL_ROUNDS}";
        roundText.text = "<color=red>วางซ้ำ! โดนรีเซ็ตกระดาน!</color>";
        yield return new WaitForSeconds(3f);
        roundText.text = originalText;
    }

    void CreateBoardGuide()
    {
        if (boardParent != null)
        {
            foreach (Transform child in boardParent)
            {
                Destroy(child.gameObject);
            }
        }
        guidePieces.Clear();

        GameObject currentPrefab = GetCurrentPrefab();
        if (boardParent == null || currentPrefab == null) return;

        Sprite[] allSlices = LoadSlicesForCurrentPrefab();
        int pieceCount = GetTotalPieces();
        if (allSlices == null || allSlices.Length < pieceCount) return;

        for (int i = 0; i < pieceCount; i++)
        {
            Vector2 targetPos = CalculateTargetPosition(i, pieceCount, allSlices[0]);

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

    void SpawnPieces()
    {
        GameObject currentPrefab = GetCurrentPrefab();
        Sprite[] allSlices = LoadSlicesForCurrentPrefab();
        if (allSlices == null || allSlices.Length == 0) return;

        for (int i = 0; i < totalPieces; i++)
        {
            Vector2 targetPos = CalculateTargetPosition(i, totalPieces, allSlices[0]);

            // 🌟 [แก้ตรงนี้] ใช้ฟังก์ชันสุ่มพิกัดปลอดภัยแทนของเดิม
            Vector2 spawnPos = GetSafeSpawnPosition();

            GameObject obj = Instantiate(currentPrefab, spawnPos, Quaternion.identity);

            JigsawPiece piece = obj.GetComponent<JigsawPiece>();
            piece.pieceIndex = i;
            piece.originalPosition = spawnPos;
            piece.targetPosition = targetPos;

            pieces.Add(piece);
        }

        int decoyCount = GetDecoyCount();
        for (int i = 0; i < decoyCount; i++)
        {
            GameObject decoyPrefab = GetRandomDecoyPrefab(currentPrefab);
            if (decoyPrefab == null) continue;

            // 🌟 [แก้ตรงนี้ด้วย] ใช้ฟังก์ชันสุ่มพิกัดปลอดภัยสำหรับชิ้นหลอก
            Vector2 spawnPos = GetSafeSpawnPosition();

            GameObject obj = Instantiate(decoyPrefab, spawnPos, Quaternion.identity);
            JigsawPiece piece = obj.GetComponent<JigsawPiece>();

            var decoyScript = decoyPrefab.GetComponent<JigsawPiece>();
            int decoyIndexRange = totalPieces;
            if (decoyScript != null && !string.IsNullOrEmpty(decoyScript.spriteSheetName))
            {
                Sprite[] decoySlices = Resources.LoadAll<Sprite>(decoyScript.spriteSheetName);
                if (decoySlices != null && decoySlices.Length > 0) decoyIndexRange = decoySlices.Length;
            }

            piece.pieceIndex = Random.Range(0, decoyIndexRange);
            piece.originalPosition = spawnPos;
            piece.targetPosition = new Vector2(9999f, 9999f);

            pieces.Add(piece);
        }
    }

    // 🔥 [เพิ่มใหม่] ฟังก์ชันสุ่มตำแหน่งให้อยู่ในหน้าจอเสมอ
    Vector2 GetSafeSpawnPosition()
    {
        if (Camera.main == null) return pieceSpawnArea.position;

        // 1. วัดขอบหน้าจอซ้าย-ขวาจริงๆ ใน World Space
        float screenLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0)).x;
        float screenRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, 0)).x;

        // 2. บีบขอบเข้ามาตาม spawnPaddingX ที่ตั้งไว้
        float safeMinX = screenLeft + spawnPaddingX;
        float safeMaxX = screenRight - spawnPaddingX;

        // 3. สุ่มแกน X ในเซฟโซน และสุ่มแกน Y รอบๆ จุด pieceSpawnArea
        float randomX = Random.Range(safeMinX, safeMaxX);
        float randomY = pieceSpawnArea.position.y + Random.Range(-spawnSpreadY / 2f, spawnSpreadY / 2f);

        return new Vector2(randomX, randomY);
    }

    GameObject GetRandomDecoyPrefab(GameObject excludePrefab)
    {
        List<GameObject> allPrefabs = new List<GameObject>();
        allPrefabs.AddRange(soloPrefabs);
        allPrefabs.AddRange(multiPrefabs);

        allPrefabs.Remove(excludePrefab);
        allPrefabs.RemoveAll(item => item == null);

        if (allPrefabs.Count == 0) return null;
        return allPrefabs[Random.Range(0, allPrefabs.Count)];
    }

    public void OnPiecePlaced(int pieceIndex)
    {
        if (isSoloMode)
        {
            RPC_UpdateBoard(pieceIndex, myTeam);
        }
        else
        {
            photonView.RPC("RPC_UpdateBoard", RpcTarget.All, pieceIndex, myTeam);
        }
    }

    [PunRPC]
    void RPC_UpdateBoard(int pieceIndex, int teamWhoScored)
    {
        Debug.Log($"[Network] ได้รับคำสั่งอัปเดตกระดาน: ชิ้นที่ {pieceIndex} จากทีม {teamWhoScored} | ทีมของเราคือ {myTeam}");

        if (!isSoloMode && myTeam != teamWhoScored) return;

        if (isPiecePlaced[pieceIndex])
        {
            TriggerPenaltyReset();
            return;
        }

        isPiecePlaced[pieceIndex] = true;
        piecesPlaced++;

        // ทำให้ภาพไกด์บนกระดานชัดขึ้น เพื่อบอกให้รู้ว่าชิ้นนี้มีคนในทีมต่อเสร็จแล้วนะ!
        if (guidePieces.Count > pieceIndex && guidePieces[pieceIndex] != null)
        {
            guidePieces[pieceIndex].GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 1f);
            guidePieces[pieceIndex].GetComponent<SpriteRenderer>().sortingOrder = 5;
        }

        // ❌ [จุดที่แก้ไข] คอมเมนต์ หรือ ลบ โค้ดด้านล่างนี้ทิ้งไปเลยครับ!
        /* foreach (var p in pieces)
        {
            if (p != null && p.pieceIndex == pieceIndex)
            {
                Destroy(p.gameObject); // <--- ตัวการที่ทำให้จิ๊กซอว์หายไปจากจอเพื่อน!
            }
        }
        */

        UpdateProgressToRoom();

        if (piecesPlaced >= totalPieces)
        {
            OnRoundComplete();
        }
    }

    void UpdateProgressToRoom()
    {
        if (isSoloMode) return;
        if (PhotonNetwork.CurrentRoom == null) return;

        string keyProgress = (myTeam == 1) ? PROP_TEAM1_PROGRESS : PROP_TEAM2_PROGRESS;
        string progressText = $"{piecesPlaced}/{totalPieces}";

        string keyPieces = (myTeam == 1) ? "T1PiecesArray" : "T2PiecesArray";
        string placedPieces = "";

        if (isPiecePlaced != null)
        {

            for (int i = 0; i < isPiecePlaced.Length; i++)
            {
                if (isPiecePlaced[i]) placedPieces += i + ",";
            }
        }

        var props = new Hashtable {
            { keyProgress, progressText },
            { keyPieces, placedPieces }
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public void ResetPlacedCount()
    {
        piecesPlaced = 0;
        UpdateProgressToRoom();
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

        if (PhotonNetwork.CurrentRoom != null)
        {
            string key = (myTeam == 1) ? PROP_TEAM1_PROGRESS : PROP_TEAM2_PROGRESS;
            var props = new Hashtable { { key, "FINISH!" } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        SaveTimeToRoom(elapsedTime);
        StartCoroutine(LoadSummaryScene());
    }

    IEnumerator LoadSummaryScene() { yield return new WaitForSeconds(2f); PhotonNetwork.LoadLevel("SummaryScene"); }

    void DestroyAllPieces()
    {
        JigsawPiece[] allPiecesInScene = FindObjectsOfType<JigsawPiece>();
        foreach (JigsawPiece p in allPiecesInScene)
        {
            if (p != null) Destroy(p.gameObject);
        }
        pieces.Clear();
    }

    void SaveRoundToRoom(int round)
    {
        if (PhotonNetwork.CurrentRoom == null) return;
        string key = (myTeam == 1) ? PROP_TEAM1_ROUND : PROP_TEAM2_ROUND;
        var props = new Hashtable { { key, round } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    void SaveTimeToRoom(float time)
    {
        if (PhotonNetwork.CurrentRoom == null) return;
        string key = (myTeam == 1) ? PROP_TEAM1_TIME : PROP_TEAM2_TIME;
        var props = new Hashtable { { key, time } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    void UpdateTimerUI() { int min = (int)(elapsedTime / 60); int sec = (int)(elapsedTime % 60); timerText.text = $"{min:00}:{sec:00}"; }
}