using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TrashMinigameController : MonoBehaviour
{
    [Header("เวลาที่จะให้ถังขยะอยู่บนจอก่อนย้าย (วินาที)")]
    public float moveInterval = 1f;
    private float timer;

    [Header("UI บอกเป้าหมายผู้เล่น")]
    [Tooltip("ลาก UI Text (TextMeshPro) มาใส่ช่องนี้")]
    public TextMeshProUGUI targetText;
    [Tooltip("ลาก UI Image ที่จะให้แสดงรูปถังเป้าหมายมาใส่ช่องนี้")]
    public Image targetImage;

    // ซ่อนตัวแปรนี้จาก Inspector
    private TrashCan[] trashCans;

    void Start()
    {
        // กวาดหาถังขยะทุกใบที่มีอยู่ในฉาก
        trashCans = FindObjectsByType<TrashCan>(FindObjectsSortMode.None);

        if (trashCans.Length == 0)
        {
            Debug.LogError("ไม่พบถังขยะในฉากเลย!");
            return;
        }

        // 🌟 สุ่มเป้าหมายแค่ครั้งเดียวใน Start()
        AssignTarget();
        MoveAllTrashCans();
    }

    void Update()
    {
        if (trashCans == null || trashCans.Length == 0) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            MoveAllTrashCans();
        }
    }

    public void AssignTarget()
    {
        if (trashCans == null || trashCans.Length == 0) return;

        // รีเซ็ตสถานะเป็นตัวหลอกให้หมดก่อน
        foreach (TrashCan tc in trashCans)
        {
            tc.isTarget = false;
        }

        // สุ่ม 1 ถังให้เป็นของจริงประจำเกมรอบนี้
        int randomIndex = Random.Range(0, trashCans.Length);
        trashCans[randomIndex].isTarget = true;

        // 🌟 อัปเดต UI ให้ผู้เล่นเห็น
        if (targetText != null)
        {
            targetText.text = "กดถัง ";
            targetText.gameObject.SetActive(true);
        }

        if (targetImage != null)
        {
            // ดูดภาพจาก SpriteRenderer ของถังใบที่สุ่มได้มาใส่ใน UI
            SpriteRenderer sr = trashCans[randomIndex].GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                targetImage.sprite = sr.sprite;
                targetImage.gameObject.SetActive(true);
            }
        }

        Debug.Log($"เกมรอบนี้ ถังเป้าหมายคือ: {trashCans[randomIndex].gameObject.name}");
    }

    public void MoveAllTrashCans()
    {
        if (trashCans == null || trashCans.Length == 0) return;

        foreach (TrashCan tc in trashCans)
        {
            tc.MoveToRandomPosition();
        }
        timer = moveInterval;
    }
}