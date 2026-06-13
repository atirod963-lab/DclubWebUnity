using UnityEngine;
using TMPro; // 1. ต้องเพิ่มการเรียกใช้ Namespace นี้เพื่อควบคุม TextMesh Pro

public class TouchManager2D : MonoBehaviour
{
    public static TouchManager2D Instance;
    public int score = 0;
    public TextMeshProUGUI scoreText;

    void Awake()
    {
        // เช็คว่ามี TouchManager2D ตัวอื่นอยู่ในระบบหรือยัง
        if (Instance == null)
        {
            Instance = this; // กำหนดให้ตัวนี้คือตัวหลัก
            DontDestroyOnLoad(gameObject); // สั่งไม่ให้ทำลายเมื่อเปลี่ยน Scene
        }
        else
        {
            // ถ้ามีตัวหลักอยู่แล้ว และกำลังจะสร้างตัวซ้ำ ให้ทำลายตัวซ้ำทิ้งซะ
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // อัปเดตคะแนนเริ่มต้น (0) โชว์บนหน้าจอต้อนรับตอนเริ่มเกม
        UpdateScoreUI();
    }

    void Update()
    {
        // 1. ตรวจสอบการสัมผัสบนมือถือ
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                CheckTouch2D(touch.position);
            }
        }
        // 2. ตรวจสอบการคลิกเมาส์ (สำหรับทดสอบในคอมฯ)
        else if (Input.GetMouseButtonDown(0))
        {
            CheckTouch2D(Input.mousePosition);
        }
    }

    // ฟังก์ชันตรวจจับการแตะสำหรับ 2D
    void CheckTouch2D(Vector3 screenPosition)
    {
        // 1. [ส่วนที่เพิ่มใหม่] ดึงระยะห่างจากกล้องถึงระนาบ 2D แล้วยัดใส่แกน Z ก่อน
        float distanceToCamera = Mathf.Abs(Camera.main.transform.position.z);
        screenPosition.z = distanceToCamera;

        // 2. แปลงพิกัด (คราวนี้จะกางออกพอดีเป๊ะเต็มหน้าจอ 100%)
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
        Vector2 touchPosition2D = new Vector2(worldPosition.x, worldPosition.y);

        Collider2D hitCollider = Physics2D.OverlapPoint(touchPosition2D);

        // ... (โค้ดส่วนเช็ค Tag และให้คะแนนด้านล่างเก็บไว้เหมือนเดิมครับ) ...
        if (hitCollider != null)
        {
            if (hitCollider.CompareTag("Healthy Food"))
            {
                score += 1;
                Debug.Log("แตะโดนเป้าหมาย! แต้มรวม: " + score);

                // 3. เรียกใช้ฟังก์ชันอัปเดตตัวเลขบนหน้าจอทุกครั้งที่คะแนนเปลี่ยน
                UpdateScoreUI();

                Destroy(hitCollider.gameObject);
            }
            else if (hitCollider.CompareTag("Junk Food"))
            {
                score -= 1; // ปรับให้เขียนเข้าใจง่ายขึ้น
                Debug.Log("แตะโดนเป้าหมาย! แต้มรวม: " + score);

                // 3. เรียกใช้ฟังก์ชันอัปเดตตัวเลขบนหน้าจอทุกครั้งที่คะแนนเปลี่ยน
                UpdateScoreUI();

                Destroy(hitCollider.gameObject);
            }
            else if (hitCollider.CompareTag("Hoop"))
            {
                score += 1;
                Debug.Log("ชู้ตลง! แต้มรวม: " + score);
                UpdateScoreUI();

                // ---> ส่วนที่ต้องแก้ไข <---
                // ดึงสคริปต์ HoopController จากวัตถุที่เราแตะโดน
                HoopController hoop = hitCollider.GetComponent<HoopController>();
                if (hoop != null)
                {
                    // สั่งให้แป้นบาสย้ายตำแหน่งทันที (และมันจะรีเซ็ตเวลา 1 วิให้ด้วย)
                    hoop.MoveToRandomPosition();
                }
            }
            else
            {
                Debug.Log("แตะโดนวัตถุอื่นที่ไม่ได้แต้ม");
            }
        }
    }

    // 4. ฟังก์ชันสำหรับสั่งเปลี่ยนข้อความบนหน้าจอเกม
    void UpdateScoreUI()
    {
        // เช็คความปลอดภัยก่อนว่าเราได้ลากเลเยอร์ Text มาใส่ใน Inspector หรือยังเพื่อป้องกัน Error
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
    }
}