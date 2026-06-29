using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSceneManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static GameSceneManager Instance { get; private set; }

    // ── Types ─────────────────────────────────────────────────────────────────

    public enum TransitionType { Fade, SlideLeft, SlideRight, SlideUp, SlideDown, None }

    [Serializable]
    public class SceneEntry
    {
        [Tooltip("ชื่อที่ใช้เรียกใน code เช่น 'Gameplay'")]
        public string key;

        [Tooltip("ชื่อ scene จริงใน Build Settings")]
        public string sceneName;

        [Tooltip("Transition สำหรับ scene นี้โดยเฉพาะ")]
        public TransitionType transition = TransitionType.Fade;
    }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Scenes")]
    [SerializeField] private SceneEntry[] scenes;

    [Header("Transition")]
    [SerializeField] private Image overlayImage;
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private TransitionType defaultTransition = TransitionType.Fade;

    [Tooltip("Easing curve สำหรับ transition")]
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // ── Private state ─────────────────────────────────────────────────────────

    private bool _isTransitioning;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // ตรวจ key ซ้ำ
        var keys = new HashSet<string>();
        if (scenes != null)
        {
            foreach (var e in scenes)
                if (!keys.Add(e.key))
                    Debug.LogError($"[GameSceneManager] key ซ้ำ: '{e.key}'");
        }

        // ซ่อน overlay และ disable raycast
        if (overlayImage != null)
        {
            overlayImage.color = new Color(0, 0, 0, 0);
            overlayImage.rectTransform.anchoredPosition = Vector2.zero;
            overlayImage.raycastTarget = false;
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>เปลี่ยน scene โดยใช้ key ที่กำหนดใน Inspector</summary>
    public void GoTo(string key, Action onMidpoint = null)
    {
        if (_isTransitioning) return;

        SceneEntry entry = FindEntry(key);
        if (entry == null)
        {
            Debug.LogError($"[GameSceneManager] ไม่พบ key '{key}' ใน Scenes list");
            return;
        }

        StartCoroutine(TransitionRoutine(entry.sceneName, entry.transition, duration, onMidpoint));
    }

    /// <summary>โหลด scene ด้วยชื่อโดยตรง</summary>
    public void LoadScene(string sceneName,
                          TransitionType? transition = null,
                          float? customDuration = null,
                          Action onMidpoint = null)
    {
        if (_isTransitioning) return;
        StartCoroutine(TransitionRoutine(
            sceneName,
            transition ?? defaultTransition,
            customDuration ?? duration,
            onMidpoint));
    }

    /// <summary>โหลด scene ด้วย build index — แก้ bug GetSceneByBuildIndex แล้ว</summary>
    public void LoadScene(int buildIndex,
                          TransitionType? transition = null,
                          float? customDuration = null,
                          Action onMidpoint = null)
    {
        // ใช้ SceneUtility แทน GetSceneByBuildIndex เพราะ scene ยังไม่ได้โหลดอยู่
        string path = SceneUtility.GetScenePathByBuildIndex(buildIndex);
        string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
        LoadScene(sceneName, transition, customDuration, onMidpoint);
    }

    /// <summary>โหลด scene ถัดไปใน Build Settings</summary>
    public void LoadNextScene(TransitionType? transition = null)
    {
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("[GameSceneManager] ไม่มี scene ถัดไปแล้ว");
            return;
        }
        LoadScene(next, transition);
    }

    /// <summary>Reload scene ปัจจุบัน</summary>
    public void ReloadCurrentScene(TransitionType? transition = null)
        => LoadScene(SceneManager.GetActiveScene().name, transition);

    /// <summary>กำลัง transition อยู่หรือเปล่า</summary>
    public bool IsTransitioning => _isTransitioning;

    // ── Core coroutine ────────────────────────────────────────────────────────

    private IEnumerator TransitionRoutine(string sceneName,
                                          TransitionType type,
                                          float dur,
                                          Action onMidpoint)
    {
        _isTransitioning = true;

        try { } finally { } // dummy — ใช้ try/finally จริงใน coroutine ไม่ได้ ดูด้านล่าง

        // ตรวจว่า scene มีอยู่จริงใน Build Settings
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[GameSceneManager] ไม่พบ scene '{sceneName}' ใน Build Settings");
            _isTransitioning = false;
            yield break;
        }

        // enable raycast ระหว่าง transition เพื่อกัน input
        if (overlayImage != null) overlayImage.raycastTarget = true;

        // Phase 1: ปิดหน้าจอ
        yield return StartCoroutine(PlayTransition(type, Phase.Out, dur * 0.5f));

        onMidpoint?.Invoke();

        // Phase 2: โหลด scene
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError($"[GameSceneManager] LoadSceneAsync ล้มเหลว: '{sceneName}'");
            if (overlayImage != null) overlayImage.raycastTarget = false;
            _isTransitioning = false;
            yield break;
        }

        op.allowSceneActivation = false;
        while (op.progress < 0.9f) yield return null;
        op.allowSceneActivation = true;
        yield return null;

        // Phase 3: เปิดหน้าจอ
        yield return StartCoroutine(PlayTransition(type, Phase.In, dur * 0.5f));

        // disable raycast หลัง transition จบ
        if (overlayImage != null) overlayImage.raycastTarget = false;

        _isTransitioning = false;
    }

    // ── Transition phases ─────────────────────────────────────────────────────

    private enum Phase { Out, In }

    private IEnumerator PlayTransition(TransitionType type, Phase phase, float dur)
    {
        if (type == TransitionType.None || overlayImage == null) yield break;

        RectTransform rt = overlayImage.rectTransform;
        float elapsed = 0f;

        // ใช้ Canvas size แทน Screen size — ถูกต้องสำหรับ Canvas Scaler
        RectTransform canvasRect = overlayImage.canvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRect.rect.size;

        float alphaStart, alphaEnd;
        Vector2 posStart, posEnd;

        if (type == TransitionType.Fade)
        {
            alphaStart = phase == Phase.Out ? 0f : 1f;
            alphaEnd = phase == Phase.Out ? 1f : 0f;
            posStart = posEnd = Vector2.zero;
        }
        else
        {
            alphaStart = alphaEnd = 1f;
            Vector2 offscreen = GetOffscreenPos(type, canvasSize);

            // แก้ slide direction: overlay เลื่อนเข้าปิดจอ → โหลด → เลื่อนออก
            if (phase == Phase.Out)
            {
                posStart = offscreen;      // เริ่มนอกจอ → เข้ามาปิด
                posEnd = Vector2.zero;
            }
            else
            {
                posStart = Vector2.zero;   // อยู่กลางจอ → เลื่อนออก
                posEnd = -offscreen;
            }
        }

        overlayImage.color = new Color(0, 0, 0, alphaStart);
        rt.anchoredPosition = posStart;

        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = curve.Evaluate(Mathf.Clamp01(elapsed / dur));

            overlayImage.color = new Color(0, 0, 0, Mathf.Lerp(alphaStart, alphaEnd, t));
            rt.anchoredPosition = Vector2.Lerp(posStart, posEnd, t);

            yield return null;
        }

        overlayImage.color = new Color(0, 0, 0, alphaEnd);
        rt.anchoredPosition = posEnd;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private SceneEntry FindEntry(string key)
    {
        if (scenes == null) return null;
        foreach (var e in scenes)
            if (string.Equals(e.key, key, StringComparison.OrdinalIgnoreCase))
                return e;
        return null;
    }

    private Vector2 GetOffscreenPos(TransitionType type, Vector2 size)
    {
        return type switch
        {
            TransitionType.SlideLeft => new Vector2(-size.x, 0),
            TransitionType.SlideRight => new Vector2(size.x, 0),
            TransitionType.SlideUp => new Vector2(0, size.y),
            TransitionType.SlideDown => new Vector2(0, -size.y),
            _ => Vector2.zero,
        };
    }
}
