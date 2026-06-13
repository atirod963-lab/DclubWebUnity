using UnityEngine;

public class SpawnerManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("ใส่ Prefab ของ Healthy Food และ Junk Food ตรงนี้")]
    public GameObject[] foodPrefabs;

    [Tooltip("ใส่จุด Spawn (Empty GameObjects) ตรงนี้")]
    public Transform[] spawnPoints;

    [Tooltip("เวลานับถอยหลังก่อน Spawn ชิ้นต่อไป (วินาที)")]
    public float spawnInterval = 0.1f;

    // ตัวแปรสำหรับจับเวลา
    private float timer;

    void Start()
    {
        // ตั้งค่าให้เวลาเริ่มต้นเท่ากับรอบเวลาที่กำหนด
        timer = spawnInterval;
    }

    void Update()
    {
        // หักลบเวลาลงเรื่อยๆ ตามเฟรมเรตจริง (Time.deltaTime)
        timer -= Time.deltaTime;

        // เมื่อเวลานับถอยหลังลงมาถึง 0 หรือติดลบ
        if (timer <= 0f)
        {
            SpawnRandomFood(); // เรียกใช้ฟังก์ชันเสกของ

            timer = spawnInterval; // รีเซ็ตเวลากลับไปที่ 1 วินาทีใหม่
        }
    }

    // ฟังก์ชันสำหรับสุ่มและสร้างวัตถุ
    void SpawnRandomFood()
    {
        // เช็คความปลอดภัย ป้องกันเกมพังถ้าลืมใส่ช่องใน Inspector
        if (foodPrefabs.Length == 0 || spawnPoints.Length == 0)
        {
            Debug.LogWarning("ลืมใส่ Prefabs หรือ Spawn Points ใน Inspector หรือเปล่า?");
            return;
        }

        // 1. สุ่มเลือกไอเทม (ระหว่าง 0 ถึงจำนวนของใน Array)
        int randomFoodIndex = Random.Range(0, foodPrefabs.Length);
        GameObject selectedFood = foodPrefabs[randomFoodIndex];

        // 2. สุ่มเลือกตำแหน่งจุดเกิด
        int randomSpawnIndex = Random.Range(0, spawnPoints.Length);
        Transform selectedPoint = spawnPoints[randomSpawnIndex];

        // 3. สร้างวัตถุ (Instantiate) ออกมาที่ตำแหน่งของจุดเกิด
        Instantiate(selectedFood, selectedPoint.position, selectedPoint.rotation);
    }
}