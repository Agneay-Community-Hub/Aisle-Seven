using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Customer : MonoBehaviour
{
    public enum St { Entering, Waiting, Reacting, Leaving, ToAisle }
    public St state;
    public CustomerSpec spec;
    public float patience = 75f;
    public bool saidHello;

    public static readonly Vector3 CounterSpot = new Vector3(-4f, 0f, 4.25f);
    static readonly Vector3 Outside = new Vector3(6f, 0f, -5f);

    List<Vector3> path = new List<Vector3>();
    int pi;
    float speed = 1.4f, bobT, prevZ;
    Transform vis, headPivot;
    Vector3 headBase;
    bool twisting;

    void OnEnable() { AutoDoor.Watchers.Add(transform); }
    void OnDestroy()
    {
        AutoDoor.Watchers.Remove(transform);
        if (GameManager.I != null) GameManager.I.CustomerGone(this);
    }

    public void Init(CustomerSpec s, Vector3? target = null)
    {
        spec = s;
        transform.position = Outside;
        transform.rotation = Quaternion.LookRotation(Vector3.forward);
        prevZ = Outside.z;
        Build();
        path = new List<Vector3> { new Vector3(6f, 0, -1.2f), new Vector3(6f, 0, 1.8f), new Vector3(-1.5f, 0, 4.7f), target ?? CounterSpot };
        pi = 0;
        state = St.Entering;
    }

    /// Debug/screenshot helper: skip the walk.
    public void PlaceAtCounter()
    {
        transform.position = CounterSpot;
        transform.rotation = Quaternion.LookRotation(Vector3.back);
        pi = path.Count;
        state = St.Waiting;
    }

    public void Leave()
    {
        state = St.Leaving; speed = 1.4f;
        path = new List<Vector3> { new Vector3(-1.5f, 0, 4.7f), new Vector3(6f, 0, 1.8f), new Vector3(6f, 0, -1.2f), new Vector3(6f, 0, -9f) };
        pi = 0;
    }

    public void GoToAisle7()
    {
        state = St.ToAisle; speed = 0.9f;
        path = new List<Vector3> { new Vector3(-0.5f, 0, 5.6f), new Vector3(0f, 0, 19.5f), new Vector3(0f, 0, 26.5f) };
        pi = 0;
    }

    void Update()
    {
        if (state == St.Entering || state == St.Leaving || state == St.ToAisle) Walk();
    }

    void Walk()
    {
        if (pi >= path.Count) return;
        Vector3 target = path[pi];
        Vector3 d = target - transform.position; d.y = 0;
        float step = speed * Time.deltaTime;
        if (d.magnitude <= step)
        {
            transform.position = target;
            pi++;
            if (pi >= path.Count) { Arrive(); return; }
        }
        else
        {
            transform.position += d.normalized * step;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d.normalized), Time.deltaTime * 6f);
        }
        // door chime when crossing the threshold
        float z = transform.position.z;
        if (Mathf.Abs(transform.position.x - 6f) < 1.5f && (prevZ < 0f) != (z < 0f))
            Sfx.I.PlayAt(Sfx.I.chime, Store.DoorPos, 0.7f);
        prevZ = z;
        bobT += Time.deltaTime * 9f;
        vis.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(bobT)) * 0.03f, 0);
    }

    void Arrive()
    {
        if (state == St.Entering)
        {
            state = St.Waiting;
            vis.localPosition = Vector3.zero;
            StartCoroutine(TurnTo(Quaternion.LookRotation(Vector3.back)));
            if (GameManager.I != null) GameManager.I.CustomerArrived(this);
        }
        else Destroy(gameObject);
    }

    IEnumerator TurnTo(Quaternion q)
    {
        Quaternion a = transform.rotation; float t = 0;
        while (t < 1f) { t += Time.deltaTime * 2.5f; transform.rotation = Quaternion.Slerp(a, q, t); yield return null; }
    }

    public IEnumerator TwistHead(float dur)
    {
        twisting = true;
        Quaternion a = headPivot.localRotation; float t = 0;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0, 1, t / dur);
            headPivot.localRotation = a * Quaternion.Euler(0, 180f * k, 10f * Mathf.Sin(t * 30f) * (1 - k));
            yield return null;
        }
    }

    void LateUpdate()
    {
        if (headPivot == null || twisting) return;
        Quaternion target = transform.rotation;
        if ((state == St.Waiting || state == St.Reacting) && PlayerController.I != null)
        {
            Vector3 dir = PlayerController.I.Cam.transform.position - headPivot.position;
            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion look = Quaternion.LookRotation(dir);
                if (Quaternion.Angle(transform.rotation, look) < 80f) target = look;
            }
        }
        if (spec.headBack) target *= Quaternion.Euler(0, 180f, 0);
        headPivot.rotation = Quaternion.Slerp(headPivot.rotation, target, Time.deltaTime * 4f);
        headPivot.localPosition = headBase + (spec.jitter && Random.value < 0.04f ? Random.insideUnitSphere * 0.07f : Vector3.zero);
    }

    // ---------------------------------------------------------------- visuals
    static readonly Vector2[] EyeSpots =
    {
        new Vector2(-0.045f, 0.17f), new Vector2(0.045f, 0.17f), new Vector2(0f, 0.228f),
        new Vector2(-0.078f, 0.118f), new Vector2(0.078f, 0.118f), new Vector2(-0.082f, 0.215f), new Vector2(0.082f, 0.215f),
    };

    static float FaceZ(float x, float y)
    {
        // head ellipsoid: radii (0.12, 0.145, 0.125), centre (0, 0.15, 0)
        float u = x / 0.12f, v = (y - 0.15f) / 0.145f;
        return 0.125f * Mathf.Sqrt(Mathf.Max(0.05f, 1f - u * u - v * v));
    }

    void Build()
    {
        vis = new GameObject("Vis").transform; vis.SetParent(transform, false);
        var body = new GameObject("Body").transform; body.SetParent(vis, false);
        body.localScale = new Vector3(1f, spec.height, 1f);

        Material shirt = Mat.Lit(spec.shirt), pants = Mat.Lit(spec.pants), skin = Mat.Lit(spec.faceless ? new Color(0.88f, 0.86f, 0.82f) : spec.skin, 0.3f);
        Geo.Box("Legs", body, new Vector3(0, 0.43f, 0), new Vector3(0.36f, 0.86f, 0.22f), pants, false);
        Geo.Prim(PrimitiveType.Capsule, "Torso", body, new Vector3(0, 1.17f, 0), new Vector3(0.46f, 0.42f, 0.28f), shirt, false);
        Geo.Prim(PrimitiveType.Capsule, "ArmL", body, new Vector3(-0.29f, 1.12f, 0), new Vector3(0.11f, 0.36f, 0.11f), shirt, false);
        Geo.Prim(PrimitiveType.Capsule, "ArmR", body, new Vector3(0.29f, 1.12f, 0), new Vector3(0.11f, 0.36f, 0.11f), shirt, false);
        Geo.Prim(PrimitiveType.Sphere, "HandL", body, new Vector3(-0.3f, 0.75f, 0), Vector3.one * 0.09f, skin, false);
        Geo.Prim(PrimitiveType.Sphere, "HandR", body, new Vector3(0.3f, 0.75f, 0), Vector3.one * 0.09f, skin, false);
        if (spec.uniform)
        {
            Geo.Box("Vest", body, new Vector3(0, 1.2f, 0), new Vector3(0.5f, 0.5f, 0.31f), Mat.Lit(CustomerFactory.UniformColor), false);
            Geo.Box("Stripe", body, new Vector3(0, 1.05f, 0), new Vector3(0.51f, 0.05f, 0.315f), Mat.Lit(new Color(0.7f, 0.08f, 0.06f)), false);
            Geo.Box("Tag", body, new Vector3(0.12f, 1.32f, 0.158f), new Vector3(0.13f, 0.05f, 0.01f), Mat.Lit(Color.white), false);
            Geo.Text(body, "RILEY", new Vector3(0.12f, 1.32f, 0.166f), 0.032f, Color.black, Quaternion.Euler(0, 180f, 0));
        }

        headPivot = new GameObject("HeadPivot").transform; headPivot.SetParent(vis, false);
        headPivot.localPosition = new Vector3(0, 1.6f * spec.height, 0);
        headBase = headPivot.localPosition;
        Geo.Prim(PrimitiveType.Sphere, "Head", headPivot, new Vector3(0, 0.15f, 0), new Vector3(0.24f, 0.29f, 0.25f), skin, false);
        if (!spec.faceless)
        {
            Geo.Prim(PrimitiveType.Sphere, "Hair", headPivot, new Vector3(0, 0.235f, -0.025f), new Vector3(0.255f, 0.17f, 0.265f), Mat.Lit(spec.hair), false);
            var white = Mat.Lit(new Color(0.95f, 0.94f, 0.9f), 0.6f); var black = Mat.Unlit(new Color(0.02f, 0.02f, 0.02f));
            int n = Mathf.Clamp(spec.eyes, 0, EyeSpots.Length);
            for (int i = 0; i < n; i++)
            {
                var e = EyeSpots[i]; float z = FaceZ(e.x, e.y);
                Geo.Prim(PrimitiveType.Sphere, "Eye", headPivot, new Vector3(e.x, e.y, z - 0.004f), Vector3.one * 0.052f, white, false);
                Geo.Prim(PrimitiveType.Sphere, "Pupil", headPivot, new Vector3(e.x, e.y, z + 0.016f), Vector3.one * 0.026f, black, false);
            }
            Geo.Box("Mouth", headPivot, new Vector3(0, 0.075f, FaceZ(0, 0.075f)), new Vector3(0.065f, 0.012f, 0.012f), Mat.Lit(new Color(0.35f, 0.1f, 0.1f)), false);
        }

        var cap = gameObject.AddComponent<CapsuleCollider>();
        cap.center = new Vector3(0, 0.9f * spec.height, 0); cap.height = 1.8f * spec.height; cap.radius = 0.28f;
    }
}

/// Sliding automatic doors that open for anyone except the player.
public class AutoDoor : MonoBehaviour
{
    public static readonly List<Transform> Watchers = new List<Transform>();
    public Transform left, right;
    Vector3 lc, rc;
    float open;

    void Start() { lc = left.localPosition; rc = right.localPosition; }

    void Update()
    {
        Watchers.RemoveAll(t => t == null);
        bool near = false;
        foreach (var t in Watchers)
        {
            Vector3 d = t.position - transform.position; d.y = 0;
            if (d.sqrMagnitude < 9f) near = true;
        }
        open = Mathf.MoveTowards(open, near ? 1f : 0f, Time.deltaTime * 1.8f);
        left.localPosition = lc + Vector3.left * 0.95f * open;
        right.localPosition = rc + Vector3.right * 0.95f * open;
    }
}
