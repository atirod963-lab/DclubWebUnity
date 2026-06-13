using UnityEngine;

public class HoopController : MonoBehaviour
{
    [Tooltip("เวลาที่จะให้แป้นบาสอยู่บนจอก่อนย้าย (วินาที)")]
    public float moveInterval = 1f;
    private float timer;

    [Tooltip("ระยะขอบหน้าจอ เพื่อไม่ให้แป้นบาสเกิดชิดขอบจอเกินไป (ค่า 0.1 ถึง 0.2 กำลังดี)")]
    public float padding = 0.15f;

    void Start()
    {
        // เริ่มเกมมาให้สุ่มตำแหน่งทันที
        MoveToRandomPosition();
    }

    void Update()
    {
        // เวลานับถอยหลังจะหยุดอัตโนมัติช่วงที่นับ 3 2 1 (เพราะ Time.timeScale = 0)
        timer -= Time.deltaTime;

        // ถ้านับครบ 1 วินาที โดยที่ไม่มีคนคลิก
        if (timer <= 0f)
        {
            MoveToRandomPosition(); // สั่งย้ายตำแหน่ง
        }
    }

    // ฟังก์ชันสำหรับวาร์ปย้ายตำแหน่ง (เรียกใช้เมื่อครบเวลา หรือถูกคลิก)
    // ฟังก์ชันสำหรับวาร์ปย้ายตำแหน่ง
    public void MoveToRandomPosition()
    {
        timer = moveInterval;

        float randomX = Random.Range(padding, 1f - padding);
        float randomY = Random.Range(padding, 1f - padding);

        // 1. [ส่วนที่แก้ไข] ดึงระยะห่างกล้องจริงๆ แทนการใช้เลข 10f ตายตัว
        float distanceToCamera = Mathf.Abs(Camera.main.transform.position.z);

        // 2. สร้างพิกัดใหม่ที่ใช้ระยะกล้องที่แท้จริง
        Vector3 viewportPosition = new Vector3(randomX, randomY, distanceToCamera);
        Vector3 worldPosition = Camera.main.ViewportToWorldPoint(viewportPosition);

        worldPosition.z = 0f;
        transform.position = worldPosition;
    }
}