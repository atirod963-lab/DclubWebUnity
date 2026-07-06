using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(PhotonTransformView))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class JigsawPiece : MonoBehaviourPun, IPunOwnershipCallbacks
{
    [Header("ตั้งค่าไฟล์รูปประจำตัว")]
    public string spriteSheetName = "Galactic pink Multi";

    [Header("ข้อมูลชิ้นส่วน")]
    public int pieceIndex;
    public Vector2 originalPosition;
    public Vector2 targetPosition;
    public float snapDistance = 0.5f;

    [Header("สีทีม")]
    public Color teamRedColor = Color.red;
    public Color defaultColor = Color.white;

    [Header("สถานะ")]
    public bool isPlaced = false;
    public bool isGrabbed = false;

    private SpriteRenderer sr;
    private Camera mainCam;
    private Vector2 grabOffset;
    private bool isDragging = false;
    private JigsawGameManager gameManager;
    private int originalSortingOrder;

    // เพิ่มตัวแปร Static เพื่อล็อกไม่ให้หยิบหลายชิ้นพร้อมกัน
    public static JigsawPiece currentlyDraggingPiece = null;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        mainCam = Camera.main;
        gameManager = FindObjectOfType<JigsawGameManager>();
        PhotonNetwork.AddCallbackTarget(this);
    }

    void Start()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        originalSortingOrder = sr.sortingOrder;

        Sprite[] allSlices = Resources.LoadAll<Sprite>(spriteSheetName);
        if (allSlices != null && pieceIndex < allSlices.Length)
        {
            sr.sprite = allSlices[pieceIndex];

            if (GetComponent<BoxCollider2D>() is BoxCollider2D col)
            {
                col.size = sr.sprite.bounds.size;
                col.offset = Vector2.zero;
            }
        }
    }

    void OnDestroy() => PhotonNetwork.RemoveCallbackTarget(this);

    void Update()
    {
        if (isPlaced) return;
        if (mainCam == null) mainCam = Camera.main;

        // ถ้ามีการลากชิ้นอื่นอยู่ ห้ามชิ้นนี้ทำงาน
        if (currentlyDraggingPiece != null && currentlyDraggingPiece != this) return;

        Vector2 currentMouse = mainCam.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButtonDown(0))
        {
            // เช็คว่าเป็นชิ้นบนสุดไหมก่อนเริ่มลาก
            if (IsTopPieceAt(Input.mousePosition))
            {
                TryGrab(currentMouse);
            }
        }
        else if (Input.GetMouseButton(0))
        {
            if (isDragging && photonView.IsMine) transform.position = currentMouse + grabOffset;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (isDragging && photonView.IsMine) TrySnap();
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

        if (photonView.IsMine) StartDragging();
        else photonView.RequestOwnership();
    }

    void StartDragging()
    {
        isDragging = true;
        currentlyDraggingPiece = this;
        sr.sortingOrder = 999;
        photonView.RPC("RPC_GrabPiece", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    void TrySnap()
    {
        isDragging = false;
        currentlyDraggingPiece = null;
        sr.sortingOrder = originalSortingOrder;

        float dist = Vector2.Distance(transform.position, targetPosition);
        if (dist <= snapDistance) photonView.RPC("RPC_PlacePiece", RpcTarget.All);
        else photonView.RPC("RPC_ReleasePiece", RpcTarget.All);
    }

    // --- Implement IPunOwnershipCallbacks ---
    public void OnOwnershipRequest(PhotonView targetView, Player requestingPlayer)
    {
        if (targetView == photonView) targetView.TransferOwnership(requestingPlayer);
    }

    public void OnOwnershipTransfered(PhotonView targetView, Player previousOwner)
    {
        if (targetView == photonView && photonView.IsMine) StartDragging();
    }

    public void OnOwnershipTransferFailed(PhotonView targetView, Player senderOfFailedRequest) { }

    // --- RPCs ---
    [PunRPC] void RPC_GrabPiece(int actorNumber) { isGrabbed = true; sr.color = (actorNumber == PhotonNetwork.LocalPlayer.ActorNumber) ? defaultColor : teamRedColor; }
    [PunRPC] void RPC_ReleasePiece() { isGrabbed = false; StartCoroutine(MoveToOrigin(originalPosition)); }
    [PunRPC] void RPC_ForceResetPiece() { isGrabbed = false; StartCoroutine(MoveToOrigin(originalPosition)); }
    [PunRPC] void RPC_PlacePiece() { isPlaced = true; isGrabbed = false; transform.position = targetPosition; 
        if (GetComponent<Collider2D>() != null) GetComponent<Collider2D>().enabled = false; sr.color = defaultColor; if (photonView.IsMine && gameManager != null) gameManager.OnPiecePlaced(); }

    IEnumerator MoveToOrigin(Vector2 target)
    {
        float elapsed = 0f;
        Vector2 start = transform.position;
        while (elapsed < 0.2f) { elapsed += Time.deltaTime; transform.position = Vector2.Lerp(start, target, elapsed / 0.2f); yield return null; }
        transform.position = target;
    }
}