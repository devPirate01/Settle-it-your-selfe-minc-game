using UnityEngine;

public enum SlackMilestone
{
    Start_100Pct,
    Quarter_75Pct,
    Halfway_50Pct,
    Urgent_25Pct,
    FinalSeconds_10Pct,
    SuddenDeath_0Pct
}

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

    [Header("3. Corporate Slack Notification Sounds (By Percentage / Time)")]
    [Tooltip("Default fallback notification sound if a specific milestone slot below is empty")]
    public AudioClip defaultSlackNotification;

    [Space(6)]
    [Tooltip("100% Round Time (Match Start - 5:00 mark)")]
    public AudioClip sound_100Percent_MatchStart;

    [Tooltip("75% Round Time (Three-Quarter Mark - 3:45 mark with 5m timer)")]
    public AudioClip sound_75Percent_Q4Warning;

    [Tooltip("50% Round Time (Halfway Mark - 2:30 mark with 5m timer)")]
    public AudioClip sound_50Percent_Halfway;

    [Tooltip("25% Round Time (Urgent Warning - 1:15 mark with 5m timer)")]
    public AudioClip sound_25Percent_UrgentRush;

    [Tooltip("10% Round Time (Final 30 Seconds Countdown - 0:30 mark with 5m timer)")]
    public AudioClip sound_10Percent_FinalSeconds;

    [Tooltip("0% Round Time (Sudden Death Trigger - 0:00 mark)")]
    public AudioClip sound_0Percent_SuddenDeath;

    [Space(6)]
    [Tooltip("Legacy fallback Slack ping clip")]
    public AudioClip slackPingClip;

    [Range(0f, 1f)] public float slackPingVolume = 0.85f;

    public AudioClip GetSlackClip(SlackMilestone milestone)
    {
        AudioClip clip = milestone switch
        {
            SlackMilestone.Start_100Pct => sound_100Percent_MatchStart,
            SlackMilestone.Quarter_75Pct => sound_75Percent_Q4Warning,
            SlackMilestone.Halfway_50Pct => sound_50Percent_Halfway,
            SlackMilestone.Urgent_25Pct => sound_25Percent_UrgentRush,
            SlackMilestone.FinalSeconds_10Pct => sound_10Percent_FinalSeconds != null ? sound_10Percent_FinalSeconds : sound_25Percent_UrgentRush,
            SlackMilestone.SuddenDeath_0Pct => sound_0Percent_SuddenDeath,
            _ => null
        };

        if (clip == null) clip = defaultSlackNotification != null ? defaultSlackNotification : slackPingClip;
        return clip;
    }

    public AudioClip GetSlackClipByPercentage(float pctRemaining)
    {
        if (pctRemaining >= 0.88f) return GetSlackClip(SlackMilestone.Start_100Pct);
        if (pctRemaining >= 0.63f) return GetSlackClip(SlackMilestone.Quarter_75Pct);
        if (pctRemaining >= 0.38f) return GetSlackClip(SlackMilestone.Halfway_50Pct);
        if (pctRemaining >= 0.18f) return GetSlackClip(SlackMilestone.Urgent_25Pct);
        if (pctRemaining > 0.001f) return GetSlackClip(SlackMilestone.FinalSeconds_10Pct);
        return GetSlackClip(SlackMilestone.SuddenDeath_0Pct);
    }
}
