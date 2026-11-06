using UnityEngine;

/// All audio in the game is synthesized at startup - no audio files.
public class Sfx : MonoBehaviour
{
    public static Sfx I;
    public AudioClip hum, drone, beep, errBeep, chime, stinger, step, ring, whisper, thud, buzz, staticNoise, click;

    AudioSource[] pool;
    int next;
    const int SR = 44100;
    const float TAU = Mathf.PI * 2f;
    static readonly System.Random rng = new System.Random(7);
    static float N() => (float)(rng.NextDouble() * 2.0 - 1.0);

    void Awake()
    {
        I = this;
        Build();
        pool = new AudioSource[14];
        for (int i = 0; i < pool.Length; i++)
        {
            pool[i] = gameObject.AddComponent<AudioSource>();
            pool[i].playOnAwake = false;
        }
    }

    public void Play(AudioClip c, float vol = 1f, float pitch = 1f)
    {
        if (c == null) return;
        var s = pool[next];
        next = (next + 1) % pool.Length;
        s.pitch = pitch;
        s.PlayOneShot(c, vol);
    }

    public void PlayAt(AudioClip c, Vector3 p, float vol = 1f, float pitch = 1f)
    {
        var g = new GameObject("sfx_" + c.name);
        g.transform.position = p;
        var s = g.AddComponent<AudioSource>();
        s.clip = c; s.spatialBlend = 1f; s.volume = vol; s.pitch = pitch;
        s.minDistance = 1.5f; s.maxDistance = 25f; s.rolloffMode = AudioRolloffMode.Linear;
        s.Play();
        Destroy(g, c.length / Mathf.Max(0.1f, pitch) + 0.2f);
    }

    public AudioSource Loop2D(AudioClip c, float vol)
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.clip = c; s.loop = true; s.volume = vol; s.Play();
        return s;
    }

    public AudioSource LoopAt(AudioClip c, Vector3 pos, float vol)
    {
        var g = new GameObject("loop_" + c.name);
        g.transform.position = pos;
        var s = g.AddComponent<AudioSource>();
        s.clip = c; s.loop = true; s.volume = vol; s.spatialBlend = 1f;
        s.minDistance = 1.5f; s.maxDistance = 30f; s.rolloffMode = AudioRolloffMode.Linear;
        s.Play();
        return s;
    }

    static AudioClip Make(string name, float dur, System.Func<int, float, float> f)
    {
        int len = (int)(dur * SR);
        var d = new float[len];
        for (int i = 0; i < len; i++) d[i] = Mathf.Clamp(f(i, (float)i / SR), -1f, 1f);
        var c = AudioClip.Create(name, len, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }

    static float Env(float t, float d) => Mathf.Clamp01(t / 0.005f) * Mathf.Clamp01((d - t) / 0.03f);

    void Build()
    {
        // Fluorescent mains hum (loops cleanly: integer cycles in 2s)
        hum = Make("hum", 2f, (i, t) => 0.25f * Mathf.Sin(TAU * 60 * t) + 0.15f * Mathf.Sin(TAU * 120 * t)
                                       + 0.06f * Mathf.Sin(TAU * 240 * t) + 0.015f * N());

        // Aisle seven drone: detuned low sines + rumbling filtered noise
        float lp = 0;
        drone = Make("drone", 4f, (i, t) =>
        {
            lp += (N() - lp) * 0.02f;
            return 0.3f * Mathf.Sin(TAU * 55 * t) + 0.25f * Mathf.Sin(TAU * 55.5f * t)
                 + 0.14f * Mathf.Sin(TAU * 82.5f * t) * (0.6f + 0.4f * Mathf.Sin(TAU * 0.25f * t))
                 + 0.07f * Mathf.Sin(TAU * 333.25f * t) * Mathf.Max(0, Mathf.Sin(TAU * 0.5f * t))
                 + 0.7f * lp;
        });

        beep = Make("beep", 0.12f, (i, t) => 0.4f * Mathf.Sin(TAU * 1850 * t) * Env(t, 0.12f));
        errBeep = Make("err", 0.42f, (i, t) => 0.25f * Mathf.Sign(Mathf.Sin(TAU * 196 * t)) * ((t < 0.16f || t > 0.24f) ? 1f : 0f) * Env(t, 0.42f));
        chime = Make("chime", 1.3f, (i, t) => t < 0.5f
            ? 0.35f * Mathf.Sin(TAU * 988 * t) * Mathf.Exp(-t * 5) * Env(t, 0.5f)
            : 0.35f * Mathf.Sin(TAU * 784 * t) * Mathf.Exp(-(t - 0.5f) * 4));

        float ph = 0, lp2 = 0;
        stinger = Make("stinger", 1.8f, (i, t) =>
        {
            float f = Mathf.Lerp(1500, 80, Mathf.Sqrt(t / 1.8f));
            ph += TAU * f / SR;
            lp2 += (N() - lp2) * 0.35f;
            float e = Mathf.Exp(-t * 1.4f) * Mathf.Clamp01(t / 0.01f);
            return e * (0.45f * Mathf.Sin(ph) + 0.35f * Mathf.Sin(ph * 1.51f) + 0.25f * Mathf.Sin(ph * 2.03f) + 0.55f * lp2 + 0.3f * Mathf.Sin(TAU * 45 * t));
        });

        float lp3 = 0;
        step = Make("step", 0.11f, (i, t) => { lp3 += (N() - lp3) * 0.15f; return 1.8f * lp3 * Mathf.Exp(-t * 40); });

        ring = Make("ring", 3f, (i, t) =>
        {
            float tt = t % 3f;
            if (tt > 1.6f) return 0f;
            float am = Mathf.Sin(TAU * 20 * t) > 0 ? 1f : 0.15f;
            return 0.16f * (Mathf.Sin(TAU * 440 * t) + Mathf.Sin(TAU * 480 * t)) * am;
        });

        float lp4 = 0, lp5 = 0;
        whisper = Make("whisper", 2f, (i, t) =>
        {
            float n = N();
            lp4 += (n - lp4) * 0.55f; lp5 += (n - lp5) * 0.04f;
            float am = Mathf.Max(0, Mathf.Sin(TAU * 3.1f * t) * Mathf.Sin(TAU * 0.7f * t + 1f));
            return 1.1f * (lp4 - lp5) * am * Env(t, 2f);
        });

        thud = Make("thud", 0.5f, (i, t) => 0.75f * Mathf.Sin(TAU * (55 + 45 * Mathf.Exp(-t * 20)) * t) * Mathf.Exp(-t * 9) + 0.25f * N() * Mathf.Exp(-t * 60));

        float g = 1;
        buzz = Make("buzz", 0.6f, (i, t) =>
        {
            if (i % 441 == 0) g = rng.NextDouble() < 0.55 ? 1f : 0f;
            return (0.14f * Mathf.Sign(Mathf.Sin(TAU * 120 * t)) + 0.06f * N()) * g * Env(t, 0.6f);
        });

        staticNoise = Make("static", 1.2f, (i, t) => 0.5f * N() * Env(t, 1.2f));
        click = Make("click", 0.03f, (i, t) => 0.6f * N() * Mathf.Exp(-t * 200));
    }
}
