using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PokerOnline.EditorTools
{
    /// <summary>
    /// Сборка клиента из командной строки (используется CI и локально).
    /// Пример:
    ///   Unity.exe -batchmode -quit -projectPath client/PokerOnline \
    ///             -executeMethod PokerOnline.EditorTools.BuildScript.BuildWindows
    /// </summary>
    public static class BuildScript
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Poker/Create Main Scene")]
        public static void CreateMainScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log($"[Poker] Сцена создана: {ScenePath}");
        }

        [MenuItem("Poker/Build/Windows x64")]
        public static void BuildWindows()
        {
            ConfigurePlayer();
            CreateMainScene();

            string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));
            string outputDir = Path.Combine(repoRoot, "build", "windows");
            Directory.CreateDirectory(outputDir);
            string exePath = Path.Combine(outputDir, "PokerOnline.exe");

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[Poker] Сборка Windows не удалась: {report.summary.result}");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"[Poker] Сборка Windows готова: {exePath} " +
                      $"(размер {report.summary.totalSize} байт)");
            EditorApplication.Exit(0);
        }

        [MenuItem("Poker/Build/Android APK")]
        public static void BuildAndroid()
        {
            ConfigurePlayer();
            CreateMainScene();

            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.Android, "com.kapcha0.pokeronline");
            PlayerSettings.Android.bundleVersionCode = 1;

            string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));
            string outputDir = Path.Combine(repoRoot, "build", "android");
            Directory.CreateDirectory(outputDir);
            string apkPath = Path.Combine(outputDir, "PokerOnline.apk");

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[Poker] Сборка Android не удалась: {report.summary.result}");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"[Poker] APK готов: {apkPath}");
            EditorApplication.Exit(0);
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "kapcha0";
            PlayerSettings.productName = "Poker Online";
            PlayerSettings.defaultScreenWidth = 900;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
        }
    }
}
