using UnityEngine;

public class DestroyTimer : MonoBehaviour
{
    public float lifetime = 3f; // ตั้งเวลาทำลายตัวเอง (3 วินาที)

    void Start()
    {
        // สั่งทำลาย Object นี้หลังจากเวลาผ่านไปตามค่า lifetime
        Destroy(gameObject, lifetime);
    }
}