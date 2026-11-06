using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// Renders the YouTube trailer offline: a scripted camera tour through the real game scenes,
/// captured frame-by-frame at a fixed framerate, plus a soundtrack mixed from the game's own
/// synthesized sounds on the same timeline.  Run:  AisleSeven.exe -trailer <outputFolder>
public class TrailerDirector : MonoBehaviour
{
    public static bool Active;
    const int FPS = 30;
    const float Total = 66f;
    const int SR = 44100;

    string dir;
    float t;
    Customer cust;
    string custKind = "";
    Texture2D white;
    GUIStyle sCard, sBig, sTitle, sSub;
    float sc;

    struct Card { public float a, b; public string text; public int style; public Card(float a, float b, string t, int s = 0) { this.a = a; this.b = b; text = t; style = s; } }
    static readonly Card[] Cards =
    {
        new Card(0.6f, 3.2f, "Your first night on the graveyard shift."),
        new Card(4.6f, 8.6f, "PIK-N-GO #0407  ·  OPEN 24 HOURS"),
        new Card(9.6f, 14.2f, "The register is easy."),
        new Card(15.0f, 18.8f, "The handbook is not optional."),
        new Card(19.5f, 23.2f, "Serve every customer."),
        new Card(24.0f, 27.3f, "Count the eyes."),
        new Card(28.0f, 30.8f, "Always serve the regulars."),
        new Card(31.5f, 34.8f, "You are the only one working tonight."),
        new Card(36.6f, 39.3f, "Don't look at the windows."),
        new Card(40.0f, 42.8f, "Don't worry about aisle seven."),
        new Card(43.0f, 45.8f, "We don't have an aisle seven."),
        new Card(46.6f, 50.2f, "It only moves when it isn't seen."),
        new Card(50.6f, 53.8f, "Keep the lights on it."),
        new Card(54.2f, 56.9f, "NEVER  SHIFT  IN  AISLE  SEVEN.", 1),
    };

    public void Begin(string outDir) { dir = outDir; StartCoroutine(Run()); }

    IEnumerator Run()
    {
        Active = true;
        AudioListener.volume = 0f;
        white = new Texture2D(1, 1); white.SetPixel(0, 0, Color.white); white.Apply();
        Directory.CreateDirectory(Path.Combine(dir, "frames"));
        var gm = GameManager.I;
        gm.rulesHour = 7; gm.seenRulesHour = 7; gm.fade = 0; gm.fadeTarget = 0;
        Aisle7.I.Rebuild(36f);
        yield return null; yield return null;
        Debug.Log("[Trailer] Screen " + Screen.width + "x" + Screen.height);
        WriteAudio(Path.Combine(dir, "trailer.wav"));
        Time.captureFramerate = FPS;
        int frames = Mathf.RoundToInt(Total * FPS);
        for (int f = 0; f < frames; f++)
        {
            t = f / (float)FPS;
            Apply();
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, "frames", "f_" + f.ToString("00000") + ".png"));
            yield return null;
        }
        yield return null;
        Debug.Log("[Trailer] Done: " + frames + " frames");
        Application.Quit();
    }

    // ------------------------------------------------------------------ shots
    static float K(float a, float b, float x) => Mathf.Clamp01((x - a) / (b - a));
    static float Ease(float k) => k * k * (3f - 2f * k);

    void CamLook(Vector3 pos, Vector3 target)
    {
        var cam = PlayerController.I.Cam.transform;
        cam.position = pos;
        cam.rotation = Quaternion.LookRotation(target - pos);
    }

    void CamYaw(Vector3 pos, float yaw, float pitch)
    {
        var cam = PlayerController.I.Cam.transform;
        cam.position = pos;
        cam.rotation = Quaternion.Euler(pitch, yaw, 0);
    }

    void SetCustomer(string kind)
    {
        if (kind == custKind) return;
        custKind = kind;
        if (cust != null) Destroy(cust.gameObject);
        cust = null;
        if (kind == "") return;
        var s = CustomerFactory.TrailerSpec(kind);
        var go = new GameObject("TrailerCustomer");
        cust = go.AddComponent<Customer>();
        cust.Init(s);
        cust.PlaceAtCounter();
    }

    void Apply()
    {
        var gm = GameManager.I; var p = PlayerController.I; var a7 = Aisle7.I;
        Vector3 counterCam = new Vector3(-4f, 1.62f, 1.45f);
        Vector3 face = new Vector3(-4f, 1.76f, 4.25f);
        gm.HandbookOpen = false; gm.redFlash = 0; gm.staticAmt = 0;
        p.FlashOn = false; a7.Proximity = 0;
        RenderSettings.fogDensity = 0.03f;

        if (t < 3.5f) { CamYaw(counterCam, 0, 0); SetCustomer(""); }
        else if (t < 9f)            // exterior dolly toward the lit storefront
        {
            SetCustomer("normal");
            float k = Ease(K(3.5f, 9f, t));
            CamLook(Vector3.Lerp(new Vector3(3f, 1.7f, -15f), new Vector3(0.5f, 1.65f, -3.2f), k), new Vector3(-1.5f, 1.4f, 6f));
            RenderSettings.fogDensity = 0.025f;
        }
        else if (t < 14.5f)         // slow pan from behind the counter
        {
            SetCustomer("");
            float k = Ease(K(9f, 14.5f, t));
            CamYaw(counterCam, Mathf.Lerp(-38f, 32f, k), 4f);
        }
        else if (t < 19f)           // the handbook
        {
            CamYaw(counterCam, 2f, 6f);
            gm.HandbookOpen = true;
        }
        else if (t < 23.5f)         // ordinary customer
        {
            SetCustomer("normal");
            float k = Ease(K(19f, 23.5f, t));
            CamLook(Vector3.Lerp(counterCam, new Vector3(-4f, 1.62f, 2.1f), k), face + Vector3.down * 0.25f);
        }
        else if (t < 27.5f)         // five eyes
        {
            SetCustomer("eyes");
            float k = Ease(K(23.5f, 27.5f, t));
            CamLook(Vector3.Lerp(new Vector3(-4f, 1.66f, 2.3f), new Vector3(-4f, 1.74f, 3.45f), k), face);
        }
        else if (t < 31f)           // faceless regular
        {
            SetCustomer("faceless");
            float k = Ease(K(27.5f, 31f, t));
            CamLook(Vector3.Lerp(new Vector3(-4.6f, 1.6f, 2.2f), new Vector3(-4.15f, 1.72f, 3.35f), k), face);
        }
        else if (t < 35f)           // uniform impostor: name tag, then face
        {
            SetCustomer("uniform");
            float k = Ease(K(31f, 35f, t));
            Vector3 tag = new Vector3(-4.12f, 1.32f, 4.25f);
            CamLook(Vector3.Lerp(new Vector3(-3.7f, 1.45f, 2.9f), new Vector3(-4f, 1.7f, 3.2f), k), Vector3.Lerp(tag, face, k));
        }
        else if (t < 39.5f)         // turn around: someone at the window
        {
            SetCustomer("");
            Store.I.ShowFigureFor(-4.6f);
            float k = Ease(K(35.3f, 38.6f, t));
            CamYaw(counterCam, Mathf.Lerp(0f, 178f, k), Mathf.Lerp(3f, 1f, k));
            if (t > 38.9f && t < 39.0f) Store.I.Flicker(0.6f);
        }
        else if (t < 46f)           // down aisle four toward aisle seven
        {
            Store.I.HideFigure();
            float k = Ease(K(39.5f, 46f, t));
            CamYaw(new Vector3(0f, 1.62f, Mathf.Lerp(6.5f, 18.6f, k)), 0f, Mathf.Lerp(-2f, 2f, k));
        }
        else if (t < 54f)           // the corridor: it gets closer every blackout
        {
            RenderSettings.fogDensity = 0.05f;
            float k = K(46f, 54f, t);
            CamYaw(new Vector3(0f, 1.62f, Mathf.Lerp(21.5f, 23.8f, k)), 0f, 1.5f);
            float[] outs = { 47.6f, 49.6f, 51.1f, 52.3f, 53.3f };
            float[] zs = { 54.5f, 47f, 40f, 34f, 29.5f, 27f };
            bool dark = false; int stage = 0;
            foreach (float o in outs) { if (t >= o) stage++; if (t >= o && t < o + 0.42f) dark = true; }
            a7.SetLightsDirect(!dark);
            a7.DebugEntityAt(zs[stage]);
            a7.Proximity = Mathf.Clamp01(1f - (zs[stage] - 23.8f - 1.2f) / 12f);
            RenderSettings.ambientLight = dark ? new Color(0.004f, 0.004f, 0.005f) : GameManager.AmbientBase;
        }
        else if (t < 57f)           // flashlight on: it's right there
        {
            RenderSettings.fogDensity = 0.05f;
            a7.SetLightsDirect(false);
            RenderSettings.ambientLight = new Color(0.004f, 0.004f, 0.005f);
            CamYaw(new Vector3(0f, 1.62f, 23.8f), 0f, 1.5f);
            a7.DebugEntityAt(26.4f);
            p.FlashOn = true;
            p.Flash.intensity = (Mathf.Repeat(t * 7.3f, 1f) < 0.12f) ? 0.4f : 2.6f;
            a7.Proximity = 0.85f;
        }
        else if (t < 58.3f)         // jumpscare
        {
            a7.SetLightsDirect(false);
            p.FlashOn = true;
            CamYaw(new Vector3(0f, 1.62f, 23.8f), 0f, 0f);
            a7.PlaceForScare(p.Cam.transform);
            float k = K(57f, 58.3f, t);
            gm.staticAmt = 1f - k * 0.5f; gm.redFlash = 1f - k * 0.7f;
            a7.Proximity = 1f;
        }
        else
        {
            a7.ResetAfterCatch();
            RenderSettings.ambientLight = GameManager.AmbientBase;
            CamYaw(counterCam, 0, 0);
        }
    }

    // ------------------------------------------------------------------ overlay
    void Styles()
    {
        float s = Screen.height / 1080f;
        if (Mathf.Approximately(s, sc)) return;
        sc = s;
        GUIStyle Mk(int size, FontStyle f) { var g = new GUIStyle(GUI.skin.label) { fontSize = (int)(size * s), alignment = TextAnchor.MiddleCenter, fontStyle = f, wordWrap = true }; return g; }
        sCard = Mk(46, FontStyle.Normal); sBig = Mk(72, FontStyle.Bold); sTitle = Mk(170, FontStyle.Bold); sSub = Mk(38, FontStyle.Normal);
    }

    void Txt(Rect r, string s, GUIStyle st, Color c)
    {
        st.normal.textColor = new Color(0, 0, 0, c.a * 0.9f);
        GUI.Label(new Rect(r.x + 3 * sc, r.y + 3 * sc, r.width, r.height), s, st);
        st.normal.textColor = c;
        GUI.Label(r, s, st);
    }

    void Black(float a) { if (a <= 0) return; GUI.color = new Color(0, 0, 0, a); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), white); GUI.color = Color.white; }

    void OnGUI()
    {
        if (!Active) return;
        GUI.depth = -10;
        Styles();
        float W = Screen.width, H = Screen.height;

        // letterbox bars for a cinematic frame
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0, 0, W, H * 0.075f), white);
        GUI.DrawTexture(new Rect(0, H * 0.925f, W, H * 0.075f), white);
        GUI.color = Color.white;

        // black between acts and dips on hard cuts
        if (t < 3.5f) Black(1f - K(3.0f, 3.5f, t));
        float[] cuts = { 9f, 14.5f, 19f, 23.5f, 27.5f, 31f, 35f, 39.5f, 46f };
        foreach (float c in cuts) { float d = Mathf.Abs(t - c); if (d < 0.18f) Black(1f - d / 0.18f); }
        if (t >= 58.3f) Black(1f);

        foreach (var c in Cards)
        {
            if (t < c.a || t > c.b) continue;
            float a = Mathf.Min(K(c.a, c.a + 0.35f, t), 1f - K(c.b - 0.35f, c.b, t));
            if (c.style == 1)
            {
                float flick = Mathf.Repeat(t * 11f, 1f) < 0.08f ? 0.3f : 1f;
                Txt(new Rect(0, H * 0.42f, W, H * 0.16f), c.text, sBig, new Color(1f, 0.22f, 0.18f, a * flick));
            }
            else Txt(new Rect(W * 0.1f, H * 0.78f, W * 0.8f, H * 0.12f), c.text, sCard, new Color(0.93f, 0.95f, 0.92f, a));
        }

        if (t >= 59.2f)
        {
            float a = K(59.2f, 59.5f, t) * (1f - K(65.3f, 66f, t));
            float flick = (t < 60.2f && Mathf.Repeat(t * 9f, 1f) < 0.3f) ? 0.25f : 1f;
            Txt(new Rect(0, H * 0.28f, W, H * 0.2f), "AISLE SEVEN", sTitle, new Color(0.95f, 0.22f, 0.18f, a * flick));
            float a2 = K(60.8f, 61.3f, t) * (1f - K(65.3f, 66f, t));
            Txt(new Rect(0, H * 0.49f, W, H * 0.07f), "a night-shift horror game", sSub, new Color(0.85f, 0.88f, 0.85f, a2));
            float a3 = K(62.2f, 62.7f, t) * (1f - K(65.3f, 66f, t));
            Txt(new Rect(0, H * 0.62f, W, H * 0.07f), "GitHub Game Off 2026  ·  theme: SHIFT", sSub, new Color(0.7f, 0.72f, 0.7f, a3));
            Txt(new Rect(0, H * 0.69f, W, H * 0.07f), "Play free on itch.io", sSub, new Color(1f, 0.85f, 0.45f, a3));
        }
    }

    // ------------------------------------------------------------------ soundtrack
    float[] buf;

    void Mix(AudioClip c, float at, float vol, float pitch = 1f)
    {
        var d = new float[c.samples * c.channels];
        c.GetData(d, 0);
        int start = (int)(at * SR);
        for (int i = 0; ; i++)
        {
            float src = i * pitch;
            int s0 = (int)src;
            if (s0 + 1 >= d.Length) break;
            int o = start + i;
            if (o < 0) continue;
            if (o >= buf.Length) break;
            float fr = src - s0;
            buf[o] += (d[s0] * (1 - fr) + d[s0 + 1] * fr) * vol;
        }
    }

    void Bed(AudioClip c, float a, float b, Func<float, float> vol)
    {
        var d = new float[c.samples]; c.GetData(d, 0);
        int s = (int)(a * SR), e = Mathf.Min(buf.Length, (int)(b * SR));
        for (int i = s; i < e; i++) buf[i] += d[(i - s) % d.Length] * vol(i / (float)SR);
    }

    void WriteAudio(string path)
    {
        var x = Sfx.I;
        buf = new float[(int)(Total * SR)];
        var rng = new System.Random(3);

        // store hum inside, wind outside
        Bed(x.hum, 3.5f, 9f, tt => 0.03f);
        Bed(x.hum, 9f, 39.5f, tt => 0.11f);
        float lp = 0;
        for (int i = (int)(3.5f * SR); i < 9f * SR; i++) { lp += ((float)(rng.NextDouble() * 2 - 1) - lp) * 0.01f; buf[i] += lp * 1.6f * Mathf.Sin(Mathf.PI * K(3.5f, 9f, i / (float)SR)); }

        // ominous pad: low detuned minor chord, swelling across the whole trailer
        float[] notes = { 55f, 55.4f, 65.4f, 82.4f, 110f, 130.8f };
        for (int i = (int)(0.5f * SR); i < 57f * SR; i++)
        {
            float tt = i / (float)SR;
            float env = Mathf.Lerp(0.04f, 0.16f, K(0.5f, 46f, tt)) * K(0.5f, 3f, tt);
            float v = 0;
            for (int n = 0; n < notes.Length; n++) v += Mathf.Sin(2 * Mathf.PI * notes[n] * tt + n) / (1 + n * 0.6f);
            v *= 0.7f + 0.3f * Mathf.Sin(2 * Mathf.PI * 0.2f * tt);
            buf[i] += v * env;
        }
        Bed(x.drone, 39.5f, 57f, tt => 0.25f + 0.35f * K(39.5f, 56f, tt));

        // trailer hits on each cut
        foreach (float c in new[] { 3.5f, 9f, 14.5f, 19f, 23.5f, 27.5f, 31f, 35f, 39.5f, 46f })
        {
            Mix(x.thud, c, 0.9f, 0.45f);
            Mix(x.thud, c + 0.02f, 0.5f, 0.3f);
        }

        Mix(x.chime, 5.2f, 0.25f);
        Mix(x.click, 15f, 0.6f, 0.7f);
        Mix(x.beep, 20.2f, 0.35f); Mix(x.beep, 20.75f, 0.35f);
        Mix(x.errBeep, 24.6f, 0.4f);
        Mix(x.whisper, 28.6f, 0.4f, 0.8f);
        Mix(x.whisper, 32.4f, 0.5f, 0.9f);
        Mix(x.ring, 9.6f, 0.18f);
        Mix(x.whisper, 37.6f, 0.7f, 0.75f);
        Mix(x.buzz, 38.9f, 0.5f);
        Mix(x.whisper, 44f, 0.6f, 1.1f);

        foreach (float o in new[] { 47.6f, 49.6f, 51.1f, 52.3f, 53.3f }) { Mix(x.buzz, o, 0.7f); Mix(x.thud, o + 0.42f, 0.6f, 0.6f); }
        for (float hb = 46.2f, gap = 1.1f; hb < 56.9f; hb += gap, gap = Mathf.Max(0.3f, gap * 0.9f))
        {
            Mix(x.thud, hb, 0.55f, 0.85f);
            Mix(x.thud, hb + 0.16f, 0.4f, 0.85f);
        }
        Mix(x.click, 54f, 0.8f);
        Mix(x.whisper, 54.3f, 0.9f, 1.25f);
        // riser into the scare
        float ph = 0;
        for (int i = (int)(50f * SR); i < 57f * SR; i++)
        {
            float k = K(50f, 57f, i / (float)SR);
            ph += 2 * Mathf.PI * Mathf.Lerp(90f, 1100f, k * k) / SR;
            buf[i] += (Mathf.Sin(ph) * 0.12f + (float)(rng.NextDouble() * 2 - 1) * 0.1f) * k * k;
        }
        // silence beat, then the stinger
        for (int i = (int)(56.85f * SR); i < 57f * SR; i++) buf[i] *= Mathf.Clamp01((57f * SR - i) / (0.15f * SR));
        Mix(x.stinger, 57f, 1.1f);
        Mix(x.staticNoise, 57f, 0.45f);
        Mix(x.thud, 57f, 1f, 0.35f);

        // title
        Mix(x.thud, 59.2f, 1f, 0.4f);
        Mix(x.buzz, 59.25f, 0.4f);
        Bed(x.drone, 59.2f, 66f, tt => 0.3f * (1f - K(63f, 66f, tt)));
        Mix(x.chime, 62.2f, 0.35f, 0.5f);

        float peak = 0.0001f;
        foreach (float v in buf) peak = Mathf.Max(peak, Mathf.Abs(v));
        float g = 0.89f / peak;
        using (var w = new BinaryWriter(File.Create(path)))
        {
            int n = buf.Length;
            w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + n * 2); w.Write(new[] { 'W', 'A', 'V', 'E' });
            w.Write(new[] { 'f', 'm', 't', ' ' }); w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(SR); w.Write(SR * 2); w.Write((short)2); w.Write((short)16);
            w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(n * 2);
            foreach (float v in buf) w.Write((short)Mathf.Clamp(v * g * 32767f, -32768f, 32767f));
        }
        Debug.Log("[Trailer] Audio written, peak " + peak);
    }
}
