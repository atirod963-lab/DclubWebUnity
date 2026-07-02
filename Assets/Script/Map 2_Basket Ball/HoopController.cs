using UnityEngine;

public class HoopController : MonoBehaviour
{
    [Tooltip("เวลาที่จะให้แป้นบาสอยู่บนจอก่อนย้าย (วินาที)")]
    public float moveInterval = 1f;
    private float timer;

    [Tooltip("ระยะขอบหน้าจอ เพื่อไม่ให้แป้นบาสเกิดชิดขอบจอเกินไป")]
    public float padding = 0.15f;

    [Header("ใส่ Prefab อนิเมชั่นที่จะทิ้งไว้ตอนโดนกด")]
    public GameObject hitEffectPrefab;

    [Header("ระยะห่างขั้นต่ำ")]
    [Tooltip("ระยะทางขั้นต่ำที่ต้องกระโดดหนีจากจุดเดิม (แนะนำ 0.3 ถึง 0.4)")]
    public float minMoveDistance = 0.35f;

    void Start()
    {
        MoveToRandomPosition();
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            MoveToRandomPosition();
        }
    }

    public void MoveToRandomPosition()
    {
        timer = moveInterval;

        float distanceToCamera = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 currentViewportPos = Camera.main.WorldToViewportPoint(transform.position);

        float randomX = 0f;
        float randomY = 0f;
        float dist = 0f;
        int attempts = 0;

        // 🛡️ ระบบสุ่มวนซ้ำ: สุ่มตำแหน่งไปเรื่อยๆ จนกว่าระยะห่างจะมากกว่า minMoveDistance
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
}