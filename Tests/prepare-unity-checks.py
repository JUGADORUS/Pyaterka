from pathlib import Path
import shutil
import json
root = Path(__file__).resolve().parents[1]
target = root / "Temp/CodexValidationProject"
for folder in ["Assets/Editor", "Assets/Plugins", "Packages", "ProjectSettings"]:
    (target / folder).mkdir(parents=True, exist_ok=True)
# The checks now compile real source, including PluginYG, inside Unity.
for name in ["Pyaterka.Gameplay.dll", "Pyaterka.Gameplay.dll.meta", "Unity.TextMeshPro.dll", "Unity.TextMeshPro.dll.meta", "UnityEngine.UI.dll", "UnityEngine.UI.dll.meta"]:
    path = target / "Assets/Plugins" / name
    if path.exists(): path.unlink()
for name in ["Scripts", "PluginYourGames", "TextMesh Pro"]:
    shutil.copytree(root / "Assets" / name, target / "Assets" / name, dirs_exist_ok=True)
    meta = root / "Assets" / (name + ".meta")
    if meta.exists(): shutil.copy2(meta, target / "Assets" / meta.name)
shutil.copy2(root / "Tests/UnityRegressionRunner.cs", target / "Assets/UnityRegressionRunner.cs")
shutil.copy2(root / "ProjectSettings/ProjectVersion.txt", target / "ProjectSettings/ProjectVersion.txt")
dependencies = {"com.unity.modules." + n: "1.0.0" for n in ["audio", "physics", "particlesystem", "ui", "jsonserialize", "imgui", "unitywebrequest", "unitywebrequesttexture"]}
for name, version in [("com.unity.textmeshpro", "3.0.7"), ("com.unity.ugui", "1.0.0")]:
    dependencies[name] = "file:" + (root / "Library/PackageCache" / (name + "@" + version)).as_posix()
(target / "Packages/manifest.json").write_text(json.dumps({"dependencies":dependencies}))

flags = "YandexGamesPlatform_yg;PLUGIN_YG_2;TMP_YG2;Localization_yg;RewardedAdv_yg;Authorization_yg;Storage_yg;RedefinePlayerPrefs_yg;Payments_yg;Leaderboards_yg;InterstitialAdv_yg"
settings = (root / "ProjectSettings/ProjectSettings.asset").read_text()
settings = settings.replace("companyName: DefaultCompany", "companyName: CodexValidation").replace("productName: Pyaterka", "productName: PyaterkaRegressionChecks")
(target / "ProjectSettings/ProjectSettings.asset").write_text(settings)

(target / "Assets/Editor/RunChecks.cs").write_text('''using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class RunChecks
{
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Validation").AddComponent<UnityRegressionRunner>();
        EditorApplication.EnterPlaymode();
    }
}
''')
print("Prepared source-based PluginYG validation project")
