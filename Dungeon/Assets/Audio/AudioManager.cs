using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource uiSource;

    [Header("UI")]
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip hoverClip;

    [Header("Music")]
    [SerializeField] private AudioClip initialMusic; // asigna tu clip en el inspector
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f; // opcional
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    private void Start()
    {
        if (initialMusic != null)
        {
            musicSource.volume = musicVolume;
            PlayMusic(initialMusic);
        }
    }
    public void PlayMusic(AudioClip music)
    {
        if (music == null) return;

        musicSource.clip = music;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlayUIClick()
    {
        uiSource.PlayOneShot(clickClip);
    }

    public void PlayUIHover()
    {
        uiSource.PlayOneShot(hoverClip);
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }
}