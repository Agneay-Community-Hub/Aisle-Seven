using System.Collections.Generic;
using UnityEngine;

public struct Item
{
    public string name; public float price; public bool found;
    public Item(string n, float p, bool f) { name = n; price = p; found = f; }
}

public class CustomerSpec
{
    public string label = "Customer";
    public readonly List<string> lines = new List<string>();
    public readonly List<Item> items = new List<Item>();
    public string payment = "CASH"; public bool payValid = true;
    public bool faceless, saysName, asksAisle7, uniform, headBack, jitter;
    public int eyes = 2; public float height = 1f;
    public Color shirt, pants, skin, hair;
    public string Line => lines.Count > 0 ? string.Join(" ", lines) : "...";
    public bool HasMissing { get { foreach (var i in items) if (!i.found) return true; return false; } }
    public float Total { get { float t = 0; foreach (var i in items) if (i.found) t += i.price; return t; } }
}

public static class CustomerFactory
{
    public static readonly Color UniformColor = new Color(0.85f, 0.52f, 0.08f);

    static readonly Item[] Stock =
    {
        new Item("Bottled Water", 1.49f, true), new Item("Instant Noodles", 0.99f, true), new Item("Energy Drink", 2.79f, true),
        new Item("Beef Jerky", 5.49f, true), new Item("Sour Cream Chips", 3.29f, true), new Item("AA Batteries", 6.99f, true),
        new Item("Coffee, Large", 1.89f, true), new Item("Microwave Burrito", 2.49f, true), new Item("Aspirin", 4.99f, true),
        new Item("Scratch Ticket", 2.00f, true), new Item("Milk, Half Gallon", 2.69f, true), new Item("Candy Bar", 1.29f, true),
        new Item("Bag of Ice", 3.99f, true), new Item("Motor Oil", 7.49f, true), new Item("Roller Hot Dog", 1.99f, true),
        new Item("Gummy Worms", 2.19f, true), new Item("Phone Charger", 9.99f, true), new Item("White Bread", 2.89f, true),
        new Item("Dog Food", 8.49f, true), new Item("Duct Tape", 4.79f, true), new Item("Cat Litter", 6.29f, true),
        new Item("Frozen Pizza", 5.99f, true), new Item("Hand Warmers", 2.49f, true), new Item("Chewing Gum", 1.19f, true),
    };

    static readonly string[] Missing =
    {
        "Canned Silence", "Tomorrow's Newspaper", "Lungfish Jerky", "Unlabeled Jar (warm)", "Spare Shadow",
        "Mother's Voice, 8 oz", "Aisle 7 Cola", "Last Year, Family Size", "Teeth (assorted)", "A Seventh Thing",
        "Instant Dusk", "Your Missing Sock",
    };

    static readonly string[] GoodPay =
    {
        "CASH - a crumpled twenty", "CASH - exact change, counted twice", "CARD - tap", "CARD - chip",
        "CASH - a fifty", "CARD - swipe (takes three tries)",
    };

    static readonly string[] BadPay =
    {
        "a warm egg", "a handful of baby teeth", "coins dated 2091", "a lock of someone's hair",
        "a Polaroid of you, asleep", "a promise, whispered", "a jar of forty-one moths", "your own wallet",
    };

    static readonly string[] NormalLines =
    {
        "Just this, thanks.", "Cold out there tonight.", "You guys ever close?", "Can I get a bag?",
        "Long day. Long, long day.", "Is the coffee fresh? Doesn't matter.", "Do you sell stamps? No? Okay. Just these.",
        "Pump four. No wait, I walked.", "Sorry, I'm in a hurry.", "Night shift, huh? Rough.", "Weird night. Can't sleep.",
        "Hi. Yeah. Just these.",
    };

    static readonly string[] NameLines =
    {
        "Evening, Riley.", "Long night, Riley?", "They told me you'd be here, Riley.", "Riley. You look tired, Riley.",
        "Remember me, Riley?", "Riley, Riley, Riley... sorry. I like how it sounds.",
    };

    static readonly string[] AisleLines =
    {
        "Which way to aisle seven?", "Is aisle seven open yet?", "My daughter went into aisle seven. Can you check on her?",
        "I'm supposed to be in aisle seven. I'm late.", "Do you restock aisle seven, or does it restock you?",
    };

    static readonly string[] UniformLines =
    {
        "Shift change. Go on, go home.", "I work here. I've always worked here.", "Clock out. I've got the register.",
        "Funny. I don't remember hiring you.", "That vest looks better on me.",
    };

    static readonly string[] FacelessLines =
    {
        "(a soft, polite hum)", "(it nods, where a face would be)", "(it waits patiently, breathing through nothing)",
        "(a wet, friendly clicking)",
    };

    static readonly string[] OddLines =
    {
        "Do you hear the humming? ...Just me, then.", "It's raining inside my car. Anyway, just these.",
        "I come here every night. You're new.", "Don't look at the door. Ha. Kidding.",
        "How long have you worked here? ...Longer than that.", "I'm buying these for a friend who doesn't have hands.",
        "The parking lot was longer tonight. Didn't you notice?", "Nice store. I like how it doesn't end.",
    };

    static readonly Color[] Skins =
    {
        new Color(0.94f, 0.80f, 0.68f), new Color(0.85f, 0.67f, 0.52f), new Color(0.70f, 0.52f, 0.38f),
        new Color(0.52f, 0.37f, 0.26f), new Color(0.38f, 0.26f, 0.18f), new Color(0.98f, 0.86f, 0.76f),
    };

    static T Pick<T>(T[] a) => a[Random.Range(0, a.Length)];

    static CustomerSpec Base()
    {
        var s = new CustomerSpec();
        s.skin = Pick(Skins);
        s.shirt = Color.HSVToRGB(Random.value, Random.Range(0.25f, 0.6f), Random.Range(0.3f, 0.7f));
        s.pants = Color.HSVToRGB(Random.Range(0.55f, 0.7f), Random.Range(0.1f, 0.4f), Random.Range(0.12f, 0.3f));
        s.hair = Color.HSVToRGB(Random.Range(0.02f, 0.1f), Random.Range(0.3f, 0.7f), Random.Range(0.05f, 0.45f));
        s.height = Random.Range(0.93f, 1.07f);
        return s;
    }

    public static CustomerSpec Make(int hour, int index)
    {
        var s = Base();
        int n = Random.Range(1, 4);
        for (int i = 0; i < n; i++) s.items.Add(Pick(Stock));
        s.payment = Pick(GoodPay);
        if (index > 0)
        {
            float pA = new[] { 0.45f, 0.5f, 0.55f, 0.6f, 0.65f, 0.7f, 0.75f }[Mathf.Clamp(hour - 1, 0, 6)];
            if (Random.value < pA)
            {
                int first = ApplyRandom(s, hour, -1);
                if (hour >= 4 && Random.value < 0.3f) ApplyRandom(s, hour, first);
            }
        }
        if (s.lines.Count == 0) s.lines.Add(Pick(NormalLines));
        return s;
    }

    /// For testing / screenshots
    public static CustomerSpec WithEyes(int eyes)
    {
        var s = Base(); s.eyes = eyes; s.items.Add(Pick(Stock)); s.items.Add(new Item(Pick(Missing), 6.66f, false));
        s.payment = Pick(BadPay); s.payValid = false; s.lines.Add(Pick(OddLines)); return s;
    }

    public static CustomerSpec DayShift()
    {
        var s = Base();
        s.uniform = true; s.label = "Day Shift";
        s.skin = new Color(0.85f, 0.67f, 0.52f); s.shirt = new Color(0.3f, 0.3f, 0.32f); s.height = 1f;
        s.lines.Add("Shift change, Riley. Go home.");
        return s;
    }

    static int ApplyRandom(CustomerSpec s, int hour, int exclude)
    {
        var w = new List<(int id, float wt)>();
        w.Add((0, hour == 1 ? 6f : 1.5f));                 // item not in system -> refuse
        if (hour >= 2) w.Add((1, hour == 2 ? 5f : 1.5f));  // bad payment -> refuse
        if (hour >= 3) w.Add((2, hour == 3 ? 5f : 1.5f));  // faceless -> always serve
        if (hour >= 4) w.Add((3, hour == 4 ? 5f : 1.5f));  // knows your name -> refuse
        if (hour >= 5) w.Add((4, hour == 5 ? 5f : 1.5f));  // extra eyes -> refuse
        if (hour >= 6) w.Add((5, hour == 6 ? 5f : 1.5f));  // asks about aisle 7 -> refuse
        if (hour >= 7) w.Add((6, 5f));                     // wears uniform -> refuse
        w.Add((7, hour >= 3 ? 2.5f : 1.5f));               // harmless oddity -> serve
        w.RemoveAll(x => x.id == exclude);
        if (exclude == 2) w.RemoveAll(x => x.id == 4);
        if (exclude == 4) w.RemoveAll(x => x.id == 2);

        float tot = 0; foreach (var x in w) tot += x.wt;
        float r = Random.value * tot; int id = w[w.Count - 1].id;
        foreach (var x in w) { if (r < x.wt) { id = x.id; break; } r -= x.wt; }

        switch (id)
        {
            case 0: AddMissing(s); break;
            case 1: s.payValid = false; s.payment = Pick(BadPay); break;
            case 2:
                s.faceless = true; s.eyes = 0; s.label = "Regular";
                s.lines.Insert(0, Pick(FacelessLines));
                if (Random.value < 0.55f) { if (Random.value < 0.5f) { s.payValid = false; s.payment = Pick(BadPay); } else AddMissing(s); }
                break;
            case 3: s.saysName = true; s.lines.Add(Pick(NameLines)); break;
            case 4: s.faceless = false; s.eyes = Random.Range(3, 7); if (s.label == "Regular") s.label = "Customer"; break;
            case 5: s.asksAisle7 = true; s.lines.Add(Pick(AisleLines)); break;
            case 6: s.uniform = true; s.label = "Employee?"; s.lines.Add(Pick(UniformLines)); break;
            default:
                int k = Random.Range(0, 4);
                if (k == 0) s.height = Random.Range(1.2f, 1.3f);
                else if (k == 1 && hour >= 3) s.headBack = true;
                else if (k == 2) s.jitter = true;
                else { var it = Pick(Stock); s.items.Clear(); for (int i = 0; i < 5; i++) s.items.Add(it); }
                s.lines.Add(Pick(OddLines));
                break;
        }
        return id;
    }

    static void AddMissing(CustomerSpec s)
    {
        var m = new Item(Pick(Missing), Random.Range(1f, 13f), false);
        if (s.items.Count == 0) s.items.Add(m); else s.items[Random.Range(0, s.items.Count)] = m;
    }
}
