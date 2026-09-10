using UnityEngine;

[CreateAssetMenu(fileName = "AudioDatabase", menuName = "Game/Audio Database")]
public class AudioDatabase : ScriptableObject
{
    private static AudioDatabase _instance;
    public static AudioDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<AudioDatabase>("AudioDatabase");
                if (_instance == null)
                {
                    _instance = CreateInstance<AudioDatabase>();
                }
            }
            return _instance;
        }
    }

    [Header("1. Music Tracks")]
    [Tooltip("Plays during the main battle round (loops)")]
    public AudioClip battleMusic;
    [Range(0f, 1f)] public float battleMusicVolume = 0.7f;

    [Tooltip("Plays when Sudden Death begins (loops)")]
    public AudioClip suddenDeathMusic;
    [Range(0f, 1f)] public float suddenDeathMusicVolume = 0.85f;

    [Header("2. Combat SFX")]
    [Tooltip("Sounds played when an object hits a player (randomized for variety)")]
    public AudioClip[] propHitClips;
    [Range(0f, 1f)] public float propHitVolume = 0.9f;

    [Tooltip("Sound played when a player's HP reaches 0 (death stinger / thud)")]
    public AudioClip playerDeathClip;
    [Range(0f, 1f)] public float playerDeathVolume = 1.0f;

    [Header("3. Corporate Pacing & Notifications")]
    [Tooltip("Slack / Teams notification chime played at match pacing milestones")]
    public AudioClip slackPingClip;
    [Range(0f, 1f)] public float slackPingVolume = 0.85f;
}
