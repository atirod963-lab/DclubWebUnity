using UnityEngine;
using System.Collections;

public class DestroyEffect : MonoBehaviour
{
    [Header("Effect Settings")]
    public float floatUpDistance = 1.5f;
    public float effectDuration = 0.4f;
    public float punchScale = 1.4f;

    // เรียกจากภายนอกได้ (สำหรับ Hoop)
    public void PlayEffectManually()
    {
        SpawnEffect();
    }

    void OnDestroy()
    {
        if (!gameObject.scene.isLoaded) return;
        SpawnEffect();
    }

    void SpawnEffect()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        GameObject effect = new GameObject("DestroyEffect");
        effect.transform.position = transform.position;
        effect.transform.localScale = transform.localScale;

        SpriteRenderer effectSR = effect.AddComponent<SpriteRenderer>();
        effectSR.sprite = sr.sprite;
        effectSR.color = sr.color;
        effectSR.sortingLayerName = sr.sortingLayerName;
        effectSR.sortingOrder = sr.sortingOrder + 1;

        EffectAnimator animator = effect.AddComponent<EffectAnimator>();
        animator.Play(floatUpDistance, effectDuration, punchScale);
    }
}

public class EffectAnimator : MonoBehaviour
{
    public void Play(float floatDist, float duration, float punch)
    {
        StartCoroutine(Animate(floatDist, duration, punch));
    }

    IEnumerator Animate(float floatDist, float duration, float punch)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.up * floatDist;
        Vector3 originalScale = transform.localScale;
        Vector3 punchedScale = originalScale * punch;

        float elapsed = 0f;
        float punchDuration = duration * 0.3f;
        float fadeDuration = duration * 0.7f;

        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / punchDuration;
            transform.localScale = Vector3.Lerp(originalScale, punchedScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            float smoothT = t * t * (3f - 2f * t);

            transform.position = Vector3.Lerp(startPos, endPos, smoothT);
            transform.localScale = Vector3.Lerp(punchedScale, originalScale * 0.5f, t);

            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                sr.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}