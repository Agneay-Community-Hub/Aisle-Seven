using UnityEngine;

/// All UI is immediate-mode (OnGUI) so it needs no assets or UI packages.
public class HUD : MonoBehaviour
{
    Texture2D white, vig, noise;
    GUIStyle sTitle, sBig, sMid, sSmall, sBody, sCenter, sPaper, sPaperTitle, sMono;
    float lastS = -1f, sc;

    static readonly Color Green = new Color(0.75f, 1f, 0.78f);
    static readonly Color Red = new Color(1f, 0.32f, 0.28f);
    static readonly Color Yellow = new Color(1f, 0.88f, 0.45f);
    static readonly Color Ink = new Color(0.12f, 0.1f, 0.08f);

    void Awake()
    {
        white = new Texture2D(1, 1); white.SetPixel(0, 0, Color.white); white.Apply();
        const int n = 256;
        vig = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy) / 1.414f;
                vig.SetPixel(x, y, new Color(0, 0, 0, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.3f, 1f, r))));
            }
        vig.Apply();
        noise = new Texture2D(128, 128, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
        for (int y = 0; y < 128; y++)
            for (int x = 0; x < 128; x++) { float v = Random.value; noise.SetPixel(x, y, new Color(v, v, v, Random.value)); }
        noise.Apply();
    }

    void Styles()
    {
        sc = Screen.height / 1080f;
        if (Mathf.Approximately(sc, lastS)) return;
        lastS = sc;
        GUIStyle Mk(int size, TextAnchor a, FontStyle f = FontStyle.Normal)
        {
            var s = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(9, (int)(size * sc)), alignment = a, fontStyle = f, wordWrap = true, richText = true };
            s.normal.textColor = Color.white;
            return s;
        }
        sTitle = Mk(124, TextAnchor.MiddleCenter, FontStyle.Bold);
        sBig = Mk(48, TextAnchor.UpperLeft, FontStyle.Bold);
        sMid = Mk(30, TextAnchor.MiddleCenter);
        sSmall = Mk(24, TextAnchor.UpperLeft);
        sBody = Mk(27, TextAnchor.UpperLeft);
        sCenter = Mk(30, TextAnchor.UpperCenter);
        sPaper = Mk(27, TextAnchor.UpperLeft); sPaper.normal.textColor = Ink;
        sPaperTitle = Mk(36, TextAnchor.UpperCenter, FontStyle.Bold); sPaperTitle.normal.textColor = Ink;
        sMono = Mk(25, TextAnchor.UpperLeft);
    }

    float S(float v) => v * sc;

    void Box(Rect r, Color c) { var o = GUI.color; GUI.color = c; GUI.DrawTexture(r, white); GUI.color = o; }

    void L(Rect r, string t, GUIStyle st, Color c, float shadow = 2f)
    {
        var old = st.normal.textColor;
        st.normal.textColor = new Color(0, 0, 0, c.a * 0.85f);
        GUI.Label(new Rect(r.x + S(shadow), r.y + S(shadow), r.width, r.height), t, st);
        st.normal.textColor = c;
        GUI.Label(r, t, st);
        st.normal.textColor = old;
    }

    void OnGUI()
    {
        var gm = GameManager.I;
        if (gm == null) return;
        Styles();
        GUI.depth = 0;
        float W = Screen.width, H = Screen.height;
        var p = PlayerController.I;
        var a7 = Aisle7.I;

        // atmosphere overlays
        float vigA = 0.5f + gm.strikes * 0.12f + (a7 != null ? a7.Proximity * 0.45f : 0f);
        GUI.color = new Color(1, 1, 1, Mathf.Clamp01(vigA));
        GUI.DrawTexture(new Rect(0, 0, W, H), vig);
        float noiseA = 0.05f + gm.staticAmt * 0.8f + (p != null && p.InAisle7 ? 0.05f : 0f) + (a7 != null ? a7.Proximity * 0.12f : 0f);
        GUI.color = new Color(1, 1, 1, Mathf.Clamp01(noiseA));
        GUI.DrawTextureWithTexCoords(new Rect(0, 0, W, H), noise, new Rect(Random.value, Random.value, W / (256f * sc), H / (256f * sc)));
        GUI.color = Color.white;

        if (TrailerDirector.Active)
        {
            if (gm.HandbookOpen) Handbook(gm, W, H);
            if (gm.redFlash > 0) Box(new Rect(0, 0, W, H), new Color(0.6f, 0f, 0f, gm.redFlash * 0.45f));
            return;
        }

        switch (gm.state)
        {
            case GState.Title: Title(W, H); break;
            case GState.Intro: Intro(W, H); break;
            case GState.Playing: Playing(gm, p, a7, W, H); break;
        }

        if (gm.redFlash > 0) Box(new Rect(0, 0, W, H), new Color(0.6f, 0f, 0f, gm.redFlash * 0.45f));
        if (gm.fade > 0) Box(new Rect(0, 0, W, H), new Color(0, 0, 0, gm.fade));
        if (gm.state == GState.Ending) Ending(gm, W, H);
    }

    void Title(float W, float H)
    {
        Box(new Rect(0, 0, W, H), new Color(0, 0, 0, 0.45f));
        float flick = Random.value < 0.04f ? 0.35f : 1f;
        L(new Rect(0, H * 0.18f, W, S(160)), "AISLE SEVEN", sTitle, new Color(0.95f, 0.25f, 0.2f, flick), 4f);
        L(new Rect(0, H * 0.18f + S(150), W, S(50)), "a night shift at PIK-N-GO #0407", sMid, new Color(0.85f, 0.9f, 0.85f));
        L(new Rect(0, H * 0.52f, W, S(50)), (Time.unscaledTime % 1.2f < 0.8f) ? "[ click to clock in ]" : "", sMid, Yellow);
        string controls =
            "WASD  move        MOUSE  look        E  interact        TAB  handbook\n" +
            "F  flashlight        1 / 2  ring up / refuse        ESC  pause\n" +
            "SHIFT  sprint   (you'll want to remember which aisle not to do that in)";
        L(new Rect(W * 0.1f, H * 0.66f, W * 0.8f, S(140)), controls, sCenter, new Color(0.75f, 0.78f, 0.75f));
        L(new Rect(0, H - S(60), W, S(40)), "GitHub Game Off 2026  -  theme: SHIFT", sMid, new Color(0.5f, 0.5f, 0.5f));
    }

    void Paper(Rect r)
    {
        Box(new Rect(r.x + S(8), r.y + S(10), r.width, r.height), new Color(0, 0, 0, 0.5f));
        Box(r, new Color(0.93f, 0.9f, 0.8f));
        Box(new Rect(r.x, r.y, r.width, S(6)), new Color(0.75f, 0.2f, 0.15f));
    }

    void Intro(float W, float H)
    {
        Box(new Rect(0, 0, W, H), new Color(0, 0, 0, 0.6f));
        var r = new Rect(W * 0.5f - S(470), H * 0.5f - S(400), S(940), S(800));
        Paper(r);
        string note =
            "RILEY -\n\n" +
            "Welcome to nights. Sorry I couldn't stay to train you.\n\n" +
            "The register is easy. The handbook is not optional. Keep it on you (TAB). " +
            "It gets updated during the shift. I don't know who updates it. Read it every hour.\n\n" +
            "Three write-ups and you're off nights. Trust me, you don't want to be off nights.\n\n" +
            "The doors lock at 11 and open at 6. That's normal.\n\n" +
            "Don't worry about aisle seven. We don't have an aisle seven.\n\n" +
            "- Dale (Day Manager)";
        GUI.Label(new Rect(r.x + S(60), r.y + S(50), r.width - S(120), r.height - S(140)), note, sPaper);
        var st = sPaperTitle; var o = st.normal.textColor; st.normal.textColor = new Color(0.6f, 0.15f, 0.1f);
        GUI.Label(new Rect(r.x, r.yMax - S(80), r.width, S(50)), "[ press E to clock in ]", st);
        st.normal.textColor = o;
    }

    void Playing(GameManager gm, PlayerController p, Aisle7 a7, float W, float H)
    {
        // clock & status
        L(new Rect(S(40), S(28), S(600), S(70)), gm.ClockString(), sBig, Green);
        string w = "WRITE-UPS  ";
        for (int i = 0; i < GameManager.MaxStrikes; i++) w += i < gm.strikes ? "[X] " : "[  ] ";
        L(new Rect(S(42), S(92), S(600), S(40)), w, sSmall, gm.strikes > 0 ? Red : new Color(0.7f, 0.75f, 0.7f));
        L(new Rect(S(42), S(128), Mathf.Max(S(300), W * 0.5f - S(420) - S(60)), S(110)), gm.Objective, sSmall, Yellow);
        if (gm.seenRulesHour < gm.rulesHour)
            L(new Rect(S(42), S(190), S(600), S(40)), (Time.unscaledTime % 1f < 0.6f) ? "HANDBOOK UPDATED - press TAB" : "", sSmall, Red);

        // crosshair + prompt
        Box(new Rect(W * 0.5f - S(3), H * 0.5f - S(3), S(6), S(6)), new Color(1, 1, 1, 0.55f));
        if (p != null && p.Focus != null && p.Focus.Prompt != null && !gm.TransactionOpen)
            L(new Rect(0, H * 0.5f + S(30), W, S(50)), p.Focus.Prompt(), sMid, Yellow);

        // flashlight battery
        if (p != null && (p.FlashOn || p.Battery < 0.999f))
        {
            var br = new Rect(S(42), H - S(70), S(220), S(14));
            L(new Rect(br.x, br.y - S(34), S(400), S(30)), "FLASHLIGHT [F]", sSmall, new Color(0.8f, 0.8f, 0.75f));
            Box(br, new Color(0, 0, 0, 0.6f));
            Box(new Rect(br.x, br.y, br.width * p.Battery, br.height), p.Battery < 0.2f ? Red : new Color(0.95f, 0.9f, 0.6f));
        }

        if (gm.TransactionOpen && gm.current != null) Register(gm, W, H);

        // subtitles
        if (!string.IsNullOrEmpty(gm.subtitle))
        {
            string t = string.IsNullOrEmpty(gm.speaker) ? gm.subtitle : "<color=#ffd166>" + gm.speaker + ":</color>  " + gm.subtitle;
            var r = new Rect(W * 0.18f, H - S(190), W * 0.64f, S(120));
            L(r, t, sCenter, Color.white);
        }

        // notifications
        float y = S(30);
        foreach (var n in gm.notes)
        {
            float a = Mathf.Clamp01(n.t);
            float nh = sSmall.CalcHeight(new GUIContent(n.text), S(800)) + S(14);
            var r = new Rect(W * 0.5f - S(420), y, S(840), nh);
            Box(r, new Color(0, 0, 0, 0.6f * a));
            var st = sSmall; var al = st.alignment; st.alignment = TextAnchor.MiddleCenter;
            L(r, n.text, st, n.bad ? new Color(Red.r, Red.g, Red.b, a) : new Color(0.9f, 0.95f, 0.9f, a));
            st.alignment = al;
            y += nh + S(8);
        }

        if (gm.HandbookOpen) Handbook(gm, W, H);

        if (gm.Paused)
        {
            Box(new Rect(0, 0, W, H), new Color(0, 0, 0, 0.7f));
            L(new Rect(0, H * 0.35f, W, S(80)), "PAUSED", sTitle, Color.white);
            L(new Rect(0, H * 0.52f, W, S(50)), "[ click to clock back in ]", sMid, Yellow);
            L(new Rect(0, H * 0.6f, W, S(50)), "Mouse sensitivity: " + PlayerController.Sensitivity.ToString("0.0") + "     ( [ / ] to adjust )", sMid, new Color(0.8f, 0.8f, 0.8f));
        }
    }

    void Register(GameManager gm, float W, float H)
    {
        var s = gm.current.spec;
        var r = new Rect(W - S(600), S(220), S(560), S(560));
        Box(r, new Color(0.01f, 0.05f, 0.02f, 0.88f));
        Box(new Rect(r.x, r.y, r.width, S(4)), new Color(0.3f, 1f, 0.45f, 0.8f));
        float x = r.x + S(28), y = r.y + S(22), w = r.width - S(56);
        L(new Rect(x, y, w, S(36)), "REGISTER 01  -  PIK-N-GO #0407", sMono, Green, 1f); y += S(50);
        for (int i = 0; i < s.items.Count; i++)
        {
            string line;
            if (i < gm.scanShown)
                line = s.items[i].found
                    ? s.items[i].name + "  <color=#9cffb0>$" + s.items[i].price.ToString("0.00") + "</color>"
                    : s.items[i].name + "  <color=#ff5a50>ITEM NOT IN SYSTEM</color>";
            else line = "<color=#3c6a44>scanning...</color>";
            L(new Rect(x, y, w, S(34)), "> " + line, sMono, Green, 1f);
            y += S(36);
        }
        y += S(12);
        if (gm.ScanDone)
        {
            L(new Rect(x, y, w, S(34)), "TOTAL  $" + s.Total.ToString("0.00"), sMono, Green, 1f); y += S(44);
            L(new Rect(x, y, w, S(70)), "PAYMENT OFFERED:  <color=#ffd166>" + s.payment + "</color>", sMono, Green, 1f); y += S(80);
            L(new Rect(x, r.yMax - S(130), w, S(40)), "[1]  RING UP          [2]  REFUSE SALE", sMono, Color.white, 1f);
            L(new Rect(x, r.yMax - S(84), w, S(70)), "Look at them. Check the handbook [TAB].  [Q] step back", sMono, new Color(0.6f, 0.75f, 0.62f), 1f);
        }
    }

    void Handbook(GameManager gm, float W, float H)
    {
        var r = new Rect(W * 0.5f - S(520), H * 0.5f - S(470), S(1040), S(940));
        Paper(r);
        GUI.Label(new Rect(r.x, r.y + S(28), r.width, S(50)), "PIK-N-GO NIGHT STAFF HANDBOOK  (rev. " + gm.rulesHour + ")", sPaperTitle);
        var o = sPaper.normal.textColor;
        sPaper.normal.textColor = new Color(0.55f, 0.12f, 0.08f);
        GUI.Label(new Rect(r.x + S(50), r.y + S(84), r.width - S(100), S(40)), Rules.Header, sPaper);
        sPaper.normal.textColor = o;
        float y = r.y + S(132);
        foreach (var rule in Rules.All)
        {
            if (rule.hour > gm.rulesHour) continue;
            bool isNew = rule.hour == gm.rulesHour && gm.rulesHour > 1;
            string t = (isNew ? "<color=#b3261e><b>NEW</b></color>  " : "") + "<b>" + rule.num + ".</b>  " + rule.text;
            float h = sPaper.CalcHeight(new GUIContent(t), r.width - S(100));
            GUI.Label(new Rect(r.x + S(50), y, r.width - S(100), h), t, sPaper);
            y += h + S(10);
        }
        if (!TrailerDirector.Active) GUI.Label(new Rect(r.x, r.yMax - S(56), r.width, S(40)), "[TAB] close", sPaperTitle);
    }

    void Ending(GameManager gm, float W, float H)
    {
        Box(new Rect(0, 0, W, H), Color.black);
        bool bad = gm.endTitle == "REASSIGNED";
        L(new Rect(0, H * 0.16f, W, S(160)), gm.endTitle, sTitle, bad ? Red : Green, 4f);
        L(new Rect(W * 0.15f, H * 0.38f, W * 0.7f, H * 0.4f), gm.endBody, sCenter, new Color(0.85f, 0.85f, 0.82f));
        L(new Rect(0, H * 0.84f, W, S(50)), "[ R ]  work another shift", sMid, Yellow);
    }
}
