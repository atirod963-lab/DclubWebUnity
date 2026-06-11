using UnityEngine;

public class TouchManager2D : MonoBehaviour
{
    public int score = 0;

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
        // แปลงพิกัดจากหน้าจอ (Screen Space) ให้เป็นพิกัดในเกม (World Space)
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);

        // ตัดแกน Z ทิ้งไป เพราะเราทำเกม 2D
        Vector2 touchPosition2D = new Vector2(worldPosition.x, worldPosition.y);

        // เช็คว่าจุดที่แตะ มี Collider 2D ตัวไหนวางอยู่ตรงนั้นไหม
        Collider2D hitCollider = Physics2D.OverlapPoint(touchPosition2D);

        // ถ้าแตะโดนวัตถุที่มี Collider 2D
        if (hitCollider != null)
        {
            // ตรวจสอบ Tag ว่าใช่ของที่ได้คะแนนหรือไม่
            if (hitCollider.CompareTag("GotScore"))
            {
                score += 10;
                Debug.Log("แตะโดนเป้าหมาย! แต้มรวม: " + score);

                // ทำลายวัตถุ หรือสั่งให้มันหายไป
                Destroy(hitCollider.gameObject);
            }
            else
            {
                Debug.Log("แตะโดนวัตถุอื่นที่ไม่ได้แต้ม");
            }
        }
    }
}