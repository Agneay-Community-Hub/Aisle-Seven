using System.Collections;
using System.IO;
using UnityEngine;

/// The only object in the scene. Builds the whole game at runtime.
public class GameBootstrap : MonoBehaviour
{
    void Awake()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // Forward rendering draws every object once per pixel light; keep that small in the browser.
        // Remaining lights fall back to cheap per-vertex lighting.
        QualitySettings.pixelLightCount = 3;
        QualitySettings.antiAliasing = 0;
#else
        QualitySettings.pixelLightCount = 6;
        QualitySettings.antiAliasing = 2;
#endif
        Time.maximumDeltaTime = 0.1f;
        QualitySettings.shadows = ShadowQuality.Disable;
        Application.targetFrameRate = 60;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.024f;
        RenderSettings.fogColor = Color.black;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.07f, 0.08f, 0.085f);

        new GameObject("Sfx").AddComponent<Sfx>();
        var store = new GameObject("Store").AddComponent<Store>();
        store.Build();
        new GameObject("Aisle7").AddComponent<Aisle7>();
        new GameObject("Player").AddComponent<PlayerController>();
        var gmGo = new GameObject("Game");
        gmGo.AddComponent<GameManager>().Init(store);
        gmGo.AddComponent<HUD>();

        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-autoshot") StartCoroutine(AutoShot(args[i + 1]));
            if (args[i] == "-autoplay") StartCoroutine(AutoPlay(args[i + 1]));
            if (args[i] == "-trailer") gameObject.AddComponent<TrailerDirector>().Begin(args[i + 1]);
        }
    }

    /// Plays the real loop hands-off for a while (customers spawn & walk naturally), with screenshots.
    IEnumerator AutoPlay(string dir)
    {
        Directory.CreateDirectory(dir);
        yield return new WaitForSeconds(1f);
        GameManager.I.DebugStart();
        for (int i = 0; i < 6; i++)
        {
            yield return new WaitForSeconds(6f);
            var c = GameManager.I.current;
            Debug.Log("[AutoPlay] t=" + GameManager.I.clock.ToString("0.0") + " current=" + (c == null ? "none" : c.state + " @ " + c.transform.position));
            Shot(dir, "play_" + i);
        }
        Application.Quit();
    }

    /// Automated screenshot tour, used to verify builds from the command line:
    ///   AisleSeven.exe -autoshot <folder>
    IEnumerator AutoShot(string dir)
    {
        Directory.CreateDirectory(dir);
        var gm = GameManager.I; var p = PlayerController.I;
        yield return new WaitForSeconds(2.5f);
        Shot(dir, "01_title");
        yield return new WaitForSeconds(0.4f);
        gm.state = GState.Intro;
        yield return new WaitForSeconds(0.6f);
        Shot(dir, "02_intro");
        yield return new WaitForSeconds(0.4f);
        gm.DebugStart();
        yield return new WaitForSeconds(1.0f);
        p.SetLook(15f, 5f);
        yield return new WaitForSeconds(0.6f);
        Shot(dir, "03_store");
        yield return new WaitForSeconds(0.4f);

        gm.DebugSetClock(GameManager.HourLength * 4 + 5f);   // 3 AM, rules up to 8
        gm.DebugCustomer(CustomerFactory.WithEyes(5));
        p.SetLook(0f, 6f);
        yield return new WaitForSeconds(1.5f);
        gm.DebugOpenTransaction();
        yield return new WaitForSeconds(0.8f);
        Shot(dir, "04_customer");
        yield return new WaitForSeconds(0.4f);

        gm.ToggleHandbook();
        yield return new WaitForSeconds(0.5f);
        Shot(dir, "05_handbook");
        yield return new WaitForSeconds(0.3f);
        gm.ToggleHandbook();
        gm.TransactionOpen = false;

        p.Teleport(new Vector3(0, 0, 14f), 0f);
        p.SetLook(0f, 0f);
        yield return new WaitForSeconds(1.0f);
        Shot(dir, "06_toward_aisle7");
        yield return new WaitForSeconds(0.4f);

        p.Teleport(new Vector3(0, 0, 23f), 0f);
        yield return new WaitForSeconds(2.0f);
        Aisle7.I.DebugEntityAt(36f);
        yield return new WaitForSeconds(0.4f);
        Shot(dir, "07_aisle7");
        yield return new WaitForSeconds(0.4f);
        p.FlashOn = true;
        Aisle7.I.ForceBlackout(3f);
        yield return new WaitForSeconds(0.4f);
        Shot(dir, "08_aisle7_blackout");
        yield return new WaitForSeconds(0.4f);

        Aisle7.I.PlaceForScare(p.Cam.transform);
        yield return new WaitForSeconds(0.3f);
        Shot(dir, "09_scare");
        yield return new WaitForSeconds(0.3f);
        Aisle7.I.ResetAfterCatch();
        p.FlashOn = false;

        p.Teleport(Store.PlayerStart, 180f);
        Store.I.DebugFigureAt(-4f);
        yield return new WaitForSeconds(1.2f);
        Shot(dir, "10_window");
        yield return new WaitForSeconds(0.5f);
        Application.Quit();
    }

    static void Shot(string dir, string name) => ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"));
}
