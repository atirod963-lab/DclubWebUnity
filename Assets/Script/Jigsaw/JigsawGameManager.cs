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
    // -------------------------------------------------------
    //  CONSTANTS: Custom Property Keys
    // -------------------------------------------------------
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

    [Header("UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI countdownText;
    public GameObject conflictEffectPrefab;

    [Header("Board")]
    public Transform boardParent;
    public Transform pieceSpawnArea;

    // -------------------------------------------------------
    //  STATE
    // -------------------------------------------------------
    private int currentRound = 1;
    private int piecesPlaced = 0;
    private int totalPieces = 0;
    private float elapsedTime = 0f;
    private bool isRunning = false;
    private bool isFinished = false;

    private int gameStartTimestamp = 0;

    private List<Vector2> targetSlots = new List<Vector2>();
    private List<Vector2> spawnPoints = new List<Vector2>();
    private List<JigsawPiece> pieces = new List<JigsawPiece>();

    // -------------------------------------------------------
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

    // -------------------------------------------------------
    //  PHOTON CALLBACKS
    // -------------------------------------------------------
    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        if (changedProps.ContainsKey(PROP_GAME_START_TS) && !isRunning)
        {
            gameStartTimestamp = (int)changedProps[PROP_GAME_START_TS];
            int msUntilStart = gameStartTimestamp - PhotonNetwork.ServerTimestamp;
            StartCoroutine(CountdownAndStart(Mathf.Max(0, msUntilStart / 1000f)));
        }


        RefreshOpponentUI(changedProps);
    }

    // -------------------------------------------------------
    //  COUNTDOWN (ซิงค์กับ ServerTimestamp)
    // -------------------------------------------------------
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

    // -------------------------------------------------------
    //  ROUND MANAGEMENT
    // -------------------------------------------------------
    void BeginRound(int round)
    {
        currentRound = round;
        piecesPlaced = 0;
        pieces.Clear();

        roundText.text = $"ภาพที่ {round} / {TOTAL_ROUNDS}";

        if (PhotonNetwork.IsMasterClient)
            SpawnPieces();

        isRunning = true;

        SaveRoundToRoom(round);
    }

    void SpawnPieces()
    {

        totalPieces = 9;
        float spacing = 1.2f;

        for (int i = 0; i < totalPieces; i++)
        {

            int col = i % 3;
            int row = i / 3;
            Vector2 targetPos = (Vector2)boardParent.position
                                + new Vector2(col * spacing - spacing, row * spacing - spacing);


            Vector2 spawnPos = (Vector2)pieceSpawnArea.position
                                + new Vector2(Random.Range(-3f, 3f), Random.Range(-0.5f, 0.5f));

            GameObject obj = PhotonNetwork.Instantiate(
                jigsawPiecePrefab.name,
                spawnPos,
                Quaternion.identity
            );

            JigsawPiece piece = obj.GetComponent<JigsawPiece>();
            piece.pieceIndex = i;
            piece.originalPosition = spawnPos;
            piece.targetPosition = targetPos;

            pieces.Add(piece);
        }
    }

    // -------------------------------------------------------
    //  PIECE PLACED CALLBACK (เรียกจาก JigsawPiece.RPC_PlacePiece)
    // -------------------------------------------------------
    public void OnPiecePlaced()
    {
        piecesPlaced++;
        Debug.Log($"[Team {myTeam}] Pieces: {piecesPlaced}/{totalPieces}");

        if (piecesPlaced >= totalPieces)
            OnRoundComplete();
    }

    void OnRoundComplete()
    {
        if (currentRound >= TOTAL_ROUNDS)
        {
            FinishGame();
        }
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

    IEnumerator LoadSummaryScene()
    {
        yield return new WaitForSeconds(2f);
        PhotonNetwork.LoadLevel("SummaryScene");
    }

    void DestroyAllPieces()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        foreach (var p in pieces)
        {
            if (p != null)
                PhotonNetwork.Destroy(p.gameObject);
        }
        pieces.Clear();
    }

    // -------------------------------------------------------
    //  ROOM PROPERTIES: บันทึก/อ่านข้อมูลทีม
    // -------------------------------------------------------
    void SaveRoundToRoom(int round)
    {
        string key = (myTeam == 1) ? PROP_TEAM1_ROUND : PROP_TEAM2_ROUND;
        var props = new Hashtable { { key, round } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    void SaveTimeToRoom(float time)
    {
        string key = (myTeam == 1) ? PROP_TEAM1_TIME : PROP_TEAM2_TIME;
        var props = new Hashtable { { key, time } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    void RefreshOpponentUI(Hashtable props)
    {

        if (props.ContainsKey(PROP_TEAM1_ROUND))
            Debug.Log($"ทีม 1 ตอนนี้อยู่ Round: {props[PROP_TEAM1_ROUND]}");
        if (props.ContainsKey(PROP_TEAM2_ROUND))
            Debug.Log($"ทีม 2 ตอนนี้อยู่ Round: {props[PROP_TEAM2_ROUND]}");
    }

    // -------------------------------------------------------
    //  CONFLICT EFFECT (เรียกจาก JigsawPiece)
    // -------------------------------------------------------
    public void ShowConflictEffect(Vector3 position)
    {
        if (conflictEffectPrefab == null) return;
        var fx = Instantiate(conflictEffectPrefab, position, Quaternion.identity);
        Destroy(fx, 1.5f);
    }

    // -------------------------------------------------------
    //  UI HELPERS
    // -------------------------------------------------------
    void UpdateTimerUI()
    {
        int min = (int)(elapsedTime / 60);
        int sec = (int)(elapsedTime % 60);
        timerText.text = $"{min:00}:{sec:00}";
    }
}