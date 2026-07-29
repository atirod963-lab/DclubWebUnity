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
    [Tooltip("สีของแผ่นพื้นหลัง")]
    public Color popupHighlightColor = Color.yellow;
    [Tooltip("ระยะความหนาของขอบ (Padding) ที่จะให้ใหญ่กว่าตัวจิ๊กซอว์")]
    public float highlightPadding = 0.3f;

    [Header("สถานะ")]
    public bool isPlaced = false;
    public bool isGrabbed = false;

    private SpriteRenderer sr;
    private Camera mainCam;
    private Vector2 grabOffset;
    private bool isDragging = false;
    private JigsawGameManager gameManager;
    private int originalSortingOrder;

    private GameObject popupIndicator;

    // 🌟 ตัวแปรเก็บภาพสี่เหลี่ยมสีขาวที่สร้างขึ้นจากโค้ด
    private static Sprite autoGenBgSprite;

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

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SFXId.JigsawPick);
        }

        sr.sortingOrder = 999;

        // 🌟 1. ลด Opacity ชิ้นส่วนจริงลงเหลือ 50%
        Color semiTransparent = grabbedColor;
        semiTransparent.a = 0.5f;
        sr.color = semiTransparent;

        if (popupIndicator == null)
        {
            popupIndicator = new GameObject("PopupIndicator");
            popupIndicator.transform.SetParent(transform);
            popupIndicator.transform.localPosition = new Vector3(0, popupOffsetY, 0);
            popupIndicator.transform.localScale = Vector3.one * 1.2f;

            // 🌟 2. สร้างแผ่นพื้นหลังสี่เหลี่ยมด้วยโค้ด (รับประกันว่าโผล่แน่นอน 100%)
            if (autoGenBgSprite == null)
            {
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                autoGenBgSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }

            GameObject highlightBg = new GameObject("HighlightBackground");
            highlightBg.transform.SetParent(popupIndicator.transform);
            highlightBg.transform.localPosition = Vector3.zero;

            SpriteRenderer highlightSr = highlightBg.AddComponent<SpriteRenderer>();
            highlightSr.sprite = autoGenBgSprite;
            highlightSr.sortingOrder = 999; // ให้อยู่ด้านหลังภาพ Popup
            highlightSr.color = popupHighlightColor;

            // คำนวณความกว้างยาวให้ใหญ่กว่าจิ๊กซอว์ชิ้นนั้นๆ
            float targetWidth = sr.sprite.bounds.size.x + highlightPadding;
            float targetHeight = sr.sprite.bounds.size.y + highlightPadding;

            highlightBg.transform.localScale = new Vector3(targetWidth, targetHeight, 1f);

            // 🌟 3. ภาพ Popup ตัวจิ๊กซอว์
            SpriteRenderer popupSr = popupIndicator.AddComponent<SpriteRenderer>();
            popupSr.sprite = sr.sprite;
            popupSr.sortingOrder = 1000;
            popupSr.color = Color.white;
        }
    }

    void TrySnap()
    {
        isDragging = false;
        isGrabbed = false;
        currentlyDraggingPiece = null;
        sr.sortingOrder = originalSortingOrder;

        // คืนค่าสีเดิม (Opacity กลับมาเต็ม 100%)
        sr.color = defaultColor;

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

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SFXId.JigsawDrop);
        }

        if (gameManager != null) gameManager.OnPiecePlaced(pieceIndex);
    }

    void ResetBoardLocally()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SFXId.JigsawError);
        }

        if (gameManager != null)
        {
            gameManager.ShowConflictEffect(transform.position);
            gameManager.TriggerPenaltyReset();
        }
    }
}