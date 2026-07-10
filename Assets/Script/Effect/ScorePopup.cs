using UnityEngine;

public class ScorePopup : MonoBehaviour
{
    [Tooltip("ความเร็วในการลอยขึ้น")]
    public float floatSpeed = 2f;

    [Tooltip("เวลาที่จะแสดงก่อนหายไป (วินาที)")]
    public float destroyTime = 1f;

    void Start()
    {
        // สั่งทำลายตัวเองล่วงหน้าตามเวลาที่กำหนด
        Destroy(gameObject, destroyTime);
    }

    void Update()
    {
        // ลอยขึ้นด้านบนเรื่อยๆ
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;
    }
}