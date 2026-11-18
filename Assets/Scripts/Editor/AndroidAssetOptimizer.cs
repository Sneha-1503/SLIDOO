using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class AndroidAssetOptimizer : EditorWindow
{
    [Serializable]
    private class BackupEntry
    {
        public string guid;
        public string path;
        public string meta;
    }

    [Serializable]
    private class Backup
    {
        public List<BackupEntry> entries = new List<BackupEntry>();
    }

    private class AssetRow
    {
        public string path;
        public long bytes;
        public bool selected;
        public bool texture;
    }

    private readonly List<AssetRow> rows = new List<AssetRow>();
    private Vector2 scroll;
    private int maxTextureSize = 1024;
    private bool optimizeAudio = true;
    private float audioQuality = .65f;
    private TextureImporterFormat format = TextureImporterFormat.ASTC_6x6;
    private string status = "Scan assets to preview Android optimization candidates.";
    private const string BackupFolder = "AndroidAssetBackups";

    [MenuItem("Slidoo/Android Asset Optimizer")]
    public static void Open() => GetWindow<AndroidAssetOptimizer>("Android Assets");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Android Asset Optimizer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Android import overrides reduce built texture/audio data. Original PNG/WAV files and the Library cache still occupy project disk space. Measure the final result with an Android build.", MessageType.Info);
        maxTextureSize = EditorGUILayout.IntPopup("Maximum texture size", maxTextureSize,
            new[] { "512", "1024", "2048" }, new[] { 512, 1024, 2048 });
        format = (TextureImporterFormat)EditorGUILayout.EnumPopup("Texture format", format);
        optimizeAudio = EditorGUILayout.Toggle("Compress audio (Vorbis)", optimizeAudio);
        audioQuality = EditorGUILayout.Slider("Audio quality", audioQuality, .3f, 1f);
        EditorGUILayout.HelpBox("Default selection covers game UI, puzzle textures and audio. Font atlases and other assets require explicit selection. Sprite slicing, alpha, readability and audio loading behavior are preserved. Inspect UI text and puzzle detail after compression.", MessageType.None);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Scan / Refresh")) Scan();
            if (GUILayout.Button("Select game assets"))
                foreach (var row in rows) row.selected = IsGameAsset(row.path);
            if (GUILayout.Button("Clear selection"))
                foreach (var row in rows) row.selected = false;
        }
        EditorGUILayout.LabelField(status, EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField($"Selected {rows.Count(row => row.selected)} / {rows.Count} assets");
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var row in rows)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                row.selected = EditorGUILayout.Toggle(row.selected, GUILayout.Width(18));
                EditorGUILayout.LabelField($"{row.bytes / 1048576f:F2} MB", GUILayout.Width(70));
                if (GUILayout.Button(row.path, EditorStyles.linkLabel))
                    EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(row.path));
            }
        }
        EditorGUILayout.EndScrollView();
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || !rows.Any(row => row.selected)))
            if (GUILayout.Button("Apply selected Android overrides (creates backup)")) Apply();
        if (GUILayout.Button("Restore from backup…")) Restore();
        if (GUILayout.Button("Export source-size report…")) Export();
    }

    private static bool IsGameAsset(string path) =>
        path.StartsWith("Assets/UI/", StringComparison.Ordinal) ||
        path.StartsWith("Assets/Resources/Puzzles/", StringComparison.Ordinal) ||
        AssetImporter.GetAtPath(path) is AudioImporter;

    private void Scan()
    {
        rows.Clear();
        long sourceBytes = 0;
        foreach (var path in AssetDatabase.GetAllAssetPaths())
        {
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || !File.Exists(path)) continue;
            long bytes = new FileInfo(path).Length;
            sourceBytes += bytes;
            var importer = AssetImporter.GetAtPath(path);
            if (!(importer is TextureImporter) && !(importer is AudioImporter)) continue;
            rows.Add(new AssetRow { path = path, bytes = bytes,
                texture = importer is TextureImporter, selected = IsGameAsset(path) });
        }
        rows.Sort((a, b) => b.bytes.CompareTo(a.bytes));
        status = $"Assets source files: {sourceBytes / 1048576f:F1} MB. Listed textures/audio: {rows.Sum(row => row.bytes) / 1048576f:F1} MB. These are source sizes, not APK estimates.";
    }

    private void Apply()
    {
        if (format != TextureImporterFormat.ASTC_4x4 &&
            format != TextureImporterFormat.ASTC_6x6 &&
            format != TextureImporterFormat.ASTC_8x8 &&
            format != TextureImporterFormat.ETC2_RGBA8)
        {
            EditorUtility.DisplayDialog("Choose a mobile format", "Use ASTC 4x4, 6x6, 8x8 or ETC2 RGBA8. ETC2 is available for devices without ASTC support.", "OK");
            return;
        }
        var selected = rows.Where(row => row.selected && (row.texture || optimizeAudio)).ToArray();
        if (selected.Length == 0) return;
        var backup = new Backup();
        foreach (var row in selected)
            backup.entries.Add(new BackupEntry { guid = AssetDatabase.AssetPathToGUID(row.path),
                path = row.path, meta = File.ReadAllText(row.path + ".meta") });
        Directory.CreateDirectory(BackupFolder);
        string backupPath = Path.Combine(BackupFolder, DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".json");
        File.WriteAllText(backupPath, JsonUtility.ToJson(backup, true));
        int applied = 0;
        try
        {
            foreach (var row in selected)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Android optimization", row.path, (float)applied / selected.Length)) break;
                if (AssetImporter.GetAtPath(row.path) is TextureImporter texture)
                {
                    var settings = texture.GetPlatformTextureSettings("Android");
                    int currentLimit = settings.overridden ? settings.maxTextureSize : texture.maxTextureSize;
                    settings.name = "Android";
                    settings.overridden = true;
                    settings.maxTextureSize = Mathf.Min(maxTextureSize, currentLimit);
                    settings.format = format;
                    settings.textureCompression = TextureImporterCompression.Compressed;
                    settings.crunchedCompression = false;
                    texture.SetPlatformTextureSettings(settings);
                    texture.SaveAndReimport();
                }
                else if (AssetImporter.GetAtPath(row.path) is AudioImporter audio)
                {
                    var settings = audio.ContainsSampleSettingsOverride("Android")
                        ? audio.GetOverrideSampleSettings("Android") : audio.defaultSampleSettings;
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    settings.quality = audioQuality;
                    if (!audio.SetOverrideSampleSettings("Android", settings))
                        throw new InvalidOperationException("Audio override rejected: " + row.path);
                    audio.SaveAndReimport();
                }
                applied++;
            }
            status = $"Applied {applied}/{selected.Length}. Backup: {backupPath}. Build Android to compare package size.";
        }
        catch (Exception exception)
        {
            status = $"Stopped after {applied} assets. Restore backup {backupPath} if needed. {exception.Message}";
            Debug.LogException(exception);
        }
        finally { EditorUtility.ClearProgressBar(); }
    }

    private void Restore()
    {
        string path = EditorUtility.OpenFilePanel("Restore Android optimization backup", BackupFolder, "json");
        if (string.IsNullOrEmpty(path)) return;
        var backup = JsonUtility.FromJson<Backup>(File.ReadAllText(path));
        if (backup == null || backup.entries == null) return;
        if (!EditorUtility.DisplayDialog("Restore importer settings",
            "This restores all import settings recorded in this backup, including any later importer edits. Source images and audio are unchanged.", "Restore", "Cancel")) return;
        int restored = 0;
        foreach (var entry in backup.entries)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(entry.guid);
            if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal) || !File.Exists(assetPath + ".meta")) continue;
            File.WriteAllText(assetPath + ".meta", entry.meta);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            restored++;
        }
        status = $"Restored {restored} asset import settings.";
    }

    private void Export()
    {
        string path = EditorUtility.SaveFilePanel("Export asset report", "", "android-assets.csv", "csv");
        if (string.IsNullOrEmpty(path)) return;
        var lines = new List<string> { "Asset,SourceBytes,Selected" };
        string quote = ((char)34).ToString();
        lines.AddRange(rows.Select(row => quote + row.path.Replace(quote, quote + quote)
            + quote + "," + row.bytes + "," + row.selected));
        File.WriteAllLines(path, lines);
    }
}
