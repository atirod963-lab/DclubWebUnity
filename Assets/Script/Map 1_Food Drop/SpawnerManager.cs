using System.Collections.Generic;
using UnityEngine;

public class SpawnerManager : MonoBehaviour
{
    [Header("📦 สำรับไอเทม (ใส่ให้ครบ 6 ช่อง: บวก 3 / ลด 3)")]
    public GameObject[] foodPrefabs;

    [Header("📍 จุดเกิดของ")]
    public Transform[] spawnPoints;

    [Header("⏱️ ตั้งค่าความแฟร์")]
    [Tooltip("ระยะเวลาที่จะปล่อยของในถุงจนหมดเกลี้ยง (วินาที)")]
    public float spawnDuration = 18f;

    [Tooltip("ไอเทมแต่ละชนิดจะมีกี่ชิ้น? (เช่น มี 6 ชนิด x ใส่เลข 6 = มีของร่วงลงมาทั้งหมด 36 ชิ้น)")]
    public int copiesPerPrefab = 6;

    private List<GameObject> spawnPool = new List<GameObject>();
    private float calculatedInterval;
    private float timer;
    private int currentPoolIndex = 0;

    void Start()
    {
        GenerateAndShuffleBag();

        // 🧠 [ความฉลาดของระบบ] คำนวณความเร็วปล่อยของอัตโนมัติ!
        // เอาเวลา 18 วินาที ตั้ง หารด้วยจำนวนของทั้งหมดในถุง
        if (spawnPool.Count > 0)
        {
            calculatedInterval = spawnDuration / spawnPool.Count;
        }

        timer = calculatedInterval;
    }

    void GenerateAndShuffleBag()
    {
        spawnPool.Clear();

        // 1. โคลนของทั้ง 6 อย่าง ยัดลงถุงตามจำนวน copiesPerPrefab
        foreach (GameObject prefab in foodPrefabs)
        {
            if (prefab != null)
            {
                for (int i = 0; i < copiesPerPrefab; i++)
                {
                    spawnPool.Add(prefab);
                }
            }
        }

        // 2. สับไพ่ในถุงให้มั่วสนิทด้วยอัลกอริทึม Fisher-Yates
        for (int i = 0; i < spawnPool.Count; i++)
        {
            GameObject temp = spawnPool[i];
            int randomIndex = Random.Range(i, spawnPool.Count);
            spawnPool[i] = spawnPool[randomIndex];
            spawnPool[randomIndex] = temp;
        }
    }

    void Update()
    {
        // ถ้าหยิบของในถุงออกไปปล่อยจนครบหมดแล้ว ให้ปิดโรงงานทันที! (พักจอ 2 วินาทีรอของร่วงถึงพื้น)
        if (currentPoolIndex >= spawnPool.Count) return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            SpawnNextItemFromBag();
            timer = calculatedInterval;
        }
    }

    void SpawnNextItemFromBag()
    {
        if (spawnPoints.Length == 0) return;

        GameObject selectedFood = spawnPool[currentPoolIndex];
        Transform randomPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        Instantiate(selectedFood, randomPoint.position, randomPoint.rotation);

        currentPoolIndex++; // ขยับนิ้วไปรอหยิบของชิ้นถัดไปในถุง
    }
}