using System.Collections; // <--- [¨Ø´·Õè 1] à¾ÔèÁà¾×èÍãªéÃÐººË¹èÇ§àÇÅÒ Coroutine
using UnityEngine;

public class HoopController : MonoBehaviour
{
    [Tooltip("àÇÅÒ·Õè¨ÐãËéá»é¹ºÒÊÍÂÙèº¹¨Í¡èÍ¹ÂéÒÂ (ÇÔ¹Ò·Õ)")]
    public float moveInterval = 1f;
    private float timer;

    [Tooltip("ÃÐÂÐ¢ÍºË¹éÒ¨Í à¾×èÍäÁèãËéá»é¹ºÒÊà¡Ô´ªÔ´¢Íº¨Íà¡Ô¹ä» (¤èÒ 0.1 ¶Ö§ 0.2 ¡ÓÅÑ§´Õ)")]
    public float padding = 0.15f;

    // --- [¨Ø´·Õè 2] à¾ÔèÁµÑÇá»Ãà¡çºÍ¹ÔàÁàµÍÃì áÅÐÊÇÔµªìàºÃ¡àÇÅÒ ---
    private Animator anim;
    private bool isPlaying = false;

    void Start()
    {
        anim = GetComponent<Animator>(); // ´Ö§¤ÍÁâ¾à¹¹µì Animator ÁÒàµÃÕÂÁäÇé
        MoveToRandomPosition();
    }

    void Update()
    {
        // --- [¨Ø´·Õè 3] ¶éÒ¡ÓÅÑ§àÅè¹Í¹ÔàÁªÑè¹ÍÂÙè ãËéáªèá¢ç§àÇÅÒ¹Ñº¶ÍÂËÅÑ§äÇé¡èÍ¹ ---
        if (isPlaying) return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            MoveToRandomPosition();
        }
    }

    // --- [¨Ø´·Õè 4] à¾ÔèÁÃÐººÃÑº¤ÅÔ¡ -> àÅè¹Í¹ÔàÁªÑè¹ -> ÂéÒÂ·Õè ---
    void OnMouseDown()
    {
        if (isPlaying) return; // ¡Ñ¹¡´àºÔéÅ
        StartCoroutine(PlayAnimThenMove());
    }

    IEnumerator PlayAnimThenMove()
    {
        isPlaying = true;

        // ถูกต้อง — เรียกผ่าน PlayEffectManually() เท่านั้น
        DestroyEffect destroyEffect = GetComponent<DestroyEffect>();
        if (destroyEffect != null) destroyEffect.PlayEffectManually();

        if (anim != null) anim.SetTrigger("Play");

        yield return new WaitForSeconds(0.66f); // Ë¹èÇ§ÃÍ¨¹Í¹ÔàÁªÑè¹àÅè¹¨º (0.33ÇÔ / Ê»Õ´ 0.5)

        MoveToRandomPosition(); // àÃÕÂ¡ãªé¿Ñ§¡ìªÑ¹ÂéÒÂ·Õèà´ÔÁ¢Í§¤Ø³
        isPlaying = false;
    }

    // ¿Ñ§¡ìªÑ¹à´ÔÁ¢Í§¤Ø³ 100% äÁèä´éáµÐµéÍ§¤ÃÑº
    public void MoveToRandomPosition()
    {
        timer = moveInterval;

        float randomX = Random.Range(padding, 1f - padding);
        float randomY = Random.Range(padding, 1f - padding);

        float distanceToCamera = Mathf.Abs(Camera.main.transform.position.z);

        Vector3 viewportPosition = new Vector3(randomX, randomY, distanceToCamera);
        Vector3 worldPosition = Camera.main.ViewportToWorldPoint(viewportPosition);

        worldPosition.z = 0f;
        transform.position = worldPosition;
    }

   
}