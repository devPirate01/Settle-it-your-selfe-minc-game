using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public static class SetupUIToolkit
{
    [MenuItem("Tools/Generate Panel Settings Asset")]
    public static void CreatePanelSettings()
    {
        string path = "Assets/UI/GamePanelSettings.asset";
        var existing = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
        if (existing != null)
        {
            Debug.Log("[UI Toolkit] PanelSettings already exists at " + path);
            Selection.activeObject = existing;
            return;
        }

        var settings = ScriptableObject.CreateInstance<PanelSettings>();
        settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        settings.referenceResolution = new Vector2Int(1920, 1080);
        settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        settings.match = 0.5f;

        AssetDatabase.CreateAsset(settings, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[UI Toolkit] Successfully created GamePanelSettings.asset at " + path);
        Selection.activeObject = settings;
    }
}
