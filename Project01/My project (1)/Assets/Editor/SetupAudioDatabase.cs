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
    }
}
