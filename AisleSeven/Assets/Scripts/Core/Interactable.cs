using System;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    public Func<string> Prompt;
    public Action OnUse;
    public Func<bool> CanUse;
    public bool Usable => CanUse == null || CanUse();
}

public class ThudOnHit : MonoBehaviour
{
    bool done;
    void OnCollisionEnter(Collision c)
    {
        if (done || c.relativeVelocity.magnitude < 1f) return;
        done = true;
        Sfx.I.PlayAt(Sfx.I.thud, transform.position, 0.8f, UnityEngine.Random.Range(1.3f, 1.7f));
    }
}
