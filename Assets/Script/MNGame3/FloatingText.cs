using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    public float moveSpeed = 100f; // ความเร็วในการลอยขึ้น
    public float destroyTime = 0.5f; // เวลาที่จะทำลายตัวเอง

    private TextMeshProUGUI textMesh;
    private Color originalColor;
    private float timer;

    void Start()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        originalColor = textMesh.color;
        timer = 0;

        // ทำลายตัวเองเมื่อถึงเวลาที่กำหนด
        Destroy(gameObject, destroyTime);
    }

    void Update()
    {
        // สั่งให้ลอยขึ้นข้างบน (บวกค่า Y)
        transform.position += new Vector3(0, moveSpeed * Time.deltaTime, 0);

        // ทำ Effect Fade Out (ค่อยๆ จางหาย)
        timer += Time.deltaTime;
        float alpha = Mathf.Lerp(1, 0, timer / destroyTime);
        textMesh.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
    }
}