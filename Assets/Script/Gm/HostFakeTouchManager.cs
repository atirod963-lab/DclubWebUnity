using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections;

public class HostFakeTouchManager : MonoBehaviour
{
    [Header("Fake Touch Settings")]
    [Tooltip("ลาก Host_V มาใส่ตรงนี้")]
    public RectTransform canvasArea;

    [Header("Speed Settings")]
    public float minDelay = 0.2f;
    public float maxDelay = 1.0f;

    private bool isSimulating = false;

    void Start()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Role"))
        {
            string role = (string)PhotonNetwork.LocalPlayer.CustomProperties["Role"];
            if (role == "Spectator")
            {
                isSimulating = true;
                StartCoroutine(SimulatePlayerTouch());
            }
        }
    }

    IEnumerator SimulatePlayerTouch()
    {
        while (isSimulating)
        {
            float waitTime = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(waitTime);

            float randomScreenX = Random.Range(0, Screen.width);
            float randomScreenY = Random.Range(0, Screen.height);
            Vector3 randomScreenPos = new Vector3(randomScreenX, randomScreenY, 0);

            SpawnVisualClick(randomScreenPos);
            CheckRealTouch(randomScreenPos);
        }
    }

    void CheckRealTouch(Vector3 screenPosition)
    {
        // 🌳 เช็คว่าถ้าเป็นด่าน 3 (ด่านปลูกต้นไม้)
        if (TreeGameManager.Instance != null)
        {
            // อาศัย TouchManager2D เป็นตัวช่วยเช็คว่าเกมนับถอยหลัง 3..2..1 จบหรือยัง
            bool isGlobalGameActive = (TouchManager2D.Instance != null && TouchManager2D.Instance.isGameActive);

            if (TreeGameManager.Instance.isGameActive || isGlobalGameActive)
            {
                // บังคับงัดสวิตช์เปิดเกมด่าน 3 ให้ Host ทันที! (แก้บั๊กต้นไม้ไม่งอก)
                TreeGameManager.Instance.isGameActive = true;
                TreeGameManager.Instance.AddScore();
            }
            return;
        }

        // 🍎 ระบบยิงเลเซอร์หาอาหาร สำหรับด่าน 1 และ 2
        if (Camera.main == null) return;

        float distanceToCamera = Mathf.Abs(Camera.main.transform.position.z);
        screenPosition.z = distanceToCamera;

        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
        Vector2 touchPosition2D = new Vector2(worldPosition.x, worldPosition.y);

        Collider2D hitCollider = Physics2D.OverlapPoint(touchPosition2D);

        if (hitCollider != null)
        {
            if (hitCollider.CompareTag("Healthy Food") || hitCollider.CompareTag("Hoop") || hitCollider.CompareTag("Water") || hitCollider.CompareTag("Junk Food"))
            {
                if (hitCollider.CompareTag("Hoop"))
                {
                    HoopController hoop = hitCollider.GetComponent<HoopController>();
                    if (hoop != null)
                    {
                        if (hoop.hitEffectPrefab != null) Instantiate(hoop.hitEffectPrefab, hitCollider.transform.position, Quaternion.identity);
                        hoop.MoveToRandomPosition();
                    }
                }
                else
                {
                    Destroy(hitCollider.gameObject);
                }
            }
        }
    }

    void SpawnVisualClick(Vector3 screenPos)
    {
        if (canvasArea == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasArea, screenPos, null, out Vector2 localPoint);

        GameObject fakeClickObj = new GameObject("FakeClick");
        fakeClickObj.transform.SetParent(canvasArea, false);

        Image img = fakeClickObj.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.4f);

        RectTransform rect = fakeClickObj.GetComponent<RectTransform>();
        rect.anchoredPosition = localPoint;
        rect.sizeDelta = new Vector2(50f, 50f);

        StartCoroutine(FadeAndDestroy(fakeClickObj, img));
    }

    IEnumerator FadeAndDestroy(GameObject obj, Image img)
    {
        float timer = 0f;
        float duration = 0.3f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            obj.transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(1.5f, 1.5f, 1f), progress);
            img.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.4f, 0f, progress));
            yield return null;
        }
        Destroy(obj);
    }
}