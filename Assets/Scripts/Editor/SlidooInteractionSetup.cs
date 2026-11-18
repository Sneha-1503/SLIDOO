#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Slidoo;

public static class SlidooInteractionSetup
{
    [InitializeOnLoadMethod]
    private static void Schedule() => EditorApplication.delayCall += Apply;

    [MenuItem("Slidoo/Update UI Interactions")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        foreach (var splash in Object.FindObjectsByType<SplashScreenUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RegisterFullObjectHierarchyUndo(splash.gameObject, "Add loading bar");
            splash.EnsureLoadingBar();
            var splashSettings = new SerializedObject(splash);
            var duration = splashSettings.FindProperty("duration");
            var hold = splashSettings.FindProperty("completedHoldDuration");
            var timingUpdated = splashSettings.FindProperty("timingUpdated");
            if (!timingUpdated.boolValue && duration.floatValue <= 2f)
            {
                duration.floatValue = 5f;
                hold.floatValue = 1f;
            }
            timingUpdated.boolValue = true;
            splashSettings.ApplyModifiedProperties();
            EditorUtility.SetDirty(splash);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(splash.gameObject.scene);
        }
        foreach (var settings in Object.FindObjectsByType<SettingsPopupUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RegisterFullObjectHierarchyUndo(settings.gameObject, "Fix toggle input");
            settings.ConfigureInputs();
            EditorUtility.SetDirty(settings);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(settings.gameObject.scene);
        }
        foreach (var gameplay in Object.FindObjectsByType<GameplayUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var area = gameplay.transform.Find("BoardArea");
            if (area == null) continue;
            var layout = area.GetComponent<PuzzleBoardLayout>();
            if (layout == null) layout = Undo.AddComponent<PuzzleBoardLayout>(area.gameObject);
            layout.Refresh();
        }
        foreach (var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (button.GetComponent<ModeButtonUI>() == null && button.GetComponent<LevelButtonUI>() == null) continue;
            if (button.GetComponent<ButtonPressFeedback>() == null)
                Undo.AddComponent<ButtonPressFeedback>(button.gameObject);
        }
    }
}
#endif
