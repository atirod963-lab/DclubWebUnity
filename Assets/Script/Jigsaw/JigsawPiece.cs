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
    [Header("ข้อมูลชิ้นส่วน")]
    public int pieceIndex;
    public Vector2 originalPosition;
    public Vector2 targetPosition;
    public float snapDistance = 0.5f;

    [Header("สีทีม")]
    public Color teamRedColor = Color.red;
    public Color teamBlueColor = new Color(0.2f, 0.4f, 1f);
    public Color defaultColor = Color.white;

    [Header("สถานะ")]
    public bool isPlaced = false;
    public bool isGrabbed = false;

    private SpriteRenderer sr;
    private Camera mainCam;
    private Vector2 grabOffset;
    private bool isDragging = false;
    private int grabberActorNumber = -1;

    private int activeFingerId = -1;

    private JigsawGameManager gameManager;

    // =====================================================
    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        mainCam = Camera.main;
        gameManager = FindObjectOfType<JigsawGameManager>();


        PhotonNetwork.AddCallbackTarget(this);
    }

    void OnDestroy()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    // =====================================================
    //  UPDATE — จัดการ Touch & Mouse Input
    // =====================================================
    void Update()
    {
        if (isPlaced) return;

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                Vector2 worldPos = mainCam.ScreenToWorldPoint(t.position);

                switch (t.phase)
                {
                    case TouchPhase.Began:
                        if (activeFingerId == -1 && HitTest(t.position))
                        {
                            activeFingerId = t.fingerId;
                            TryGrab(worldPos);
                        }
                        break;

                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        if (t.fingerId == activeFingerId && isDragging && photonView.IsMine)
                        {
                            transform.position = worldPos + grabOffset;
                        }
                        break;

                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (t.fingerId == activeFingerId)
                        {
                            activeFingerId = -1;
                            if (isDragging && photonView.IsMine)
                            {
                                TrySnap();
                            }
                        }
                        break;
                }
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (HitTest(Input.mousePosition))
                {
                    TryGrab(mainCam.ScreenToWorldPoint(Input.mousePosition));
                }
            }
            else if (Input.GetMouseButton(0))
            {
                if (isDragging && photonView.IsMine)
                {
                    Vector2 mouseWorld = mainCam.ScreenToWorldPoint(Input.mousePosition);
                    transform.position = mouseWorld + grabOffset;
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (isDragging && photonView.IsMine)
                {
                    TrySnap();
                }
            }
        }
    }

    // =====================================================
    //  HIT TEST
    // =====================================================
    bool HitTest(Vector2 screenPos)
    {
        Vector2 world = mainCam.ScreenToWorldPoint(screenPos);
        return GetComponent<Collider2D>().OverlapPoint(world);
    }

    // =====================================================
    //  TRY GRAB
    // =====================================================
    void TryGrab(Vector2 inputWorldPos)
    {
        if (isGrabbed)
        {
            photonView.RPC("RPC_ForceResetPiece", RpcTarget.All);
            return;
        }

        grabOffset = (Vector2)transform.position - inputWorldPos;
        photonView.RequestOwnership();
    }

    // =====================================================
    //  OWNERSHIP CALLBACKS
    // =====================================================
    public void OnOwnershipRequest(PhotonView targetView, Player requestingPlayer)
    {
        if (targetView == photonView)
            targetView.TransferOwnership(requestingPlayer);
    }

    public void OnOwnershipTransfered(PhotonView targetView, Player previousOwner)
    {
        if (targetView != photonView) return;
        if (!photonView.IsMine) return;

        isDragging = true;
        photonView.RPC("RPC_GrabPiece", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    public void OnOwnershipTransferFailed(PhotonView targetView, Player senderOfFailedRequest) { }

    // =====================================================
    //  TRY SNAP
    // =====================================================
    void TrySnap()
    {
        isDragging = false;
        float dist = Vector2.Distance(transform.position, targetPosition);

        if (dist <= snapDistance)
            photonView.RPC("RPC_PlacePiece", RpcTarget.All);
        else
            photonView.RPC("RPC_ReleasePiece", RpcTarget.All);
    }

    // =====================================================
    //  RPCs
    // =====================================================
    [PunRPC]
    void RPC_GrabPiece(int actorNumber)
    {
        isGrabbed = true;
        grabberActorNumber = actorNumber;

        bool isMine = actorNumber == PhotonNetwork.LocalPlayer.ActorNumber;
        sr.color = isMine ? defaultColor : teamRedColor;
    }

    [PunRPC]
    void RPC_ReleasePiece()
    {
        ResetGrabState();
        StartCoroutine(MoveToOrigin(originalPosition));
    }

    [PunRPC]
    void RPC_ForceResetPiece()
    {
        ResetGrabState();

        if (gameManager != null)
        {
            gameManager.ShowConflictEffect(transform.position);
        }

        StartCoroutine(MoveToOrigin(originalPosition));
    }

    [PunRPC]
    void RPC_PlacePiece()
    {
        isPlaced = true;
        ResetGrabState();

        transform.position = targetPosition;
        GetComponent<Collider2D>().enabled = false;

        if (photonView.IsMine)
        {
            if (gameManager != null)
            {
                gameManager.OnPiecePlaced();
            }
        }
    }

    // =====================================================
    //  HELPERS
    // =====================================================
    void ResetGrabState()
    {
        isGrabbed = false;
        grabberActorNumber = -1;
        isDragging = false;
        activeFingerId = -1;
        sr.color = defaultColor;
    }

    IEnumerator MoveToOrigin(Vector2 target)
    {
        float elapsed = 0f;
        float duration = 0.3f;
        Vector2 start = transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector2.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        transform.position = target;
    }
}