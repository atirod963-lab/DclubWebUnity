using System.Collections; // <--- [จุดที่ 1] เพิ่มเพื่อใช้ระบบหน่วงเวลา Coroutine
using UnityEngine;

public class HoopController : MonoBehaviour
{
    [Tooltip("เวลาที่จะให้แป้นบาสอยู่บนจอก่อนย้าย (วินาที)")]
    public float moveInterval = 1f;
    private float timer;

    [Tooltip("ระยะขอบหน้าจอ เพื่อไม่ให้แป้นบาสเกิดชิดขอบจอเกินไป (ค่า 0.1 ถึง 0.2 กำลังดี)")]
    public float padding = 0.15f;

    // --- [จุดที่ 2] เพิ่มตัวแปรเก็บอนิเมเตอร์ และสวิตช์เบรกเวลา ---
    private Animator anim;
    private bool isPlaying = false;

    void Start()
    {
        anim = GetComponent<Animator>(); // ดึงคอมโพเนนต์ Animator มาเตรียมไว้
        MoveToRandomPosition();
    }

    void Update()
    {
        // --- [จุดที่ 3] ถ้ากำลังเล่นอนิเมชั่นอยู่ ให้แช่แข็งเวลานับถอยหลังไว้ก่อน ---
        if (isPlaying) return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            MoveToRandomPosition();
        }
    }

    // --- [จุดที่ 4] เพิ่มระบบรับคลิก -> เล่นอนิเมชั่น -> ย้ายที่ ---
    void OnMouseDown()
    {
        if (isPlaying) return; // กันกดเบิ้ล
        StartCoroutine(PlayAnimThenMove());
    }

    IEnumerator PlayAnimThenMove()
    {
        isPlaying = true;
        if (anim != null) anim.SetTrigger("Play");

        yield return new WaitForSeconds(0.66f); // หน่วงรอจนอนิเมชั่นเล่นจบ (0.33วิ / สปีด 0.5)

        MoveToRandomPosition(); // เรียกใช้ฟังก์ชันย้ายที่เดิมของคุณ
        isPlaying = false;
    }

    // ฟังก์ชันเดิมของคุณ 100% ไม่ได้แตะต้องครับ
    public void MoveToRandomPosition()
    {
        timer = moveInterval;

        float randomX = Random.Range(padding, 1f - padding);
        float randomY = Random.Range(padding, 1f - padding);

        float distanceToCamera = Mathf.Abs(Camera.main.transform.position.z);

        Vector3 viewportPosition = new Vector3(randomX, randomY, distanceToCamera);
        Vector3 worldPosition = Camera.main.ViewportToWorldPoint(viewportPosition);

        worldPosition.z = 0f;
        transform.position = worldPosition;
    }
}