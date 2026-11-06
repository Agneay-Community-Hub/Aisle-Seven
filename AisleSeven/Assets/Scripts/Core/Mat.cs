using System.Collections.Generic;
using UnityEngine;

/// Material factory. Base materials live in Resources/Mats (created by the editor setup
/// script) so their shader variants are guaranteed to ship in WebGL/standalone builds.
public static class Mat
{
    static Material lit, glow, unlit, glass;
    static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

    static void Init()
    {
        if (lit != null) return;
        lit = Resources.Load<Material>("Mats/Lit");
        glow = Resources.Load<Material>("Mats/Glow");
        unlit = Resources.Load<Material>("Mats/Unlit");
        glass = Resources.Load<Material>("Mats/Glass");
        if (lit == null) lit = new Material(Shader.Find("Standard"));
        if (glow == null) { glow = new Material(lit); glow.EnableKeyword("_EMISSION"); }
        if (unlit == null) unlit = new Material(Shader.Find("Unlit/Color"));
        if (glass == null) glass = new Material(lit);
    }

    public static Material Lit(Color c, float gloss = 0.15f)
    {
        Init();
        string k = "L" + c + gloss;
        if (cache.TryGetValue(k, out var m)) return m;
        m = new Material(lit) { color = c };
        m.SetFloat("_Glossiness", gloss);
        cache[k] = m;
        return m;
    }

    public static Material Textured(Texture tex, Vector2 tiling, Color tint, float gloss = 0.3f)
    {
        Init();
        var m = new Material(lit) { color = tint, mainTexture = tex };
        m.mainTextureScale = tiling;
        m.SetFloat("_Glossiness", gloss);
        return m;
    }

    public static Material Glow(Color c, float intensity = 1.5f, bool unique = false)
    {
        Init();
        string k = "G" + c + intensity;
        if (!unique && cache.TryGetValue(k, out var m)) return m;
        m = new Material(glow) { color = c * 0.4f };
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        m.SetColor("_EmissionColor", c * intensity);
        if (!unique) cache[k] = m;
        return m;
    }

    public static Material Unlit(Color c)
    {
        Init();
        string k = "U" + c;
        if (cache.TryGetValue(k, out var m)) return m;
        m = new Material(unlit) { color = c };
        cache[k] = m;
        return m;
    }

    public static Material Glass(Color c)
    {
        Init();
        string k = "T" + c;
        if (cache.TryGetValue(k, out var m)) return m;
        m = new Material(glass) { color = c };
        cache[k] = m;
        return m;
    }
}
