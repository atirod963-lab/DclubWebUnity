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

    [Header("Prefabs")]
    public GameObject jigsawPiecePrefab;

    [Header("ข้อมูลทีม")]
    public int myTeam = 1;

    // =========================================================
    [Header("🔮 ตั้งค่ากระดาน & ภาพไกด์ลางๆ")]
    public float pieceSpacing = 1.2f; // ดึงออกมาเป็นตัวแปรกลาง! จะได้ใช้ร่วมกันทั้งตอนเสกจริงและเสกไกด์
    [Range(0f, 1f)]
    public float guideAlpha = 0.25f;  // ความจางของภาพไกด์ (0.25 = จาง 25%)
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
    private List<GameObject> guidePieces = new List<GameObject>(); // เก็บออบเจกต์ภาพไกด์ไว้ทำลายตอนจบด่าน

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

        // 🔮 สั่งเสกภาพไกด์ลางๆ ลงบนกระดานทันที! (สั่งรันทั้งเครื่อง Master และ Client จะได้เห็นเหมือนกัน)
        CreateBoardGuide();

        if (PhotonNetwork.IsMasterClient) SpawnPieces();

        isRunning = true;
        SaveRoundToRoom(round);

    }

    // =========================================================
    // ฟังก์ชันประกอบร่างภาพไกด์ลางๆ
    // =========================================================
    void CreateBoardGuide()
    {
        // 1. ทำลายภาพไกด์ของด่านที่แล้วทิ้งให้หมดก่อน
        foreach (var g in guidePieces) { if (g != null) Destroy(g); }
        guidePieces.Clear();

        if (boardParent == null || jigsawPiecePrefab == null) return;

        // 2. ไปแอบดูชื่อสไปรต์ชีตจาก JigsawPiece ที่อยู่ใน Prefab
        string sheetName = "Galactic pink Multi";
        var pieceScript = jigsawPiecePrefab.GetComponent<JigsawPiece>();
        if (pieceScript != null && !string.IsNullOrEmpty(pieceScript.spriteSheetName))
        {
            sheetName = pieceScript.spriteSheetName;
        }

        // 3. โหลดสไปรต์ย่อย 9 ชิ้น
        Sprite[] allSlices = Resources.LoadAll<Sprite>(sheetName);
        if (allSlices == null || allSlices.Length < 9) return;

        // 4. ประกอบร่างตาราง 3x3 ทับตำแหน่งเป้าหมาย
        // 4. ประกอบร่างตาราง 3x3 ทับตำแหน่งเป้าหมาย
        for (int i = 0; i < 9; i++)
        {
            int col = i % 3;
            int row = i / 3;
            Vector2 targetPos = (Vector2)boardParent.position
                                + new Vector2(col * pieceSpacing - pieceSpacing, row * pieceSpacing - pieceSpacing);

            GameObject ghostObj = new GameObject($"GuideSlice_{i}");
            ghostObj.transform.position = targetPos;
            ghostObj.transform.SetParent(boardParent);

            // 🔥 [เพิ่มบรรทัดนี้ลงไป!!] บังคับย่อสเกลลงมาเหลือ 0.55 ตามที่สั่งเป๊ะ
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
        totalPieces = 9;
        // เปลี่ยนจากที่เคยเขียน float spacing = 1.2f; ตายตัวในนี้ ไปใช้ pieceSpacing ของคลาสแทน!
        for (int i = 0; i < totalPieces; i++)
        {
            int col = i % 3;
            int row = i / 3;
            Vector2 targetPos = (Vector2)boardParent.position
                                + new Vector2(col * pieceSpacing - pieceSpacing, row * pieceSpacing - pieceSpacing);

            Vector2 spawnPos = (Vector2)pieceSpawnArea.position
                                + new Vector2(Random.Range(-3f, 3f), Random.Range(-0.5f, 0.5f));

            GameObject obj = PhotonNetwork.Instantiate(jigsawPiecePrefab.name, spawnPos, Quaternion.identity);

            JigsawPiece piece = obj.GetComponent<JigsawPiece>();
            piece.pieceIndex = i;
            piece.originalPosition = spawnPos;
            piece.targetPosition = targetPos;

            pieces.Add(piece);
        }
    }

    public void OnPiecePlaced()
    {
        piecesPlaced++;
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