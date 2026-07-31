using UnityEngine;

[RequireComponent(typeof(Collider2D))] // บังคับให้ต้องมี Collider ไว้รับการกด
public class TrashCan : MonoBehaviour
{
    [Header("สถานะ (Manager จะเป็นคนเปลี่ยนค่านี้เอง)")]
    public bool isTarget = false;

    [Header("ตั้งค่าระยะการเกิด")]
    [Tooltip("ระยะขอบหน้าจอ เพื่อไม่ให้ถังขยะเกิดชิดขอบจอเกินไป")]
    public float padding = 0.15f;
    [Tooltip("ระยะทางขั้นต่ำที่ต้องกระโดดหนีจากจุดเดิม")]
    public float minMoveDistance = 0.35f;

    [Header("เอฟเฟกต์")]
    public GameObject hitEffectPrefab;

    private TrashMinigameController controller;

    void Start()
    {
        // ดึงตัว Controller ประจำมินิเกมมาใช้งาน
        controller = FindFirstObjectByType<TrashMinigameController>();
    }

    // ลอจิกย้ายตำแหน่งหนี (อ้างอิงจากลอจิกแป้นบาสเดิม)
    public void MoveToRandomPosition()
    {
        float distanceToCamera = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 currentViewportPos = Camera.main.WorldToViewportPoint(transform.position);

        float randomX = 0f;
        float randomY = 0f;
        float dist = 0f;
        int attempts = 0;

        // ระบบสุ่มวนซ้ำ: สุ่มตำแหน่งไปเรื่อยๆ จนกว่าระยะห่างจะมากกว่า minMoveDistance
        do
        {
            randomX = Random.Range(padding, 1f - padding);
            randomY = Random.Range(padding, 1f - padding);

            dist = Vector2.Distance(new Vector2(currentViewportPos.x, currentViewportPos.y), new Vector2(randomX, randomY));
            attempts++;

        } while (dist < minMoveDistance && attempts < 50);

        Vector3 viewportPosition = new Vector3(randomX, randomY, distanceToCamera);
        Vector3 worldPosition = Camera.main.ViewportToWorldPoint(viewportPosition);

        worldPosition.z = 0f;
        transform.position = worldPosition;
    }

    // ฟังก์ชันนี้จะทำงานเมื่อผู้เล่นกดโดนถังขยะ
    /*public void OnInteract()
    {
        if (isTarget)
        {
            // ถังของจริง: ได้คะแนน
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }

            // TODO: เรียกโค้ดบวกคะแนน +1 ส่งไปที่ตัวเกมหลักของคุณตรงนี้
            Debug.Log("ได้คะแนน! กดโดนถังขยะจริง");

            // ย้ายตำแหน่งใหม่ทันทีที่กดโดน เพื่อให้เกมเดินต่อ
            if (controller != null)
            {
                controller.MoveAllTrashCans();
            }
        }
        else
        {
            // ตัวหลอก: ไม่ทำอะไร
            Debug.Log("ตัวหลอก! ไม่มีอะไรเกิดขึ้น");
        }
    }

    // หากระบบเกมของคุณรับการกดผ่าน OnMouseDown สามารถเปิดใช้โค้ดนี้ได้เลย
    // หรือถ้าใช้ TouchManager2D ก็ให้โยงมาเรียก OnInteract() ได้เหมือนกันครับ
    void OnMouseDown()
    {
        OnInteract();
    }*/
}