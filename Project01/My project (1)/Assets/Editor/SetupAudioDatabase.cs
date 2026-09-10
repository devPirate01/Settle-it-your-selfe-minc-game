using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SetupAudioDatabase
{
    static SetupAudioDatabase()
    {
        EditorApplication.delayCall += EnsureAssetExists;
    }

    [MenuItem("Tools/Generate Audio Database Asset")]
    public static void EnsureAssetExists()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        var db = AssetDatabase.LoadAssetAtPath<AudioDatabase>("Assets/Resources/AudioDatabase.asset");
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<AudioDatabase>();
            AssetDatabase.CreateAsset(db, "Assets/Resources/AudioDatabase.asset");
            AssetDatabase.SaveAssets();
            Debug.Log("[Audio] Generated Assets/Resources/AudioDatabase.asset");
        }

        bool modified = false;

        AudioClip LoadClip(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/AudioFiles/" + fileName);
        }

        // Music Tracks
        if (db.battleMusic == null)
        {
            var c = LoadClip("Music_Normal.wav");
            if (c != null) { db.battleMusic = c; modified = true; }
        }
        if (db.suddenDeathMusic == null)
        {
            var c = LoadClip("Music_Sudden_Death.wav");
            if (c != null) { db.suddenDeathMusic = c; modified = true; }
        }

        // Combat SFX
        if (db.propHitClips == null || db.propHitClips.Length == 0)
        {
            var c = LoadClip("Hit_01.wav");
            if (c != null) { db.propHitClips = new AudioClip[] { c }; modified = true; }
        }
        if (db.playerDeathClip == null)
        {
            var c = LoadClip("Death_1.wav");
            if (c == null) c = LoadClip("Death_2.wav");
            if (c != null) { db.playerDeathClip = c; modified = true; }
        }

        // Corporate Slack Milestone Notifications
        if (db.sound_100Percent_MatchStart == null)
        {
            var c = LoadClip("Slack_100pct_05m00s_MatchStart.wav");
            if (c != null) { db.sound_100Percent_MatchStart = c; modified = true; }
        }
        if (db.sound_75Percent_Q4Warning == null)
        {
            var c = LoadClip("Slack_075pct_03m45s_Q4Warning.wav");
            if (c != null) { db.sound_75Percent_Q4Warning = c; modified = true; }
        }
        if (db.sound_50Percent_Halfway == null)
        {
            var c = LoadClip("Slack_050pct_02m30s_Halfway.wav");
            if (c != null) { db.sound_50Percent_Halfway = c; modified = true; }
        }
        if (db.sound_25Percent_UrgentRush == null)
        {
            var c = LoadClip("Slack_025pct_01m15s_UrgentRush.wav");
            if (c != null) { db.sound_25Percent_UrgentRush = c; modified = true; }
        }
        if (db.sound_0Percent_SuddenDeath == null)
        {
            var c = LoadClip("Slack_000pct_00m00s_SuddenDeath.wav");
            if (c != null) { db.sound_0Percent_SuddenDeath = c; modified = true; }
        }
        if (db.defaultSlackNotification == null)
        {
            var c = LoadClip("Notif_Normal_01.wav");
            if (c != null) { db.defaultSlackNotification = c; modified = true; }
        }

        if (modified)
        {
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log("[Audio] Automatically linked audio files from Assets/AudioFiles to AudioDatabase.asset!");
        }
    }
}
