using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// Command-line pipeline:
///   Unity -batchmode -quit -projectPath AisleSeven -executeMethod BuildScript.Setup
///   Unity -batchmode -quit -projectPath AisleSeven -buildTarget WebGL -executeMethod BuildScript.BuildWebGL
///   Unity -batchmode -quit -projectPath AisleSeven -buildTarget Win64 -executeMethod BuildScript.BuildWindows
public static class BuildScript
{
    const string ScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("Aisle Seven/Setup Project")]
    public static void Setup()
    {
        Directory.CreateDirectory("Assets/Resources/Mats");
        Directory.CreateDirectory("Assets/Scenes");

        MakeMat("Lit", Shader.Find("Standard"), m => m.SetFloat("_Glossiness", 0.2f));
        MakeMat("Glow", Shader.Find("Standard"), m =>
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.white);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        });
        MakeMat("Unlit", Shader.Find("Unlit/Color"), m => { });
        MakeMat("Glass", Shader.Find("Standard"), m =>
        {
            m.SetFloat("_Mode", 3f);
            m.SetInt("_SrcBlend", (int)BlendMode.One);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.DisableKeyword("_ALPHABLEND_ON");
            m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 3000;
            m.color = new Color(0.25f, 0.35f, 0.4f, 0.22f);
            m.SetFloat("_Glossiness", 0.9f);
        });
        AssetDatabase.SaveAssets();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.024f;
        RenderSettings.fogColor = Color.black;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.07f, 0.08f, 0.085f);
        RenderSettings.skybox = null;
        new GameObject("Bootstrap").AddComponent<GameBootstrap>();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        PlayerSettings.companyName = "Night Shift Games";
        PlayerSettings.productName = "Aisle Seven";
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.runInBackground = true;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.SplashScreen.showUnityLogo = false;
        PlayerSettings.SplashScreen.show = false;
        AssetDatabase.SaveAssets();
        Debug.Log("[AisleSeven] Setup complete.");
    }

    static void MakeMat(string name, Shader shader, System.Action<Material> cfg)
    {
        string path = "Assets/Resources/Mats/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
        m.shader = shader;
        cfg(m);
        EditorUtility.SetDirty(m);
    }

    public static void BuildWebGL()
    {
        bool debug = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-debugExceptions") >= 0;
        PlayerSettings.WebGL.exceptionSupport = debug ? WebGLExceptionSupport.FullWithStacktrace : WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        Build(BuildTarget.WebGL, "../Builds/WebGL");
    }
    public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "../Builds/Windows/AisleSeven.exe");

    static void Build(BuildTarget target, string path)
    {
        var opts = new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = path, target = target, options = BuildOptions.None };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("[AisleSeven] Build " + target + ": " + report.summary.result + " (" + report.summary.totalSize / (1024 * 1024) + " MB)");
        if (report.summary.result != BuildResult.Succeeded) { EditorApplication.Exit(1); return; }
        if (target == BuildTarget.WebGL) PatchWebGLIndex(Path.Combine(path, "index.html"));
    }

    /// Some embeds (sandboxed iframes, previews) reject pointer lock; Unity's loader would treat the
    /// rejected promise as a fatal error. Swallow just that one so the game keeps running.
    static void PatchWebGLIndex(string file)
    {
        if (!File.Exists(file)) return;
        string html = File.ReadAllText(file);
        const string marker = "<!-- aisle7-pointerlock-guard -->";
        if (html.Contains(marker)) return;
        string guard = marker + "\n<script>\n" +
            "window.addEventListener('unhandledrejection', function (e) {\n" +
            "  var r = String(e && e.reason);\n" +
            "  if (r.indexOf('pointer lock') >= 0 || r.indexOf('PointerLock') >= 0 || r.indexOf('WrongDocumentError') >= 0) { e.preventDefault(); e.stopImmediatePropagation(); }\n" +
            "}, true);\n</script>\n";
        int i = html.IndexOf("<head>");
        html = i >= 0 ? html.Insert(i + 6, "\n" + guard) : guard + html;
        // Render at CSS-pixel resolution: retina/HiDPI 2x rendering quadruples fill cost for no gameplay benefit.
        int c = html.IndexOf("var config = {");
        if (c >= 0)
        {
            int end = html.IndexOf("};", c);
            if (end > 0) html = html.Insert(end + 2, "\n      config.devicePixelRatio = 1;");
        }
        File.WriteAllText(file, html);
        Debug.Log("[AisleSeven] Patched index.html with pointer-lock guard.");
    }
}
