using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    const string PrefBGMVolume = "Sound_BGM_Volume";
    const string PrefSFXVolume = "Sound_SFX_Volume";
    const string PrefMuted = "Sound_Muted";

    [Header("Library")]
    [SerializeField] SoundLibrary library;

    [Header("Pool")]
    [SerializeField] int sfxPoolSize = 5;

    [Header("Default Volume")]
    [Range(0f, 1f)][SerializeField] float defaultBGMVolume = 0.7f;
    [Range(0f, 1f)][SerializeField] float defaultSFXVolume = 1f;

    AudioSource bgmSource;
    AudioSource[] sfxPool;
    int sfxPoolIndex;

    float bgmVolume;
    float sfxVolume;
    bool isMuted;
    bool audioUnlocked;

    BGMId? pendingBGM;

    public float BGMVolume => bgmVolume;
    public float SFXVolume => sfxVolume;
    public bool IsMuted => isMuted;
    public bool IsAudioUnlocked => audioUnlocked;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupAudioSources();
        LoadVolumeSettings();
    }

    void Start()
    {
        PlayBGMForScene(SceneManager.GetActiveScene().name);

#if UNITY_EDITOR
        UnlockAudio();
#endif
    }

    void Update()
    {
        if (audioUnlocked)
            return;

        if (Input.GetMouseButtonDown(0) ||
            (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            UnlockAudio();
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void SetupAudioSources()
    {
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        sfxPoolSize = Mathf.Max(1, sfxPoolSize);
        sfxPool = new AudioSource[sfxPoolSize];

        for (int i = 0; i < sfxPoolSize; i++)
        {
            sfxPool[i] = gameObject.AddComponent<AudioSource>();
            sfxPool[i].loop = false;
            sfxPool[i].playOnAwake = false;
        }
    }

    void LoadVolumeSettings()
    {
        bgmVolume = PlayerPrefs.GetFloat(PrefBGMVolume, defaultBGMVolume);
        sfxVolume = PlayerPrefs.GetFloat(PrefSFXVolume, defaultSFXVolume);
        isMuted = PlayerPrefs.GetInt(PrefMuted, 0) == 1;
        ApplyVolume();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayBGMForScene(scene.name);
    }

    public void PlayBGMForScene(string sceneName)
    {
        BGMId bgm = GetBGMForScene(sceneName);
        if (bgm != BGMId.None)
            PlayBGM(bgm);
    }

    public static BGMId GetBGMForScene(string sceneName)
    {
        switch (sceneName)
        {
            case "menu":
                return BGMId.MenuTheme;
            case "MG1_1":
                return BGMId.MG1_FoodDrop;
            case "MG1_2":
                return BGMId.MG1_Basketball;
            case "MG1_3":
                return BGMId.MG1_TreePlanting;

            case "SummaryScene":
                return BGMId.Summary;

            case "menu_Jigsaw":
                return BGMId.menu_Jigsaw;
            case "GameplaySolo":
                return BGMId.GameplaySolo;
            case "GameplayTeam1":
                return BGMId.GameplayTeam1;
            case "GameplayTeam2":
                return BGMId.GameplayTeam2;
            case "HostMonitor":
                return BGMId.HostMonitor;

            default:
                return BGMId.None;
        }
    }

    public void UnlockAudio()
    {
        if (audioUnlocked)
            return;

        audioUnlocked = true;

        if (pendingBGM.HasValue)
        {
            BGMId queued = pendingBGM.Value;
            pendingBGM = null;
            PlayBGM(queued, true);
            return;
        }

        PlayBGMForScene(SceneManager.GetActiveScene().name);
    }

    public void PlayBGM(BGMId id, bool restartIfSame = false)
    {
        if (library == null || id == BGMId.None)
            return;

        AudioClip clip = library.GetBGM(id);
        if (clip == null)
            return;

        if (!audioUnlocked)
        {
            pendingBGM = id;
            bgmSource.clip = clip;
            return;
        }

        if (bgmSource.clip == clip && bgmSource.isPlaying && !restartIfSame)
            return;

        bgmSource.clip = clip;
        bgmSource.volume = isMuted ? 0f : bgmVolume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        pendingBGM = null;

        if (bgmSource != null)
            bgmSource.Stop();
    }

    public void PlaySFX(SFXId id, float volumeScale = 1f)
    {
        if (library == null || id == SFXId.None || isMuted)
            return;

        if (!audioUnlocked)
            return;

        AudioClip clip = library.GetSFX(id);
        if (clip == null)
            return;

        AudioSource source = sfxPool[sfxPoolIndex];
        sfxPoolIndex = (sfxPoolIndex + 1) % sfxPool.Length;

        source.clip = clip;
        source.volume = Mathf.Clamp01(sfxVolume * volumeScale);
        source.Play();
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(PrefBGMVolume, bgmVolume);
        ApplyVolume();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(PrefSFXVolume, sfxVolume);
    }

    public void SetMuted(bool muted)
    {
        isMuted = muted;
        PlayerPrefs.SetInt(PrefMuted, isMuted ? 1 : 0);
        ApplyVolume();
    }

    public void ToggleMute()
    {
        SetMuted(!isMuted);
    }

    void ApplyVolume()
    {
        if (bgmSource != null)
            bgmSource.volume = isMuted ? 0f : bgmVolume;
    }
}