using System.Collections.Generic;
using UnityEngine;

/// Builds the PIK-N-GO #0407 convenience store and runs its lights, signs and window visitor.
public class Store : MonoBehaviour
{
    public static Store I;
    public static Texture2D Checker;
    public static readonly Vector3 PlayerStart = new Vector3(-4f, 0f, 1.45f);
    public static readonly Vector3 DoorPos = new Vector3(6f, 1.2f, 0f);
    public Transform PhoneT;
    public Interactable RegisterI;

    static readonly Color LightCol = new Color(0.9f, 1f, 0.94f);
    readonly List<Light> lights = new List<Light>();
    readonly List<Material> fixtureMats = new List<Material>();
    readonly List<TextMesh> signs = new List<TextMesh>();
    readonly List<string> signText = new List<string>();
    readonly List<Vector3> slots = new List<Vector3>();
    readonly List<float> slotSide = new List<float>();
    Light emergency, sun;
    AudioSource hum;
    float outageT, flickerT, flickTick, creep = 1f, swapT;
    bool[] off;
    int badLight, swapIdx = -1;
    Transform figure, figHead;
    float figT, figSeen;
    Material[] productMats;

    public void Build()
    {
        I = this;
        Checker = MakeChecker();
        productMats = new[]
        {
            Mat.Lit(new Color(0.75f, 0.12f, 0.1f), 0.35f), Mat.Lit(new Color(0.9f, 0.75f, 0.15f), 0.35f), Mat.Lit(new Color(0.12f, 0.3f, 0.7f), 0.35f),
            Mat.Lit(new Color(0.9f, 0.45f, 0.1f), 0.35f), Mat.Lit(new Color(0.15f, 0.55f, 0.25f), 0.35f), Mat.Lit(new Color(0.45f, 0.2f, 0.6f), 0.35f),
            Mat.Lit(new Color(0.9f, 0.9f, 0.88f), 0.35f), Mat.Lit(new Color(0.45f, 0.3f, 0.18f), 0.25f), Mat.Lit(new Color(0.1f, 0.55f, 0.55f), 0.35f),
            Mat.Lit(new Color(0.85f, 0.4f, 0.55f), 0.35f), Mat.Lit(new Color(0.1f, 0.1f, 0.12f), 0.5f), Mat.Lit(new Color(0.85f, 0.8f, 0.6f), 0.25f),
        };

        var st = new GameObject("Static").transform; st.SetParent(transform, false);
        var dyn = new GameObject("Dynamic").transform; dyn.SetParent(transform, false);

        Material wall = Mat.Lit(new Color(0.62f, 0.64f, 0.58f)), ceil = Mat.Lit(new Color(0.33f, 0.34f, 0.33f));
        Material trim = Mat.Lit(new Color(0.12f, 0.32f, 0.26f)), dark = Mat.Lit(new Color(0.08f, 0.08f, 0.09f), 0.3f);
        Material metal = Mat.Lit(new Color(0.55f, 0.57f, 0.6f), 0.45f), glass = Mat.Glass(new Color(0.25f, 0.35f, 0.4f, 0.22f));

        // ---- shell
        Geo.Slab("Floor", st, new Vector3(0, -0.05f, 10), new Vector3(20, 0.1f, 20), Mat.Textured(Checker, new Vector2(2, 2), Color.white, 0.55f));
        Geo.Slab("Ceiling", st, new Vector3(0, 3.25f, 10), new Vector3(20, 0.1f, 20), ceil, false);
        Geo.Slab("WallL", st, new Vector3(-10.05f, 1.6f, 10), new Vector3(0.1f, 3.3f, 20), wall);
        Geo.Slab("WallR", st, new Vector3(10.05f, 1.6f, 10), new Vector3(0.1f, 3.3f, 20), wall);
        Geo.Slab("WallB1", st, new Vector3(-5.6f, 1.6f, 20.05f), new Vector3(8.8f, 3.3f, 0.1f), wall);
        Geo.Slab("WallB2", st, new Vector3(5.6f, 1.6f, 20.05f), new Vector3(8.8f, 3.3f, 0.1f), wall);
        Geo.Box("WallB3", st, new Vector3(0, 2.95f, 20.05f), new Vector3(2.4f, 0.7f, 0.1f), wall);
        Geo.Box("StripeL", st, new Vector3(-9.99f, 1.25f, 10), new Vector3(0.02f, 0.18f, 20), trim, false);
        Geo.Box("StripeR", st, new Vector3(9.99f, 1.25f, 10), new Vector3(0.02f, 0.18f, 20), trim, false);
        Geo.Box("StripeB1", st, new Vector3(-5.6f, 1.25f, 19.99f), new Vector3(8.8f, 0.18f, 0.02f), trim, false);
        Geo.Box("StripeB2", st, new Vector3(5.6f, 1.25f, 19.99f), new Vector3(8.8f, 0.18f, 0.02f), trim, false);

        // ---- front wall: low wall + glass + door gap x 5..7
        Geo.Box("FrontLow1", st, new Vector3(-2.5f, 0.45f, -0.05f), new Vector3(15f, 0.9f, 0.12f), trim);
        Geo.Box("FrontLow2", st, new Vector3(8.5f, 0.45f, -0.05f), new Vector3(3f, 0.9f, 0.12f), trim);
        Geo.Box("FrontTop", st, new Vector3(0, 2.95f, -0.05f), new Vector3(20f, 0.7f, 0.12f), trim);
        Geo.Box("Glass1", st, new Vector3(-2.5f, 1.75f, -0.05f), new Vector3(15f, 1.7f, 0.04f), glass);
        Geo.Box("Glass2", st, new Vector3(8.5f, 1.75f, -0.05f), new Vector3(3f, 1.7f, 0.04f), glass);
        for (float x = -10f; x <= 10.01f; x += 3f)
            if (x < 4.9f || x > 7.1f) Geo.Box("Mullion", st, new Vector3(x, 1.75f, -0.05f), new Vector3(0.1f, 1.7f, 0.1f), dark, false);
        Geo.Box("DoorFrameL", st, new Vector3(4.95f, 1.3f, -0.05f), new Vector3(0.1f, 2.6f, 0.14f), dark);
        Geo.Box("DoorFrameR", st, new Vector3(7.05f, 1.3f, -0.05f), new Vector3(0.1f, 2.6f, 0.14f), dark);

        var door = new GameObject("AutoDoor").transform; door.SetParent(dyn, false); door.localPosition = new Vector3(6f, 0, -0.05f);
        var dl = Geo.Box("DoorL", door, new Vector3(-0.5f, 1.3f, 0), new Vector3(1f, 2.55f, 0.05f), glass, false);
        var dr = Geo.Box("DoorR", door, new Vector3(0.5f, 1.3f, 0.03f), new Vector3(1f, 2.55f, 0.05f), glass, false);
        var ad = door.gameObject.AddComponent<AutoDoor>(); ad.left = dl.transform; ad.right = dr.transform;
        var block = new GameObject("DoorBlock"); block.transform.SetParent(dyn, false);
        block.transform.localPosition = new Vector3(6f, 1.3f, -0.05f);
        block.AddComponent<BoxCollider>().size = new Vector3(2.1f, 2.6f, 0.3f);
        Geo.Box("ExitSign", st, new Vector3(6f, 2.82f, 0.06f), new Vector3(0.7f, 0.24f, 0.05f), Mat.Glow(new Color(1f, 0.15f, 0.1f), 1.6f), false);
        Geo.Text(st, "EXIT", new Vector3(6f, 2.82f, 0.095f), 0.18f, Color.white, Quaternion.Euler(0, 180f, 0));

        // ---- exterior
        Geo.Box("Asphalt", st, new Vector3(0, -0.08f, -22f), new Vector3(90f, 0.1f, 44f), Mat.Lit(new Color(0.07f, 0.07f, 0.075f), 0.2f));
        Geo.Box("Sidewalk", st, new Vector3(0, 0.0f, -1.3f), new Vector3(24f, 0.1f, 2.4f), Mat.Lit(new Color(0.3f, 0.3f, 0.29f)));
        for (int i = 0; i < 6; i++)
            Geo.Box("Line", st, new Vector3(-8f + i * 3f, -0.02f, -6f), new Vector3(0.1f, 0.02f, 4f), Mat.Lit(new Color(0.55f, 0.5f, 0.25f)), false);
        Geo.Box("LampPole", st, new Vector3(-9f, 2.5f, -11f), new Vector3(0.15f, 5f, 0.15f), dark);
        Geo.Box("LampHead", st, new Vector3(-8.6f, 5f, -11f), new Vector3(0.9f, 0.12f, 0.35f), Mat.Glow(new Color(1f, 0.6f, 0.25f), 2.2f), false);
        var lamp = new GameObject("StreetLight").AddComponent<Light>(); lamp.transform.SetParent(dyn, false);
        lamp.transform.localPosition = new Vector3(-8.5f, 4.6f, -11f); lamp.type = LightType.Point; lamp.range = 15f; lamp.intensity = 1.6f; lamp.color = new Color(1f, 0.55f, 0.2f);
        var car = Mat.Lit(new Color(0.3f, 0.05f, 0.05f), 0.6f);
        Geo.Box("CarBody", st, new Vector3(-5f, 0.55f, -8f), new Vector3(1.8f, 0.7f, 4.2f), car);
        Geo.Box("CarCabin", st, new Vector3(-5f, 1.15f, -8.2f), new Vector3(1.6f, 0.55f, 2.2f), Mat.Lit(new Color(0.05f, 0.06f, 0.08f), 0.8f));
        foreach (var w in new[] { new Vector3(-0.9f, 0.3f, 1.3f), new Vector3(0.9f, 0.3f, 1.3f), new Vector3(-0.9f, 0.3f, -1.3f), new Vector3(0.9f, 0.3f, -1.3f) })
        {
            var wh = Geo.Prim(PrimitiveType.Cylinder, "Wheel", st, new Vector3(-5f, 0, -8f) + w, new Vector3(0.6f, 0.12f, 0.6f), dark, false);
            wh.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        }

        // ---- counter
        Geo.Box("Counter", st, new Vector3(-4f, 0.5f, 3f), new Vector3(4.2f, 1f, 0.9f), Mat.Lit(new Color(0.45f, 0.12f, 0.1f), 0.3f));
        Geo.Box("CounterTop", st, new Vector3(-4f, 1.025f, 3f), new Vector3(4.4f, 0.05f, 1.05f), Mat.Lit(new Color(0.18f, 0.18f, 0.2f), 0.7f));
        Geo.Box("Partition", st, new Vector3(-6.25f, 0.5f, 1.5f), new Vector3(0.1f, 1f, 3f), Mat.Lit(new Color(0.45f, 0.12f, 0.1f), 0.3f));
        Geo.Box("CounterKick", st, new Vector3(-4f, 0.06f, 3.46f), new Vector3(4.2f, 0.12f, 0.02f), dark, false);
        Geo.Box("Lotto", st, new Vector3(-2.6f, 1.25f, 3.2f), new Vector3(0.6f, 0.4f, 0.08f), Mat.Glow(new Color(0.2f, 0.6f, 1f), 1.0f), false);
        Geo.Text(st, "LOTTO", new Vector3(-2.6f, 1.25f, 3.155f), 0.14f, Color.white);
        // cigarette-style wall rack behind the player is the window, so put a snack rack on the partition
        for (int i = 0; i < 10; i++)
            Geo.Box("Gum", st, new Vector3(-6.15f, 1.08f, 0.4f + i * 0.22f), new Vector3(0.08f, 0.1f, 0.18f), productMats[i % productMats.Length], false);

        var reg = Geo.Box("Register", dyn, new Vector3(-5.05f, 1.2f, 3f), new Vector3(0.55f, 0.3f, 0.45f), dark);
        var screen = Geo.Box("RegScreen", dyn, new Vector3(-5.05f, 1.45f, 2.82f), new Vector3(0.36f, 0.22f, 0.03f), Mat.Glow(new Color(0.15f, 0.8f, 0.35f), 0.55f), false);
        screen.transform.localRotation = Quaternion.Euler(-25f, 25f, 0);
        Geo.Box("Scanner", dyn, new Vector3(-3.55f, 1.06f, 3f), new Vector3(0.35f, 0.02f, 0.3f), Mat.Glow(new Color(1f, 0.1f, 0.05f), 1.2f), false);
        var regI = reg.AddComponent<Interactable>();
        regI.Prompt = () => "[E] Ring up customer";
        regI.CanUse = () => GameManager.I != null && GameManager.I.CanRingUp;
        regI.OnUse = () => GameManager.I.OpenTransaction();
        RegisterI = regI;

        var phone = Geo.Box("Phone", dyn, new Vector3(-5.8f, 1.1f, 2.8f), new Vector3(0.26f, 0.12f, 0.22f), Mat.Lit(new Color(0.8f, 0.76f, 0.66f), 0.4f));
        Geo.Box("Handset", phone.transform, new Vector3(0, 0.7f, 0), new Vector3(1.1f, 0.4f, 0.35f), Mat.Lit(new Color(0.8f, 0.76f, 0.66f), 0.4f), false);
        PhoneT = phone.transform;
        var phI = phone.AddComponent<Interactable>();
        phI.Prompt = () => "[E] Answer the phone";
        phI.CanUse = () => GameManager.I != null && GameManager.I.phoneRinging && GameManager.I.PlayerActive;
        phI.OnUse = () => GameManager.I.AnswerPhone();

        var book = Geo.Box("Handbook", dyn, new Vector3(-3.0f, 1.06f, 2.85f), new Vector3(0.3f, 0.03f, 0.4f), Mat.Lit(new Color(0.85f, 0.8f, 0.6f)));
        book.transform.localRotation = Quaternion.Euler(0, 12f, 0);
        var bI = book.AddComponent<Interactable>();
        bI.Prompt = () => "[E] Read the handbook (or press TAB)";
        bI.CanUse = () => GameManager.I != null && GameManager.I.PlayerActive && !GameManager.I.HandbookOpen;
        bI.OnUse = () => GameManager.I.ToggleHandbook();

        // ---- shelving: six rows, aisles 1-6 in between (aisle 4 leads to aisle 7)
        float[] rows = { -7.5f, -4.5f, -1.5f, 1.5f, 4.5f, 7.5f };
        foreach (float x in rows) ShelfRow(st, x, 7.5f, 16.5f, metal, dark);

        string[] cats = { "SNACKS", "CANNED GOODS", "HOUSEHOLD", "BEVERAGES", "AUTO / PET", "PHARMACY" };
        var navy = Mat.Lit(new Color(0.08f, 0.12f, 0.3f), 0.3f);
        for (int i = 0; i < 6; i++)
        {
            float x = -9f + 3f * i;
            Geo.Box("SignBoard", st, new Vector3(x, 2.72f, 7.3f), new Vector3(1.6f, 0.5f, 0.05f), navy, false);
            Geo.Box("SignWire", st, new Vector3(x - 0.6f, 3.0f, 7.3f), new Vector3(0.01f, 0.5f, 0.01f), dark, false);
            Geo.Box("SignWire", st, new Vector3(x + 0.6f, 3.0f, 7.3f), new Vector3(0.01f, 0.5f, 0.01f), dark, false);
            var tm = Geo.Text(dyn, "AISLE " + (i + 1), new Vector3(x, 2.78f, 7.265f), 0.2f, Color.white);
            Geo.Text(dyn, cats[i], new Vector3(x, 2.58f, 7.265f), 0.09f, new Color(0.8f, 0.85f, 1f));
            signs.Add(tm); signText.Add(tm.text);
        }

        // ---- cooler wall on the right
        Geo.Box("CoolerBody", st, new Vector3(9.5f, 1.2f, 12f), new Vector3(0.95f, 2.4f, 10f), dark);
        var coolGlow = Mat.Glow(new Color(0.55f, 0.85f, 1f), 0.7f);
        for (int i = 0; i < 9; i++)
        {
            float z = 7.6f + i * 1.1f;
            Geo.Box("CoolerDoor", st, new Vector3(9.01f, 1.2f, z), new Vector3(0.04f, 1.9f, 1.0f), coolGlow, false);
            for (int k = 0; k < 4; k++)
                Geo.Box("Can", st, new Vector3(8.97f, 0.55f + k * 0.45f, z), new Vector3(0.04f, 0.25f, 0.7f), productMats[(i + k) % productMats.Length], false);
        }
        Geo.Text(dyn, "COLD DRINKS", new Vector3(9.4f, 2.6f, 12f), 0.22f, new Color(0.6f, 0.9f, 1f), Quaternion.Euler(0, 90f, 0));

        // ---- aisle seven entrance
        Geo.Box("A7Board", st, new Vector3(0, 2.92f, 19.96f), new Vector3(2.0f, 0.5f, 0.05f), Mat.Glow(new Color(0.6f, 0.04f, 0.04f), 0.9f), false);
        Geo.Text(dyn, "AISLE 7", new Vector3(0, 2.92f, 19.92f), 0.26f, new Color(1f, 0.9f, 0.85f));
        var yellow = Mat.Lit(new Color(0.95f, 0.8f, 0.1f), 0.4f);
        var wf1 = Geo.Box("WetFloorA", st, new Vector3(1.55f, 0.35f, 19.15f), new Vector3(0.45f, 0.72f, 0.03f), yellow);
        wf1.transform.localRotation = Quaternion.Euler(15f, 20f, 0);
        var wf2 = Geo.Box("WetFloorB", st, new Vector3(1.6f, 0.35f, 19.35f), new Vector3(0.45f, 0.72f, 0.03f), yellow);
        wf2.transform.localRotation = Quaternion.Euler(-15f, 20f, 0);
        Geo.Text(dyn, "CAUTION", new Vector3(1.53f, 0.45f, 19.1f), 0.07f, Color.black, Quaternion.Euler(15f, 20f, 0));

        // ---- branding
        Geo.Text(dyn, "PIK-N-GO", new Vector3(-6f, 2.55f, 19.9f), 0.45f, new Color(0.9f, 0.3f, 0.2f));
        Geo.Text(dyn, "#0407", new Vector3(-6f, 2.15f, 19.9f), 0.2f, new Color(0.8f, 0.8f, 0.7f));
        Geo.Text(dyn, "OPEN 24 HOURS", new Vector3(5.5f, 2.5f, 19.9f), 0.3f, new Color(0.3f, 0.8f, 0.4f));
        Geo.Text(dyn, "WE NEVER CLOSE", new Vector3(5.5f, 2.15f, 19.9f), 0.14f, new Color(0.7f, 0.7f, 0.65f));
        Geo.Text(dyn, "EMPLOYEES ONLY", new Vector3(-9.93f, 2.3f, 2f), 0.16f, new Color(0.85f, 0.85f, 0.8f), Quaternion.Euler(0, -90f, 0));
        Geo.Box("BackDoor", st, new Vector3(-9.97f, 1.05f, 2f), new Vector3(0.05f, 2.1f, 1f), Mat.Lit(new Color(0.3f, 0.32f, 0.3f), 0.4f));

        // ---- ceiling lights
        float[] lx = { -6f, 0f, 6f }; float[] lz = { 4.5f, 10.5f, 16.5f };
        foreach (float z in lz)
            foreach (float x in lx)
            {
                var m = Mat.Glow(LightCol, 1.6f, true);
                Geo.Box("Fixture", dyn, new Vector3(x, 3.17f, z), new Vector3(0.45f, 0.05f, 2.2f), m, false);
                fixtureMats.Add(m);
                var lg = new GameObject("CeilingLight"); lg.transform.SetParent(dyn, false); lg.transform.localPosition = new Vector3(x, 2.85f, z);
                var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 9f; l.intensity = 1.25f; l.color = LightCol; l.shadows = LightShadows.None;
                l.cullingMask = ~(1 << Aisle7.Layer);
                lights.Add(l);
            }
        off = new bool[lights.Count];
        badLight = 7; // the one nearest aisle seven

        var eg = new GameObject("EmergencyLight"); eg.transform.SetParent(dyn, false); eg.transform.localPosition = new Vector3(6f, 2.6f, 1f);
        emergency = eg.AddComponent<Light>(); emergency.type = LightType.Point; emergency.range = 14f; emergency.intensity = 0.9f;
        emergency.color = new Color(1f, 0.1f, 0.08f); emergency.enabled = false;

        var sg = new GameObject("Dawn"); sg.transform.SetParent(dyn, false); sg.transform.rotation = Quaternion.Euler(12f, 20f, 0);
        sun = sg.AddComponent<Light>(); sun.type = LightType.Directional; sun.color = new Color(1f, 0.65f, 0.4f); sun.intensity = 0.9f; sun.enabled = false;

        BuildFigure(dyn);
        hum = Sfx.I.Loop2D(Sfx.I.hum, 0.09f);
        StaticBatchingUtility.Combine(st.gameObject);
    }

    void ShelfRow(Transform p, float x, float z0, float z1, Material metal, Material dark)
    {
        float len = z1 - z0, zc = (z0 + z1) * 0.5f;
        Geo.Slab("ShelfBase", p, new Vector3(x, 0.08f, zc), new Vector3(1.0f, 0.16f, len), dark, true, 4f, 3f);
        Geo.Slab("ShelfBack", p, new Vector3(x, 1.05f, zc), new Vector3(0.06f, 1.9f, len), metal, true, 4f, 3f);
        foreach (float y in new[] { 0.72f, 1.28f, 1.84f })
            Geo.Slab("Board", p, new Vector3(x, y, zc), new Vector3(1.0f, 0.035f, len), metal, false, 4f, 3f);
        Geo.Box("EndCapA", p, new Vector3(x, 1.0f, z0), new Vector3(1.04f, 2.0f, 0.05f), metal, false);
        Geo.Box("EndCapB", p, new Vector3(x, 1.0f, z1), new Vector3(1.04f, 2.0f, 0.05f), metal, false);
        float[] levels = { 0.16f, 0.735f, 1.295f, 1.855f };
        for (int side = -1; side <= 1; side += 2)
            for (int li = 0; li < levels.Length; li++)
                for (float z = z0 + 0.25f; z < z1 - 0.2f; z += Random.Range(0.3f, 0.46f))
                {
                    if (Random.value > (li == 3 ? 0.4f : 0.88f)) continue;
                    float w = Random.Range(0.18f, 0.3f), h = Random.Range(0.18f, li == 0 ? 0.48f : 0.42f), d = Random.Range(0.22f, 0.38f);
                    var pos = new Vector3(x + side * (0.06f + d * 0.5f + 0.02f), levels[li] + h * 0.5f + 0.01f, z);
                    var m = productMats[Random.Range(0, productMats.Length)];
                    if (Random.value < 0.25f) Geo.Prim(PrimitiveType.Cylinder, "Jar", p, pos, new Vector3(w * 0.8f, h * 0.5f, w * 0.8f), m, false);
                    else Geo.Box("Product", p, pos, new Vector3(d, h, w), m, false);
                    if (li < 3) { slots.Add(pos); slotSide.Add(side); }
                }
    }

    void BuildFigure(Transform p)
    {
        figure = new GameObject("WindowVisitor").transform; figure.SetParent(p, false);
        var pale = Mat.Lit(new Color(0.88f, 0.88f, 0.85f), 0.1f);
        var black = Mat.Unlit(new Color(0.01f, 0.01f, 0.01f));
        Geo.Prim(PrimitiveType.Capsule, "Body", figure, new Vector3(0, 0.95f, 0), new Vector3(0.45f, 0.75f, 0.3f), Mat.Lit(new Color(0.12f, 0.12f, 0.13f)), false);
        Geo.Prim(PrimitiveType.Capsule, "ArmL", figure, new Vector3(-0.27f, 1.35f, 0.3f), new Vector3(0.09f, 0.3f, 0.09f), pale, false).transform.localRotation = Quaternion.Euler(70f, 0, 0);
        Geo.Prim(PrimitiveType.Capsule, "ArmR", figure, new Vector3(0.27f, 1.35f, 0.3f), new Vector3(0.09f, 0.3f, 0.09f), pale, false).transform.localRotation = Quaternion.Euler(70f, 0, 0);
        Geo.Prim(PrimitiveType.Sphere, "HandL", figure, new Vector3(-0.28f, 1.45f, 0.6f), new Vector3(0.12f, 0.14f, 0.05f), pale, false);
        Geo.Prim(PrimitiveType.Sphere, "HandR", figure, new Vector3(0.28f, 1.45f, 0.6f), new Vector3(0.12f, 0.14f, 0.05f), pale, false);
        figHead = new GameObject("Head").transform; figHead.SetParent(figure, false); figHead.localPosition = new Vector3(0, 1.75f, 0.1f);
        Geo.Prim(PrimitiveType.Sphere, "Skull", figHead, Vector3.zero, new Vector3(0.25f, 0.3f, 0.26f), pale, false);
        Geo.Prim(PrimitiveType.Sphere, "EyeL", figHead, new Vector3(-0.05f, 0.03f, 0.11f), new Vector3(0.05f, 0.06f, 0.03f), black, false);
        Geo.Prim(PrimitiveType.Sphere, "EyeR", figHead, new Vector3(0.05f, 0.03f, 0.11f), new Vector3(0.05f, 0.06f, 0.03f), black, false);
        Geo.Box("Mouth", figHead, new Vector3(0, -0.08f, 0.115f), new Vector3(0.1f, 0.05f, 0.02f), black, false);
        figure.gameObject.SetActive(false);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (outageT > 0) outageT -= dt;
        if (flickerT > 0) flickerT -= dt;
        flickTick -= dt;
        bool reroll = flickTick <= 0;
        if (reroll) flickTick = 0.06f;
        bool anyOn = false;
        for (int i = 0; i < lights.Count; i++)
        {
            if (reroll) off[i] = (flickerT > 0 && Random.value < 0.5f) || (i == badLight && Random.value < 0.015f);
            bool on = outageT <= 0 && !off[i];
            lights[i].enabled = on;
            lights[i].intensity = 1.25f * creep;
            fixtureMats[i].SetColor("_EmissionColor", on ? LightCol * 1.6f * creep : Color.black);
            anyOn |= on;
        }
        emergency.enabled = outageT > 0 && outageT < 900f;
        hum.volume = Mathf.MoveTowards(hum.volume, anyOn ? 0.09f : 0f, dt * 2f);
        if (swapIdx >= 0) { swapT -= dt; if (swapT <= 0) { signs[swapIdx].text = signText[swapIdx]; swapIdx = -1; } }
        UpdateFigure(dt);
    }

    // ---------------------------------------------------------------- events
    public void Flicker(float d) { flickerT = Mathf.Max(flickerT, d); Sfx.I.Play(Sfx.I.buzz, 0.4f); }

    public void Outage(float d)
    {
        outageT = d;
        Sfx.I.Play(Sfx.I.thud, 0.7f, 0.6f);
        Sfx.I.Play(Sfx.I.buzz, 0.5f, 0.8f);
    }

    public void SetCreep(int hour)
    {
        creep = 1f - 0.05f * (hour - 1);
    }

    public void SetDawn()
    {
        outageT = 0; flickerT = 0; creep = 1.15f;
        sun.enabled = true;
        RenderSettings.ambientLight = new Color(0.25f, 0.2f, 0.18f);
        RenderSettings.fogColor = new Color(0.32f, 0.22f, 0.18f);
        if (PlayerController.I != null) PlayerController.I.Cam.backgroundColor = new Color(0.45f, 0.3f, 0.25f);
    }

    public void SwapSign()
    {
        if (swapIdx >= 0) return;
        swapIdx = Random.Range(0, signs.Count);
        signs[swapIdx].text = "AISLE 7";
        swapT = 20f;
    }

    public void DropProduct()
    {
        if (slots.Count == 0) return;
        Vector3 pp = PlayerController.I.transform.position;
        int best = Random.Range(0, slots.Count);
        for (int k = 0; k < 20; k++)
        {
            int i = Random.Range(0, slots.Count);
            float d = Vector3.Distance(slots[i], pp);
            if (d > 3f && d < 10f) { best = i; break; }
        }
        var g = Geo.Box("Fallen", transform, slots[best] + new Vector3(slotSide[best] * 0.2f, 0.05f, 0), new Vector3(0.25f, 0.3f, 0.2f), productMats[Random.Range(0, productMats.Length)]);
        var rb = g.AddComponent<Rigidbody>();
        rb.mass = 0.5f;
        rb.linearVelocity = new Vector3(slotSide[best] * Random.Range(1.2f, 2.2f), 0.6f, Random.Range(-0.4f, 0.4f));
        rb.angularVelocity = Random.insideUnitSphere * 5f;
        g.AddComponent<ThudOnHit>();
    }

    public void ShowWindowFigure()
    {
        if (figure.gameObject.activeSelf) return;
        float px = PlayerController.I.transform.position.x;
        float[] xs = { -8.5f, -5.5f, -4.6f, -3.4f, -2.5f, 0.5f, 3.5f, 8.5f }; // avoid the window mullions
        float x = xs[Random.Range(0, xs.Length)];
        if (Random.value < 0.6f)
        {
            float bd = 99f;
            foreach (var c in xs) if (Mathf.Abs(c - px) < bd) { bd = Mathf.Abs(c - px); x = c; }
        }
        figure.localPosition = new Vector3(x, 0, -0.75f);
        figure.localRotation = Quaternion.identity;
        figure.gameObject.SetActive(true);
        figT = 14f; figSeen = 0f;
    }

    public void DebugFigureAt(float x) { figure.localPosition = new Vector3(x, 0, -0.75f); figure.gameObject.SetActive(true); figT = 30f; figSeen = -99f; }

    void UpdateFigure(float dt)
    {
        if (!figure.gameObject.activeSelf || PlayerController.I == null) return;
        figT -= dt;
        var cam = PlayerController.I.Cam.transform;
        Vector3 to = cam.position - figHead.position;
        if (to.sqrMagnitude > 0.01f) figHead.rotation = Quaternion.Slerp(figHead.rotation, Quaternion.LookRotation(to), dt * 3f);
        Vector3 d = figHead.position - cam.position;
        if (Vector3.Dot(cam.forward, d.normalized) > 0.92f && d.magnitude < 16f) figSeen += dt;
        if (figSeen > 0.9f || figT <= 0)
        {
            figure.gameObject.SetActive(false);
            if (figSeen > 0.9f) Flicker(0.3f);
        }
    }

    static Texture2D MakeChecker()
    {
        const int n = 32;
        var t = new Texture2D(n, n, TextureFormat.RGBA32, true);
        t.wrapMode = TextureWrapMode.Repeat; t.filterMode = FilterMode.Trilinear; t.anisoLevel = 4;
        Color a = new Color(0.82f, 0.82f, 0.78f), b = new Color(0.4f, 0.46f, 0.44f), grout = new Color(0.25f, 0.26f, 0.25f);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                bool tile = (x < n / 2) ^ (y < n / 2);
                bool g = (x % (n / 2) == 0) || (y % (n / 2) == 0);
                Color c = g ? grout : (tile ? a : b);
                float noise = (Mathf.PerlinNoise(x * 0.3f, y * 0.3f) - 0.5f) * 0.06f;
                t.SetPixel(x, y, c + new Color(noise, noise, noise));
            }
        t.Apply();
        return t;
    }
}
