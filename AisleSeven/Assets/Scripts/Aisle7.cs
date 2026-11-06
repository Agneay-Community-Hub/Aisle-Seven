using System.Collections.Generic;
using UnityEngine;

/// Aisle seven: a corridor behind the back wall that gets longer every hour, and the
/// thing at the end of it, which only moves while it isn't seen.
public class Aisle7 : MonoBehaviour
{
    public static Aisle7 I;
    public const float EntranceZ = 20f;
    public const int Layer = 8; // aisle seven geometry: lit only by its own lights

    static void SetLayer(Transform t)
    {
        t.gameObject.layer = Layer;
        foreach (Transform c in t) SetLayer(c);
    }

    public float Length { get; private set; }
    public bool TaskActive, HasItem;
    public string ItemName = "";
    public bool LightsOn = true;
    public float Proximity;

    static readonly Color FixtureCol = new Color(0.85f, 1f, 0.65f);
    Transform geo, entity;
    GameObject item;
    readonly List<Light> lights = new List<Light>();
    readonly List<Material> fixtures = new List<Material>();
    AudioSource drone;
    bool entityActive, scaring;
    Vector3 entityPos;
    float blackoutT, nextBlackout = 6f, sprintTick, heartT, pendingLen = -1f;

    void Awake()
    {
        I = this;
        BuildEntity();
        BuildItem();
        drone = Sfx.I.Loop2D(Sfx.I.drone, 0f);
        DoBuild(LengthFor(1));
    }

    // Aisle seven SHIFTS: it is longer every hour. Multiples of 4 keep the floor tiles seamless.
    static readonly float[] Lengths = { 12f, 16f, 20f, 24f, 32f, 36f, 44f };
    public static float LengthFor(int hour) => Lengths[Mathf.Clamp(hour - 1, 0, Lengths.Length - 1)];

    public void Rebuild(float len)
    {
        if (Mathf.Approximately(len, Length)) return;
        if (PlayerController.I != null && PlayerController.I.transform.position.z > EntranceZ - 0.5f) { pendingLen = len; return; }
        DoBuild(len);
    }

    void DoBuild(float len)
    {
        if (geo != null) Destroy(geo.gameObject);
        lights.Clear(); fixtures.Clear();
        Length = len;
        geo = new GameObject("Aisle7Geo").transform; geo.SetParent(transform, false);
        var st = new GameObject("Static").transform; st.SetParent(geo, false);
        float z0 = EntranceZ, z1 = EntranceZ + len, zc = (z0 + z1) * 0.5f;

        var floor = Mat.Textured(Store.Checker, new Vector2(2.4f, 2f), new Color(0.85f, 0.82f, 0.62f), 0.6f);
        var wall = Mat.Lit(new Color(0.5f, 0.52f, 0.42f));
        var metal = Mat.Lit(new Color(0.5f, 0.5f, 0.48f), 0.4f);
        var dark = Mat.Lit(new Color(0.07f, 0.07f, 0.06f));
        Material[] jars = { Mat.Lit(new Color(0.78f, 0.7f, 0.55f), 0.6f), Mat.Lit(new Color(0.72f, 0.6f, 0.58f), 0.6f), Mat.Lit(new Color(0.82f, 0.8f, 0.7f), 0.6f) };

        Geo.Slab("Floor", st, new Vector3(0, -0.05f, zc), new Vector3(4.8f, 0.1f, len), floor, true, 5f, 4f);
        Geo.Slab("Ceiling", st, new Vector3(0, 3.05f, zc), new Vector3(4.8f, 0.1f, len), Mat.Lit(new Color(0.25f, 0.25f, 0.22f)), false, 5f, 4f);
        Geo.Slab("WallL", st, new Vector3(-2.35f, 1.5f, zc), new Vector3(0.1f, 3.1f, len), wall, true, 5f, 4f);
        Geo.Slab("WallR", st, new Vector3(2.35f, 1.5f, zc), new Vector3(0.1f, 3.1f, len), wall, true, 5f, 4f);
        Geo.Box("EndWall", st, new Vector3(0, 1.5f, z1 + 0.05f), new Vector3(4.8f, 3.1f, 0.1f), wall);
        Geo.Box("Lintel", st, new Vector3(0, 2.85f, z0 + 0.1f), new Vector3(4.8f, 0.5f, 0.1f), wall, false);

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * 1.75f;
            Geo.Slab("Base", st, new Vector3(x, 0.08f, zc), new Vector3(0.7f, 0.16f, len), dark, true, 5f, 4f);
            Geo.Slab("Back", st, new Vector3(side * 2.2f, 1.1f, zc), new Vector3(0.06f, 2.2f, len), metal, false, 5f, 4f);
            foreach (float y in new[] { 0.75f, 1.35f, 1.95f })
                Geo.Slab("Board", st, new Vector3(x, y, zc), new Vector3(0.7f, 0.035f, len), metal, false, 5f, 4f);
            float[] levels = { 0.16f, 0.765f, 1.365f };
            foreach (float y in levels)
                for (float z = z0 + 0.3f; z < z1 - 0.3f; z += 0.42f)
                {
                    float depth = (z - z0) / len;
                    if (Random.value > 0.92f - 0.75f * depth) continue;
                    float h = Random.Range(0.16f, 0.26f);
                    Geo.Prim(PrimitiveType.Cylinder, "Jar", st, new Vector3(x + Random.Range(-0.12f, 0.12f), y + h + 0.01f, z),
                        new Vector3(0.17f, h, 0.17f), jars[Random.Range(0, jars.Length)], false);
                }
        }

        for (float z = z0 + 2f; z < z1 - 0.5f; z += 4f)
        {
            var m = Mat.Glow(FixtureCol, 1.3f, true);
            Geo.Box("Fixture", geo, new Vector3(0, 2.98f, z), new Vector3(0.35f, 0.04f, 1.6f), m, false);
            fixtures.Add(m);
            var lg = new GameObject("A7Light"); lg.transform.SetParent(geo, false); lg.transform.localPosition = new Vector3(0, 2.7f, z);
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 6.5f; l.intensity = 1.0f; l.color = FixtureCol;
            l.cullingMask = 1 << Layer;
            lights.Add(l);
        }
        SetLayer(geo);
        SetLights(blackoutT <= 0);
        StaticBatchingUtility.Combine(st.gameObject);
        if (TaskActive && !HasItem) PlaceItem();
    }

    void BuildEntity()
    {
        entity = new GameObject("TheStocker").transform; entity.SetParent(transform, false);
        var black = Mat.Unlit(new Color(0.012f, 0.012f, 0.014f));
        var white = Mat.Unlit(new Color(0.95f, 0.95f, 0.9f));
        Geo.Prim(PrimitiveType.Capsule, "Body", entity, new Vector3(0, 1.4f, 0), new Vector3(0.42f, 1.0f, 0.3f), black, false);
        Geo.Box("LegL", entity, new Vector3(-0.1f, 0.45f, 0), new Vector3(0.09f, 0.9f, 0.09f), black, false);
        Geo.Box("LegR", entity, new Vector3(0.1f, 0.45f, 0), new Vector3(0.09f, 0.9f, 0.09f), black, false);
        Geo.Prim(PrimitiveType.Capsule, "ArmL", entity, new Vector3(-0.3f, 1.3f, 0.05f), new Vector3(0.08f, 0.8f, 0.08f), black, false);
        Geo.Prim(PrimitiveType.Capsule, "ArmR", entity, new Vector3(0.3f, 1.3f, 0.05f), new Vector3(0.08f, 0.8f, 0.08f), black, false);
        var head = new GameObject("Head").transform; head.SetParent(entity, false); head.localPosition = new Vector3(0, 2.6f, 0);
        Geo.Prim(PrimitiveType.Sphere, "Skull", head, Vector3.zero, new Vector3(0.32f, 0.42f, 0.32f), black, false);
        Geo.Prim(PrimitiveType.Sphere, "EyeL", head, new Vector3(-0.07f, 0.05f, 0.145f), new Vector3(0.05f, 0.035f, 0.02f), white, false);
        Geo.Prim(PrimitiveType.Sphere, "EyeR", head, new Vector3(0.07f, 0.05f, 0.145f), new Vector3(0.05f, 0.035f, 0.02f), white, false);
        for (int i = 0; i < 7; i++)
        {
            float t = (i - 3) / 3f;
            var tooth = Geo.Box("Smile", head, new Vector3(t * 0.11f, -0.08f + t * t * 0.035f, 0.15f - Mathf.Abs(t) * 0.025f), new Vector3(0.032f, 0.012f, 0.01f), white, false);
            tooth.transform.localRotation = Quaternion.Euler(0, 0, t * 25f);
        }
        entity.gameObject.SetActive(false);
    }

    void BuildItem()
    {
        item = new GameObject("TaskItem");
        item.transform.SetParent(transform, false);
        Geo.Box("Box", item.transform, new Vector3(0, 0.16f, 0), new Vector3(0.45f, 0.32f, 0.45f), Mat.Lit(new Color(0.55f, 0.4f, 0.25f)));
        Geo.Box("Label", item.transform, new Vector3(0, 0.2f, -0.226f), new Vector3(0.3f, 0.08f, 0.01f), Mat.Glow(new Color(1f, 0.2f, 0.15f), 1.5f), false);
        SetLayer(item.transform);
        var it = item.AddComponent<Interactable>();
        it.Prompt = () => "[E] Pick up the " + ItemName;
        it.CanUse = () => TaskActive && !HasItem;
        it.OnUse = () =>
        {
            HasItem = true;
            item.SetActive(false);
            Sfx.I.Play(Sfx.I.thud, 0.5f, 1.4f);
            nextBlackout = Mathf.Min(nextBlackout, 1.6f);
            GameManager.I.Notify("Got it. Now get out. Walk backwards - keep it in sight.", false);
        };
        item.SetActive(false);
    }

    void PlaceItem() { item.transform.localPosition = new Vector3(0, 0, EntranceZ + Length - 2.8f); }

    public void StartTask(string name)
    {
        TaskActive = true; HasItem = false; ItemName = name;
        PlaceItem(); item.SetActive(true);
    }

    public void CancelTask() { TaskActive = false; HasItem = false; item.SetActive(false); }

    void SetLights(bool on)
    {
        LightsOn = on;
        foreach (var l in lights) l.enabled = on;
        foreach (var m in fixtures) m.SetColor("_EmissionColor", on ? FixtureCol * 1.3f : Color.black);
    }

    public void ForceBlackout(float d)
    {
        blackoutT = Mathf.Max(blackoutT, d);
        SetLights(false);
    }

    void Update()
    {
        var gm = GameManager.I; var p = PlayerController.I;
        if (gm == null || p == null) return;
        float dt = Time.deltaTime;
        bool playing = gm.state == GState.Playing;
        bool inside = playing && p.InAisle7;
        float de = Vector2.Distance(new Vector2(p.transform.position.x, p.transform.position.z), new Vector2(0, EntranceZ));
        float target = !playing ? 0f : inside ? 0.6f : Mathf.Clamp01(1f - de / 16f) * 0.3f;
        drone.volume = Mathf.MoveTowards(drone.volume, target, Time.unscaledDeltaTime * 0.6f);
        if (!playing || scaring) return;

        if (pendingLen > 0 && p.transform.position.z < EntranceZ - 0.5f) { DoBuild(pendingLen); pendingLen = -1f; }

        if (blackoutT > 0) { blackoutT -= dt; if (blackoutT <= 0) SetLights(true); }
        else if (inside)
        {
            nextBlackout -= dt;
            if (nextBlackout <= 0)
            {
                int h = gm.Hour;
                blackoutT = Random.Range(0.6f, 1.1f) + 0.12f * h;
                SetLights(false);
                Sfx.I.Play(Sfx.I.buzz, 0.45f);
                float k = Mathf.Clamp01((h - 1) / 6f);
                nextBlackout = Random.Range(Mathf.Lerp(10f, 3.5f, k), Mathf.Lerp(15f, 6.5f, k));
            }
        }

        if (inside) { if (!entityActive) SpawnEntity(); UpdateEntity(dt, p, gm); }
        else if (entityActive) { entityActive = false; entity.gameObject.SetActive(false); Proximity = 0f; }

        if (HasItem && p.transform.position.z < EntranceZ - 0.2f)
        {
            HasItem = false; TaskActive = false;
            gm.TaskCompleted(ItemName);
        }
    }

    void SpawnEntity()
    {
        entityActive = true;
        entity.gameObject.SetActive(true);
        entityPos = new Vector3(0, 0, EntranceZ + Length - 0.9f);
        entity.localPosition = entityPos;
        nextBlackout = Mathf.Max(nextBlackout, Random.Range(4f, 7f));
    }

    bool Seen(PlayerController p)
    {
        Transform cam = p.Cam.transform;
        Vector3 head = entityPos + Vector3.up * 1.7f;
        Vector3 d = head - cam.position; float dist = d.magnitude;
        if (dist < 0.01f) return true;
        float dot = Vector3.Dot(cam.forward, d / dist);
        bool lit = LightsOn || (p.FlashOn && dot > 0.9f && dist < p.Flash.range);
        return dot > 0.6f && lit;
    }

    void UpdateEntity(float dt, PlayerController p, GameManager gm)
    {
        int h = gm.Hour;
        Vector3 pp = p.transform.position; pp.y = 0;
        Vector3 to = pp - entityPos; to.y = 0;
        float dist = to.magnitude;
        Vector3 dir = dist > 0.001f ? to / dist : Vector3.back;

        if (!Seen(p)) entityPos += dir * Mathf.Min(dist, (1.4f + 0.38f * h) * dt);

        if (p.Sprinting)
        {
            sprintTick -= dt;
            if (sprintTick <= 0)
            {
                sprintTick = 0.7f;
                entityPos += dir * Mathf.Min(Mathf.Max(0f, dist - 0.5f), 3f);
                ForceBlackout(0.3f);
                Sfx.I.Play(Sfx.I.whisper, 0.7f, 1.2f);
                gm.Say("", "(something whispers: \"...don't shift...\")", 2f);
            }
        }
        else sprintTick = 0.15f;

        entityPos.x = Mathf.Clamp(entityPos.x, -1f, 1f);
        entityPos.z = Mathf.Clamp(entityPos.z, EntranceZ + 0.6f, EntranceZ + Length - 0.6f);
        entity.localPosition = entityPos;
        if (dist > 0.01f) entity.localRotation = Quaternion.LookRotation(dir);

        dist = Vector3.Distance(pp, entityPos);
        Proximity = Mathf.Clamp01(1f - (dist - 1.2f) / 10f);
        if (dist < 12f)
        {
            heartT -= dt;
            if (heartT <= 0)
            {
                heartT = Mathf.Lerp(1.2f, 0.33f, Proximity);
                Sfx.I.Play(Sfx.I.thud, 0.25f + 0.6f * Proximity, 0.9f);
            }
        }
        if (dist < 1.3f) gm.Caught();
    }

    public void PlaceForScare(Transform cam)
    {
        scaring = true;
        entity.gameObject.SetActive(true);
        Vector3 f = cam.forward; f.y = 0;
        if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
        f.Normalize();
        entity.position = new Vector3(cam.position.x, cam.position.y - 2.6f, cam.position.z) + f * 0.6f;
        entity.rotation = Quaternion.LookRotation(-f);
    }

    public void ResetAfterCatch()
    {
        scaring = false; entityActive = false;
        entity.gameObject.SetActive(false);
        Proximity = 0f;
        if (HasItem) { HasItem = false; PlaceItem(); item.SetActive(true); }
        blackoutT = 0f; SetLights(true);
    }

    /// Screenshot helper: put the entity somewhere specific.
    public void DebugEntityAt(float z) { entityActive = true; entity.gameObject.SetActive(true); entityPos = new Vector3(0, 0, z); entity.localPosition = entityPos; entity.localRotation = Quaternion.LookRotation(Vector3.back); }
}
