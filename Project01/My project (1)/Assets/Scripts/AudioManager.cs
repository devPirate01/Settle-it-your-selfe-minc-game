using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<AudioManager>();
                if (_instance == null)
                {
                    var go = new GameObject("[AudioManager]");
                    _instance = go.AddComponent<AudioManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Audio Database Configuration")]
    [Tooltip("Reference to the central AudioDatabase ScriptableObject (auto-loads from Resources if empty)")]
    public AudioDatabase database;

    [Header("Internal Audio Channels")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource sfxSource;

    Coroutine musicFadeRoutine;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (database == null)
        {
            database = AudioDatabase.Instance;
        }

        // Setup dedicated audio sources if not assigned
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
    }

    AudioDatabase DB => database != null ? database : AudioDatabase.Instance;

    #region Music Playback & Crossfading

    public void PlayBattleMusic(float fadeDuration = 1.2f)
    {
        if (DB == null || DB.battleMusic == null) return;
        CrossfadeMusic(DB.battleMusic, DB.battleMusicVolume, fadeDuration);
    }

    public void PlaySuddenDeathMusic(float fadeDuration = 0.8f)
    {
        if (DB == null || DB.suddenDeathMusic == null) return;
        CrossfadeMusic(DB.suddenDeathMusic, DB.suddenDeathMusicVolume, fadeDuration);
    }

    public void StopMusic(float fadeDuration = 1.0f)
    {
        if (musicFadeRoutine != null) StopCoroutine(musicFadeRoutine);
        musicFadeRoutine = StartCoroutine(FadeOutMusicRoutine(fadeDuration));
    }

    void CrossfadeMusic(AudioClip newClip, float targetVolume, float duration)
    {
        if (musicFadeRoutine != null) StopCoroutine(musicFadeRoutine);
        musicFadeRoutine = StartCoroutine(CrossfadeMusicRoutine(newClip, targetVolume, duration));
    }

    IEnumerator CrossfadeMusicRoutine(AudioClip newClip, float targetVolume, float duration)
    {
        if (musicSource.isPlaying)
        {
            float startVol = musicSource.volume;
            float elapsed = 0f;
            while (elapsed < duration * 0.5f)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVol, 0f, elapsed / (duration * 0.5f));
                yield return null;
            }
        }

        musicSource.clip = newClip;
        musicSource.volume = 0f;
        musicSource.Play();

        float inElapsed = 0f;
        while (inElapsed < duration * 0.5f)
        {
            inElapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, inElapsed / (duration * 0.5f));
            yield return null;
        }

        musicSource.volume = targetVolume;
    }

    IEnumerator FadeOutMusicRoutine(float duration)
    {
        if (!musicSource.isPlaying) yield break;

        float startVol = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = startVol;
    }

    #endregion

    #region SFX Playback

    public void PlayPropHit(float hitForce = 1.0f)
    {
        if (DB == null || DB.propHitClips == null || DB.propHitClips.Length == 0) return;

        // Select random hit sound for variety
        var clip = DB.propHitClips[Random.Range(0, DB.propHitClips.Length)];
        if (clip == null) return;

        // Modulate pitch slightly for dynamic physical impact feel
        sfxSource.pitch = Random.Range(0.88f, 1.15f);
        float vol = Mathf.Clamp(DB.propHitVolume * Mathf.Clamp(hitForce, 0.7f, 1.4f), 0.1f, 1.0f);
        sfxSource.PlayOneShot(clip, vol);
    }

    public void PlayDeath()
    {
        if (DB == null || DB.playerDeathClip == null) return;

        sfxSource.pitch = 1.0f;
        sfxSource.PlayOneShot(DB.playerDeathClip, DB.playerDeathVolume);
    }

    public void PlaySlackPing()
    {
        if (DB == null || DB.slackPingClip == null) return;

        sfxSource.pitch = 1.0f;
        sfxSource.PlayOneShot(DB.slackPingClip, DB.slackPingVolume);
    }

    #endregion
}
