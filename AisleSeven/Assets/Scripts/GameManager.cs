using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GState { Title, Intro, Playing, Ending }

public class GameManager : MonoBehaviour
{
    public static GameManager I;
    public static bool DebugNoPause;

    public static readonly Color AmbientBase = new Color(0.07f, 0.08f, 0.085f);
    public const float HourLength = 105f;
    public const int Hours = 7;
    public const int MaxStrikes = 3;
    static readonly int[] TaskHours = { 2, 4, 5, 6, 7 };
    static readonly string[] TaskItems =
    {
        "box marked DO NOT RESTOCK", "jar that knocks back", "name tag that says RILEY", "price gun (still warm)", "mop from yesterday",
    };
    static readonly string[] PhoneLines =
    {
        "Hey, it's Dale. Just checking you found the handbook. TAB. Read it. Every hour. Okay, bye.",
        "Dale again. Forgot to mention - the regulars don't have faces. Don't make it weird. Just serve 'em.",
        "Riley? You didn't tell anybody your name tonight, right? ...Good. Keep it that way.",
        "(slow breathing) ...count them. count them. count them.",
        "It's Dale. If aisle seven seems longer, it is. Walk out backwards. Keep the light on it.",
        "Hi, Riley. It's you. From later. Whatever you do, don't let me take your shift.",
        "(very far away, the sound of someone stocking shelves. It knows you're listening.)",
    };
    static readonly string[] ThanksLines = { "Thanks.", "Cheers. Have a good night.", "Thank you.", "Appreciate it.", "Night." };
    static readonly string[] FacelessThanks = { "(it leaves a smell like old pennies)", "(it bows slightly, then glides away)", "(a satisfied, wet hum)" };
    static readonly string[] RefuseRight = { "...Fine. You'll learn.", "Smart. Smarter than the last one.", "(it smiles much too wide, and backs out the door)", "Oh, you read the handbook. How sweet.", "Another night, then." };
    static readonly string[] RefuseWrong = { "Excuse me?! I want your manager's number.", "Seriously? Unbelievable.", "What did I do? ...Whatever. Freak.", "I'm never shopping here again." };
    static readonly string[] ServeWrong = { "Thank you, Riley. I'll wait for you in aisle seven.", "Mmm. You let me in. You can't take that back.", "Wonderful. Now we're the same.", "See you on the shelves, Riley." };

    public class Note { public string text; public float t; public bool bad; }

    public GState state = GState.Title;
    public float clock;
    public int strikes, served, refused, correct, mistakes, tasksDone, caughtCount;
    public Customer current;
    public bool TransactionOpen, HandbookOpen, sequence, endReady, endingStarted;
    public int scanShown;
    public int rulesHour = 1, seenRulesHour = 1;
    public string Objective = "";
    public string subtitle, speaker;
    public readonly List<Note> notes = new List<Note>();
    public float fade = 1f, fadeTarget = 0f, redFlash, staticAmt;
    public string endTitle = "", endBody = "";
    public bool phoneRinging;

    float scanTimer, subT, phoneT, spawnTimer = 7f, nextEvent = 35f, taskAt = -1f;
    int customerIndex, lastHour, taskCount, phoneHour = -1;
    bool tutorialShown;
    AudioSource ringSrc;
    Store store;

    public int Hour => Mathf.Clamp(1 + (int)(clock / HourLength), 1, Hours);
    // Paused only when the player pressed Esc or a cursor lock that had taken effect was lost.
    // (If the browser refuses pointer lock entirely, the game keeps running instead of soft-locking.)
    bool userPaused, sawLock;
    public bool Paused => state == GState.Playing && !DebugNoPause && userPaused;
    public bool PlayerActive => state == GState.Playing && !Paused && !sequence;
    public bool MovementLocked => TransactionOpen;
    public bool CanRingUp => current != null && current.state == Customer.St.Waiting && !TransactionOpen && PlayerActive;
    public Interactable RegisterI;
    public bool NearRegister
    {
        get
        {
            var p = PlayerController.I.transform.position;
            return p.z < 2.6f && p.x > -6.2f && p.x < -1.6f;
        }
    }
    public bool ScanDone => current != null && scanShown >= current.spec.items.Count && scanTimer <= 0;

    void Awake()
    {
        I = this;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    public void Init(Store s)
    {
        store = s;
        RegisterI = s.RegisterI;
        PlayerController.I.Teleport(Store.PlayerStart, 0f);
        fade = 1f; fadeTarget = 0f;
    }

    // ------------------------------------------------------------------ main loop
    void Update()
    {
        if (TrailerDirector.Active) return;
        float udt = Time.unscaledDeltaTime;
        fade = Mathf.MoveTowards(fade, fadeTarget, udt * 1.2f);
        redFlash = Mathf.MoveTowards(redFlash, 0f, udt * 1.5f);
        staticAmt = Mathf.MoveTowards(staticAmt, 0f, udt * 0.8f);
        for (int i = notes.Count - 1; i >= 0; i--) { notes[i].t -= udt; if (notes[i].t <= 0) notes.RemoveAt(i); }
        if (subT > 0) { subT -= Time.deltaTime; if (subT <= 0) subtitle = null; }

        switch (state)
        {
            case GState.Title:
                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E))
                {
                    state = GState.Intro; Lock(); Sfx.I.Play(Sfx.I.click);
                }
                break;
            case GState.Intro:
                if (Input.GetMouseButtonDown(0)) Lock();
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) StartShift();
                break;
            case GState.Playing:
                if (Cursor.lockState == CursorLockMode.Locked) sawLock = true;
                else if (sawLock) { sawLock = false; userPaused = true; }
                if (Input.GetKeyDown(KeyCode.Escape)) { Unlock(); userPaused = true; sawLock = false; }
                else if (userPaused && Input.GetMouseButtonDown(0)) { Lock(); userPaused = false; }
                bool p = Paused;
                Time.timeScale = p ? 0f : 1f;
                AudioListener.pause = p;
                if (p)
                {
                    if (Input.GetKeyDown(KeyCode.LeftBracket) || Input.GetKeyDown(KeyCode.Minus)) PlayerController.Sensitivity = Mathf.Max(0.4f, PlayerController.Sensitivity - 0.2f);
                    if (Input.GetKeyDown(KeyCode.RightBracket) || Input.GetKeyDown(KeyCode.Equals)) PlayerController.Sensitivity = Mathf.Min(6f, PlayerController.Sensitivity + 0.2f);
                }
                else Tick(Time.deltaTime);
                break;
            case GState.Ending:
                Time.timeScale = 1f; AudioListener.pause = false;
                if (endReady && (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))) Restart();
                break;
        }
    }

    void Tick(float dt)
    {
        if (endingStarted) return;
        clock += dt;
        int h = Hour;
        if (h != lastHour) OnHour(h);
        if (clock >= HourLength * Hours) { clock = HourLength * Hours; StartCoroutine(GoodEnding()); return; }

        var pl = PlayerController.I;
        float fogT = 0.02f + 0.004f * h + (pl.InAisle7 ? 0.03f : 0f);
        RenderSettings.fogDensity = Mathf.MoveTowards(RenderSettings.fogDensity, fogT, dt * 0.02f);
        bool dark = pl.InAisle7 && !Aisle7.I.LightsOn;
        RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, dark ? new Color(0.004f, 0.004f, 0.005f) : AmbientBase, dt * 8f);

        // customers
        if (current == null)
        {
            spawnTimer -= dt;
            if (spawnTimer <= 0 && clock < HourLength * Hours - 25f) SpawnCustomer();
        }
        else if (current.state == Customer.St.Waiting && !TransactionOpen)
        {
            current.patience -= dt;
            if (current.patience < 30f && !current.saidHello)
            {
                current.saidHello = true;
                Say(current.spec.label, current.spec.faceless ? "(a polite, insistent hum from the counter)" : "Hello? ...Anyone working?", 3f);
            }
            if (current.patience <= 0) CustomerIgnored();
        }

        if (TransactionOpen && current != null)
        {
            scanTimer -= dt;
            if (scanShown < current.spec.items.Count && scanTimer <= 0)
            {
                var it = current.spec.items[scanShown++];
                Sfx.I.Play(it.found ? Sfx.I.beep : Sfx.I.errBeep, 0.6f);
                scanTimer = 0.5f;
            }
            if (ScanDone)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) Decide(true);
                else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) Decide(false);
            }
            if (Input.GetKeyDown(KeyCode.Q)) TransactionOpen = false;
        }

        if (taskAt > 0 && clock >= taskAt) { taskAt = -1f; StartTask(); }

        nextEvent -= dt;
        if (nextEvent <= 0)
        {
            RandomEvent(h);
            float k = (h - 1) / 6f;
            nextEvent = Random.Range(Mathf.Lerp(40f, 14f, k), Mathf.Lerp(60f, 26f, k));
        }

        if (phoneRinging) { phoneT -= dt; if (phoneT <= 0) StopPhone(); }

        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.H)) ToggleHandbook();

        Objective = ComputeObjective();
    }

    string ComputeObjective()
    {
        var a = Aisle7.I;
        if (a.HasItem) return "Walk back out of aisle seven. Keep it in sight.";
        if (a.TaskActive) return "Cleanup in aisle seven: retrieve the " + a.ItemName + ".";
        if (current != null && current.state == Customer.St.Waiting)
            return TransactionOpen ? "Check them against the handbook. [1] Ring up  [2] Refuse" : "A customer is waiting at the register.";
        if (phoneRinging) return "The phone is ringing.";
        return "Mind the store. Survive until 6:00 AM.";
    }

    void OnHour(int h)
    {
        lastHour = h;
        rulesHour = h;
        if (h > 1)
        {
            Sfx.I.Play(Sfx.I.chime, 0.6f, 0.5f);
            bool hasNew = false;
            foreach (var r in Rules.All) if (r.hour == h) hasNew = true;
            Notify(ClockString() + (hasNew ? "  -  The handbook has been updated. [TAB]" : ""), false);
            if (Aisle7.I.TaskActive) { Aisle7.I.CancelTask(); AddStrike("You never finished the cleanup in aisle seven."); }
        }
        Aisle7.I.Rebuild(Aisle7.LengthFor(h));
        store.SetCreep(h);
        if (System.Array.IndexOf(TaskHours, h) >= 0) taskAt = clock + Random.Range(14f, 34f);
    }

    // ------------------------------------------------------------------ flow
    public void StartShift()
    {
        state = GState.Playing;
        Lock();
        fadeTarget = 0f;
        clock = 0f; lastHour = 0;
        Notify("11:00 PM. Clocked in. Survive until 6:00 AM.", false);
        Notify("Read the handbook: press TAB.", false);
    }

    public void ToggleHandbook()
    {
        HandbookOpen = !HandbookOpen;
        Sfx.I.Play(Sfx.I.click, 0.5f, 0.7f);
        if (HandbookOpen) seenRulesHour = rulesHour;
    }

    void SpawnCustomer()
    {
        var spec = CustomerFactory.Make(Hour, customerIndex++);
        var go = new GameObject("Customer");
        current = go.AddComponent<Customer>();
        current.Init(spec);
    }

    public void CustomerArrived(Customer c)
    {
        if (c != current) return;
        if (!tutorialShown) { tutorialShown = true; Notify("A customer is at the register. Look at the register and press [E].", false); }
    }

    public void CustomerGone(Customer c)
    {
        if (c != current) return;
        current = null;
        TransactionOpen = false;
        spawnTimer = Random.Range(8f, 22f) * (Hour >= 5 ? 0.8f : 1f);
    }

    public void OpenTransaction()
    {
        if (!CanRingUp) return;
        TransactionOpen = true;
        scanShown = 0; scanTimer = 0.7f;
        Say(current.spec.label, current.spec.Line, 7f);
    }

    void Decide(bool serve)
    {
        TransactionOpen = false;
        var c = current;
        bool shouldRefuse = Rules.ShouldRefuse(c.spec, Hour, out string why);
        bool right = serve != shouldRefuse;
        StartCoroutine(Reaction(c, serve, right, why));
    }

    IEnumerator Reaction(Customer c, bool serve, bool right, string why)
    {
        c.state = Customer.St.Reacting;
        if (serve) { served++; Sfx.I.Play(Sfx.I.beep, 0.5f, 1.3f); Sfx.I.Play(Sfx.I.thud, 0.35f, 2.5f); }
        else refused++;

        if (right)
        {
            correct++;
            string line = serve ? (c.spec.faceless ? Pick(FacelessThanks) : Pick(ThanksLines)) : Pick(RefuseRight);
            Say(c.spec.label, line, 3.5f);
            yield return new WaitForSeconds(1.5f);
            if (c != null) c.Leave();
        }
        else
        {
            mistakes++;
            if (serve)
            {
                Say(c.spec.label, Pick(ServeWrong), 4.5f);
                store.Flicker(2.5f);
                yield return c.TwistHead(1.6f);
                Sfx.I.Play(Sfx.I.stinger, 0.45f);
                redFlash = 0.6f;
                AddStrike("You served something you shouldn't have. " + why);
                yield return new WaitForSeconds(0.8f);
                if (c != null) c.GoToAisle7();
            }
            else
            {
                Say(c.spec.label, Pick(RefuseWrong), 3.5f);
                AddStrike("Customer complaint. " + why);
                yield return new WaitForSeconds(1.2f);
                if (c != null) c.Leave();
            }
        }
    }

    void CustomerIgnored()
    {
        var c = current;
        c.state = Customer.St.Reacting;
        Say(c.spec.label, c.spec.faceless ? "(it waits a moment longer, and then it simply isn't there)" : "Forget it. Nobody's even working here.", 3.5f);
        AddStrike("You left a customer waiting.");
        c.Leave();
    }

    void StartTask()
    {
        string it = TaskItems[taskCount % TaskItems.Length];
        taskCount++;
        Aisle7.I.StartTask(it);
        Sfx.I.Play(Sfx.I.chime, 0.7f, 1.25f);
        Say("PA", "Cleanup in aisle seven. Cleanup in aisle seven.", 5f);
        if (taskCount == 1) Notify("Aisle seven is through the doorway at the end of aisle four. Read rules 4 and 5 first. [TAB]", false);
    }

    public void TaskCompleted(string it)
    {
        tasksDone++;
        Notify("Cleanup complete: " + it + ".", false);
        Say("PA", "Thank you, Riley.", 3f);
    }

    public void AddStrike(string reason)
    {
        if (state != GState.Playing || endingStarted) return;
        strikes++;
        Notify("WRITE-UP " + strikes + "/" + MaxStrikes + ":  " + reason, true);
        redFlash = Mathf.Max(redFlash, 0.5f);
        Sfx.I.Play(Sfx.I.thud, 0.8f, 0.7f);
        if (strikes >= MaxStrikes) StartCoroutine(BadEnding());
    }

    public void Caught()
    {
        if (sequence || endingStarted) return;
        StartCoroutine(CaughtSeq());
    }

    IEnumerator CaughtSeq()
    {
        sequence = true; caughtCount++; TransactionOpen = false; HandbookOpen = false;
        var p = PlayerController.I;
        Aisle7.I.PlaceForScare(p.Cam.transform);
        Sfx.I.Play(Sfx.I.stinger, 1f); Sfx.I.Play(Sfx.I.staticNoise, 0.35f);
        staticAmt = 1f; redFlash = 1f;
        yield return new WaitForSeconds(0.9f);
        fade = 1f; fadeTarget = 1f;
        yield return new WaitForSeconds(0.8f);
        Aisle7.I.ResetAfterCatch();
        p.FlashOn = false;
        p.Teleport(Store.PlayerStart, 0f);
        fadeTarget = 0f;
        sequence = false;
        AddStrike("Something in aisle seven caught you.");
        if (!endingStarted) Say("", "You wake up behind the counter. Your hands smell like old pennies.", 4.5f);
    }

    IEnumerator BadEnding()
    {
        endingStarted = true; sequence = true; TransactionOpen = false; HandbookOpen = false;
        StopPhone();
        yield return new WaitForSeconds(1.2f);
        store.Outage(999f);
        yield return new WaitForSeconds(1.6f);
        Sfx.I.Play(Sfx.I.whisper, 1f, 0.8f);
        Say("???", "Riley. There's room for you in aisle seven.", 3f);
        yield return new WaitForSeconds(2.4f);
        Aisle7.I.PlaceForScare(PlayerController.I.Cam.transform);
        Sfx.I.Play(Sfx.I.stinger, 1f); Sfx.I.Play(Sfx.I.staticNoise, 0.4f);
        staticAmt = 1f; redFlash = 1f;
        yield return new WaitForSeconds(0.8f);
        fade = 1f; fadeTarget = 1f;
        yield return new WaitForSeconds(1.5f);
        endTitle = "REASSIGNED";
        endBody = "Riley has been transferred to Aisle Seven, effective immediately.\nThe shelves don't stock themselves.\n\n" + Stats();
        Finish();
    }

    IEnumerator GoodEnding()
    {
        endingStarted = true; TransactionOpen = false;
        StopPhone();
        if (Aisle7.I.TaskActive) Aisle7.I.CancelTask();
        if (current != null && current.state == Customer.St.Waiting) current.Leave();
        Sfx.I.Play(Sfx.I.chime, 0.8f, 0.5f);
        Notify("6:00 AM", false);
        store.SetDawn();
        Say("PA", "Good morning. The night shift is over. Thank you for shopping at PIK-N-GO.", 5f);
        yield return new WaitForSeconds(5f);
        var go = new GameObject("DayShift");
        var c = go.AddComponent<Customer>();
        c.Init(CustomerFactory.DayShift(), new Vector3(-1.1f, 0, 2.2f));
        yield return new WaitForSeconds(9.5f);
        Say("Day Shift", "Shift change, Riley. Go home. I'll take it from here.", 4f);
        yield return new WaitForSeconds(3.5f);
        sequence = true;
        fadeTarget = 1f;
        yield return new WaitForSeconds(2.5f);
        if (strikes == 0)
        {
            endTitle = "PERFECT SHIFT";
            endBody = "You followed every rule. Dale left a sticky note on the register:\n\"Good work. See you tonight. The handbook is longer now.\"\n\n" + Stats();
        }
        else
        {
            endTitle = "SHIFT COMPLETE";
            endBody = "You clock out at 6:02 AM. You don't remember the drive home.\nWhen you wake up it's 10:45 PM, and you're already wearing the vest.\n\n" + Stats();
        }
        Finish();
    }

    void Finish()
    {
        state = GState.Ending;
        Unlock();
        Time.timeScale = 1f; AudioListener.pause = false;
        endReady = true;
        fade = 1f; fadeTarget = 1f;
    }

    string Stats() =>
        "Customers served: " + served + "    Refused: " + refused + "\n" +
        "Correct calls: " + correct + "    Mistakes: " + mistakes + "\n" +
        "Aisle seven cleanups: " + tasksDone + "    Times caught: " + caughtCount + "\n" +
        "Write-ups: " + strikes + "/" + MaxStrikes;

    void Restart()
    {
        Time.timeScale = 1f; AudioListener.pause = false;
        SceneManager.LoadScene(0);
    }

    // ------------------------------------------------------------------ events
    void RandomEvent(int h)
    {
        var opts = new List<System.Action>();
        opts.Add(() => store.Flicker(Random.Range(0.6f, 1.6f)));
        opts.Add(RingPhone);
        if (h >= 2) opts.Add(() => store.DropProduct());
        if (h >= 3) { opts.Add(() => store.SwapSign()); opts.Add(Whisper); }
        if (h >= 4) { opts.Add(() => store.ShowWindowFigure()); opts.Add(() => store.ShowWindowFigure()); }
        if (h >= 5) opts.Add(() => store.Outage(Random.Range(2f, 4f)));
        opts[Random.Range(0, opts.Count)]();
    }

    void Whisper()
    {
        var p = PlayerController.I.transform;
        Sfx.I.PlayAt(Sfx.I.whisper, p.position - p.forward * 1.4f + Vector3.up * 1.6f, 0.9f, Random.Range(0.8f, 1.1f));
    }

    void RingPhone()
    {
        if (phoneRinging || phoneHour == Hour) { store.Flicker(0.5f); return; }
        phoneHour = Hour;
        phoneRinging = true; phoneT = 15f;
        ringSrc = Sfx.I.LoopAt(Sfx.I.ring, store.PhoneT.position, 0.7f);
    }

    void StopPhone()
    {
        phoneRinging = false;
        if (ringSrc != null) Destroy(ringSrc.gameObject);
        ringSrc = null;
    }

    public void AnswerPhone()
    {
        StopPhone();
        Sfx.I.Play(Sfx.I.click);
        Say("Phone", PhoneLines[Mathf.Clamp(Hour - 1, 0, PhoneLines.Length - 1)], 7f);
    }

    // ------------------------------------------------------------------ ui helpers
    public void Say(string who, string text, float dur) { speaker = who; subtitle = text; subT = dur; }

    public void Notify(string text, bool bad)
    {
        notes.Add(new Note { text = text, t = bad ? 7f : 5.5f, bad = bad });
        while (notes.Count > 4) notes.RemoveAt(0);
    }

    public string ClockString()
    {
        int mins = 23 * 60 + (int)(clock / HourLength * 60f);
        mins %= 24 * 60;
        int hh = mins / 60, mm = mins % 60;
        string ampm = hh >= 12 ? "PM" : "AM";
        int h12 = hh % 12; if (h12 == 0) h12 = 12;
        return h12 + ":" + mm.ToString("00") + " " + ampm;
    }

    static T Pick<T>(T[] a) => a[Random.Range(0, a.Length)];

    void Lock() { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    void Unlock() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }

    // ------------------------------------------------------------------ debug (screenshot automation)
    public void DebugStart() { DebugNoPause = true; StartShift(); }
    public void DebugSetClock(float c) { clock = c; }
    public void DebugCustomer(CustomerSpec s)
    {
        if (current != null) Destroy(current.gameObject);
        var go = new GameObject("Customer");
        current = go.AddComponent<Customer>();
        current.Init(s);
        current.PlaceAtCounter();
    }
    public void DebugOpenTransaction() { OpenTransaction(); scanShown = current.spec.items.Count; scanTimer = 0f; }
}
