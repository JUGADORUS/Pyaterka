using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Reproducible release build; reports stay outside the upload directory.
public static class PyaterkaWebGLBuild
{
    [Serializable] private class AssetEntry
    {
        public string path, type, format;
        public long sourceBytes, runtimeBytes;
        public int width, height;
        public float seconds;
    }
    [Serializable] private class AssetAudit { public AssetEntry[] assets; }
    [Serializable] private class PackedEntry { public string path; public ulong bytes; }
    [Serializable] private class SizeReport
    {
        public string result, unityVersion, output;
        public ulong bytes;
        public int warnings, errors;
        public PackedEntry[] assets;
    }

    [MenuItem("Pyaterka/Build WebGL Release")]
    public static void Build()
    {
        string output = Path.GetFullPath(Argument("-pyaterkaOutput", "Builds/YandexGames"));
        string reports = Path.GetFullPath(Argument("-pyaterkaReports", "LocalChecks/WebGLReports"));
        Directory.CreateDirectory(reports);
        try
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                throw new BuildFailedException("Start Unity with -buildTarget WebGL.");
#if !YandexGamesPlatform_yg
            throw new BuildFailedException("YandexGamesPlatform_yg is missing; refusing to build without the Yandex platform bridge.");
#endif
            var sdk = YG.InfoYG.Inst();
            if (sdk == null || sdk.Basic.platform == null || sdk.Basic.platform.nameFull != "YandexGamesPlatform")
                throw new BuildFailedException("The Yandex Games platform settings are missing.");
            Debug.Log("PYATERKA_SDK_PREFLIGHT: YandexGames; build callbacks=" +
                TypeCache.GetTypesDerivedFrom<IPreprocessBuildWithReport>().Count(t => t == typeof(YG.EditorScr.BuildModify.ProcessBuild)));
            Audit(Path.Combine(reports, "assets-before.json"));
            OptimizeAssets();
            Audit(Path.Combine(reports, "assets-after.json"));
            ConfigureRelease();
            AssetDatabase.SaveAssets();
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length != 2 || scenes[0] != "Assets/Scenes/Game.unity" || scenes[1] != "Assets/Scenes/Garage.unity")
                throw new BuildFailedException("Unexpected release scenes.");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes, target = BuildTarget.WebGL, locationPathName = output,
                options = BuildOptions.DetailedBuildReport
            });
            if (report.summary.result == BuildResult.Succeeded)
            {
                string indexPath = Path.Combine(output, "index.html");
                // Ensure direct BuildPipeline calls also produce the required SDK modules.
                if (!File.ReadAllText(indexPath).Contains("function InitPayments("))
                    YG.EditorScr.BuildModify.ModifyBuild.ModifyIndex(output);
                string html = File.ReadAllText(indexPath);
                if (!html.Contains("pyaterka_premium_car") || !html.Contains("PYATERKA_CLOUD_UNAVAILABLE"))
                    throw new BuildFailedException("Generated HTML is missing the Yandex payments/storage integration.");
            }
            var packed = report.packedAssets.SelectMany(a => a.contents)
                .Where(a => !string.IsNullOrEmpty(a.sourceAssetPath))
                .GroupBy(a => a.sourceAssetPath)
                .Select(g => new PackedEntry { path = g.Key, bytes = (ulong)g.Sum(a => (long)a.packedSize) })
                .OrderByDescending(a => a.bytes).ToArray();
            File.WriteAllText(Path.Combine(reports, "build-report.json"), JsonUtility.ToJson(new SizeReport
            {
                result = report.summary.result.ToString(), unityVersion = Application.unityVersion,
                output = output, bytes = report.summary.totalSize,
                warnings = report.summary.totalWarnings, errors = report.summary.totalErrors, assets = packed
            }, true));
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("WebGL build failed: " + report.summary.result);
            Debug.Log("PYATERKA_WEBGL_BUILD_SUCCEEDED " + output);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
    }

    private static void ConfigureRelease()
    {
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.connectProfiler = false;
        EditorUserBuildSettings.allowDebugging = false;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Medium);
        PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
        PlayerSettings.WebGL.template = "PROJECT:YandexGames";
        // The WebGL extension assembly is present only with WebGL Build Support installed.
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.WebGL.UserBuildSettings"))
            .FirstOrDefault(t => t != null);
        var property = type?.GetProperty("codeOptimization");
        if (property == null) throw new BuildFailedException("WebGL code optimization API is unavailable.");
        property.SetValue(null, Enum.Parse(property.PropertyType, "DiskSizeLTO"));
    }

    private static void OptimizeAssets()
    {
        // Large images are displayed as small props or HUD icons. Font atlases and UI labels are untouched.
        var limits = new Dictionary<string, int>
        {
            { "Assets/Materials/asphalt.jpg", 1024 },
            { "Assets/Materials/rostov.png", 512 },
            { "Assets/Materials/5opka.png", 512 },
            { "Assets/Materials/NoExit.png", 512 },
            { "Assets/Materials/damageVignette.png", 1024 },
            { "Assets/dollar.png", 256 },
            { "Assets/clock.png", 256 },
            { "Assets/heart.png", 256 }
        };
        foreach (var item in limits)
        {
            var importer = AssetImporter.GetAtPath(item.Key) as TextureImporter;
            if (importer == null) continue;
            var settings = importer.GetPlatformTextureSettings("WebGL");
            settings.name = "WebGL";
            settings.overridden = true;
            settings.maxTextureSize = item.Value;
            // Automatic keeps alpha and uses the platform's compatible texture format.
            settings.format = TextureImporterFormat.Automatic;
            settings.textureCompression = TextureImporterCompression.CompressedHQ;
            settings.compressionQuality = 75;
            importer.SetPlatformTextureSettings(settings);
            importer.SaveAndReimport();
        }
        foreach (string path in Directory.GetFiles("Assets/Sounds", "*.mp3").Select(p => p.Replace('\\', '/')))
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            var settings = importer.GetOverrideSampleSettings("WebGL");
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = path.EndsWith("racing_music_default.mp3", StringComparison.Ordinal) ? 0.45f : 0.65f;
            settings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
            settings.sampleRateOverride = 32000;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            // WebGL does not support all per-platform audio overrides in Unity 2022.
            if (!importer.SetOverrideSampleSettings("WebGL", settings))
                importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            Debug.Log("PYATERKA_AUDIO_OPTIMIZED " + path);
        }
    }

    private static void Audit(string output)
    {
        var paths = new HashSet<string>(AssetDatabase.GetDependencies(
            EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(), true));
        foreach (string path in AssetDatabase.GetAllAssetPaths())
            if (path.Contains("/Resources/") && !AssetDatabase.IsValidFolder(path)) paths.Add(path);
        var entries = new List<AssetEntry>();
        foreach (string path in paths)
        {
            if (!File.Exists(path)) continue;
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null) continue;
            var entry = new AssetEntry { path = path, type = asset.GetType().Name, sourceBytes = new FileInfo(path).Length };
            if (asset is Texture2D texture)
            {
                entry.width = texture.width; entry.height = texture.height; entry.format = texture.format.ToString();
                entry.runtimeBytes = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture);
            }
            if (asset is AudioClip audio) { entry.seconds = audio.length; entry.runtimeBytes = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(audio); }
            entries.Add(entry);
        }
        File.WriteAllText(output, JsonUtility.ToJson(new AssetAudit { assets = entries.OrderByDescending(a => a.runtimeBytes).ToArray() }, true));
    }

    private static string Argument(string key, string fallback)
    {
        var args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, key);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
    }
}
