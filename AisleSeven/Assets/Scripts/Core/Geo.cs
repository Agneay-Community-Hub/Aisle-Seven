using UnityEngine;

/// Tiny primitive-building helpers. Everything in the game is built from these at runtime.
public static class Geo
{
    static Font font;
    public static Font Font
    {
        get
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font;
        }
    }

    public static GameObject Prim(PrimitiveType t, string name, Transform parent, Vector3 pos, Vector3 scale, Material m, bool collider = true)
    {
        var g = GameObject.CreatePrimitive(t);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        var r = g.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        if (!collider) Object.Destroy(g.GetComponent<Collider>());
        return g;
    }

    public static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 scale, Material m, bool collider = true)
        => Prim(PrimitiveType.Cube, name, parent, pos, scale, m, collider);

    /// A large box split into tiles. Forward rendering picks the most important lights per renderer,
    /// so big surfaces must be split up or distant lights never reach them.
    public static void Slab(string name, Transform parent, Vector3 center, Vector3 size, Material m, bool collider = true, float maxX = 4f, float maxZ = 4f)
    {
        int nx = Mathf.Max(1, Mathf.CeilToInt(size.x / maxX - 0.001f));
        int nz = Mathf.Max(1, Mathf.CeilToInt(size.z / maxZ - 0.001f));
        float sx = size.x / nx, sz = size.z / nz;
        for (int i = 0; i < nx; i++)
            for (int k = 0; k < nz; k++)
                Box(name, parent, new Vector3(center.x - size.x * 0.5f + sx * (i + 0.5f), center.y, center.z - size.z * 0.5f + sz * (k + 0.5f)),
                    new Vector3(sx, size.y, sz), m, collider);
    }

    /// World-space text. Readable when viewed from the object's -Z side (default orientation).
    public static TextMesh Text(Transform parent, string s, Vector3 pos, float height, Color c, Quaternion? rot = null)
    {
        var g = new GameObject("Text_" + s);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localRotation = rot ?? Quaternion.identity;
        var tm = g.AddComponent<TextMesh>();
        tm.font = Font;
        tm.fontSize = 64;
        tm.characterSize = height * 0.16f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = c;
        tm.text = s;
        g.GetComponent<MeshRenderer>().sharedMaterial = Font.material;
        return tm;
    }
}
