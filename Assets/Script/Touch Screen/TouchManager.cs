using UnityEngine;

public class TouchManager : MonoBehaviour
{
    // ตัวแปรสมมติสำหรับเก็บคะแนน
    public int score = 0;

    void Update()
    {
        // 1. ตรวจสอบการสัมผัสหน้าจอมือถือ
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            // ตรวจสอบว่าเพิ่งเริ่มแตะหน้าจอ (หลีกเลี่ยงการนับซ้ำตอนกดค้าง)
            if (touch.phase == TouchPhase.Began)
            {
                CheckTouchObject(touch.position);
            }
        }
        // 2. ตรวจสอบการคลิกเมาส์ซ้าย (สำหรับทดสอบบนคอมพิวเตอร์)
        else if (Input.GetMouseButtonDown(0))
        {
            CheckTouchObject(Input.mousePosition);
        }
    }

    // ฟังก์ชันสำหรับยิง Raycast เพื่อตรวจสอบวัตถุ
    void CheckTouchObject(Vector3 screenPosition)
    {
        // สร้างรังสี (Ray) จากจุดที่กล้องมองผ่านไปยังจุดที่แตะบนหน้าจอ
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit hit; // ตัวแปรสำหรับเก็บข้อมูลวัตถุที่ถูกชน

        // ยิงรังสีออกไป (หากเป็นเกม 2D ให้ใช้ Physics2D.Raycast แทน)
        if (Physics.Raycast(ray, out hit))
        {
            // ตรวจสอบว่าวัตถุที่ชน มี Tag ตรงกับที่เราตั้งไว้หรือไม่
            if (hit.collider.CompareTag("GotScore"))
            {
                // ถ้าใช่ ให้เพิ่มคะแนน
                score += 10;
                Debug.Log("ได้แต้ม! คะแนนปัจจุบัน: " + score);

                // สั่งทำลายวัตถุนั้นทิ้ง (หรือเปลี่ยนสี เล่นเอฟเฟกต์ ฯลฯ ตามต้องการ)
                Destroy(hit.collider.gameObject);
            }
            else
            {
                // ถ้าชนวัตถุอื่นที่ไม่มี Tag ที่กำหนด
                Debug.Log("โดนสิ่งอื่น ไม่ได้แต้ม");
            }
        }
    }
}