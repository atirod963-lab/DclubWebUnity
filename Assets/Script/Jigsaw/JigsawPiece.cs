using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class JigsawPiece : MonoBehaviour
{
    [Header("ตั้งค่าไฟล์รูปประจำตัว")]
    public string spriteSheetName = "";

    [Header("ข้อมูลชิ้นส่วน")]
    public int pieceIndex;
    public Vector2 originalPosition;
    public Vector2 targetPosition;
    public float snapDistance = 0.5f;

    [Header("สี")]
    public Color grabbedColor = Color.white;
    public Color defaultColor = Color.white;

    [Header("UI ป็อบอัพนิ้วบัง")]
    [Tooltip("ระยะความสูงของป็อบอัพที่จะลอยขึ้นมาเหนือจุดที่กด")]
    public float popupOffsetY = 5f;

    [Header("สถานะ")]
    public bool isPlaced = false;
    public bool isGrabbed = false;

    private SpriteRenderer sr;
    private Camera mainCam;
    private Vector2 grabOffset;
    private bool isDragging = false;
    private JigsawGameManager gameManager;
    private int originalSortingOrder;

    // ตัวแปรสำหรับเก็บออบเจกต์ป็อบอัพ
    private GameObject popupIndicator;

    public static JigsawPiece currentlyDraggingPiece = null;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        mainCam = Camera.main;
        gameManager = FindObjectOfType<JigsawGameManager>();
    }

    void Start()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        originalSortingOrder = sr.sortingOrder;
        sr.color = defaultColor;

        Sprite[] allSlices = Resources.LoadAll<Sprite>(spriteSheetName);
        if (allSlices != null && pieceIndex >= 0 && pieceIndex < allSlices.Length)
        {
            sr.sprite = allSlices[pieceIndex];

            if (GetComponent<BoxCollider2D>() is BoxCollider2D col)
            {
                col.size = sr.sprite.bounds.size;
                col.offset = Vector2.zero;
            }
        }
    }

    void Update()
    {
        if (isPlaced) return;
        if (mainCam == null) mainCam = Camera.main;

        // ถ้ามีการลากชิ้นอื่นอยู่ (ในเครื่องเดียวกัน) ห้ามชิ้นนี้ทำงาน
        if (currentlyDraggingPiece != null && currentlyDraggingPiece != this) return;

        Vector2 currentMouse = mainCam.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButtonDown(0))
        {
            if (IsTopPieceAt(Input.mousePosition))
            {
                TryGrab(currentMouse);
            }
        }
        else if (Input.GetMouseButton(0))
        {
            if (isDragging) transform.position = currentMouse + grabOffset;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (isDragging) TrySnap();
        }
    }

    bool IsTopPieceAt(Vector2 screenPos)
    {
        Vector2 worldPos = mainCam.ScreenToWorldPoint(screenPos);
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPos);

        JigsawPiece topPiece = null;
        int highestOrder = -1;

        foreach (var hit in hits)
        {
            JigsawPiece p = hit.GetComponent<JigsawPiece>();
            if (p != null && !p.isPlaced)
            {
                if (p.sr.sortingOrder > highestOrder)
                {
                    highestOrder = p.sr.sortingOrder;
                    topPiece = p;
                }
            }
        }
        return topPiece == this;
    }

    void TryGrab(Vector2 inputWorldPos)
    {
        if (isGrabbed) return;

        grabOffset = (Vector2)transform.position - inputWorldPos;
        StartDragging();
    }

    void StartDragging()
    {
        isDragging = true;
        isGrabbed = true;
        currentlyDraggingPiece = this;

        // หรี่สีชิ้นส่วนตัวจริงที่อยู่ใต้นิ้วลงเล็กน้อย
        sr.sortingOrder = 999;
        sr.color = grabbedColor;

        // 🌟 สร้าง Popup ลอยเหนือชิ้นส่วน
        if (popupIndicator == null)
        {
            popupIndicator = new GameObject("PopupIndicator");
            popupIndicator.transform.SetParent(transform); // ให้มันวิ่งตามชิ้นส่วนหลัก
            popupIndicator.transform.localPosition = new Vector3(0, popupOffsetY, 0); // ยกขึ้นแนวตั้ง
            popupIndicator.transform.localScale = Vector3.one * 1.2f; // ขยายขนาดให้มองเห็นชัดขึ้น

            // ก๊อปปี้ภาพมาใส่ใน Popup
            SpriteRenderer popupSr = popupIndicator.AddComponent<SpriteRenderer>();
            popupSr.sprite = sr.sprite;
            popupSr.sortingOrder = 1000; // ให้อยู่บนสุดเสมอ
            popupSr.color = Color.white;
        }
    }

    void TrySnap()
    {
        isDragging = false;
        isGrabbed = false;
        currentlyDraggingPiece = null;
        sr.sortingOrder = originalSortingOrder;
        sr.color = defaultColor;

        // 🌟 ทำลาย Popup ทิ้งทันทีเมื่อปล่อยมือ
        if (popupIndicator != null)
        {
            Destroy(popupIndicator);
            popupIndicator = null;
        }

        bool isDecoy = (targetPosition.x > 9000f);
        int checkIndex = isDecoy ? -1 : pieceIndex;

        if (gameManager == null)
        {
            return;
        }

        int dropStatus = gameManager.ValidateDrop(transform.position, checkIndex, snapDistance, out Vector2 snappedPos);

        if (dropStatus == 1)
        {
            PlacePiece(snappedPos);
        }
        else if (dropStatus == -1)
        {
            ResetBoardLocally();
        }
        else
        {
            DropFreely(transform.position);
        }
    }

    void DropFreely(Vector2 newPos)
    {
        isGrabbed = false;
        transform.position = newPos;
    }

    void PlacePiece(Vector2 exactPos)
    {
        isPlaced = true;
        isGrabbed = false;
        transform.position = exactPos;
        if (GetComponent<Collider2D>() != null) GetComponent<Collider2D>().enabled = false;
        sr.color = defaultColor;

        if (gameManager != null) gameManager.OnPiecePlaced(pieceIndex);
    }

    void ResetBoardLocally()
    {
        if (gameManager != null)
        {
            gameManager.ShowConflictEffect(transform.position);
            gameManager.TriggerPenaltyReset();
        }
    }
}