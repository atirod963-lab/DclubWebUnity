using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    [Tooltip("เวลาที่จะให้อนิเมชั่นเล่นจนจบก่อนหายไป (วินาที)")]
    public float destroyTime = 0.66f;

    void Start()
    {
        // สั่งทำลายตัวเองล่วงหน้าตามเวลาที่กำหนดทันทีที่เกิดมา
        Destroy(gameObject, destroyTime);
    }
}