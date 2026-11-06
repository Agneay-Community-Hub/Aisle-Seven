/// The night staff handbook. New rules appear each hour - the rules SHIFT.
/// When two rules disagree, the higher number wins; ShouldRefuse evaluates top-down.
public static class Rules
{
    public struct Rule
    {
        public int num, hour; public string text;
        public Rule(int n, int h, string t) { num = n; hour = h; text = t; }
    }

    public const string Header = "When two rules disagree, the HIGHER number wins.";

    public static readonly Rule[] All =
    {
        new Rule(1, 1, "Serve every customer. Be quick. Be polite."),
        new Rule(2, 1, "If the scanner reads ITEM NOT IN SYSTEM, refuse the sale. We do not stock it. We never have."),
        new Rule(3, 2, "Accept CASH or CARD only. Nothing else. No matter how warm it is."),
        new Rule(4, 2, "When the PA calls a cleanup in aisle seven, go. WALK. Never SHIFT (sprint) in aisle seven."),
        new Rule(5, 2, "What stands at the end of aisle seven only moves when it is not seen. Keep your eyes on it. Keep the lights on it."),
        new Rule(6, 3, "Customers without faces are regulars. ALWAYS serve them, whatever they pay with, whatever they buy."),
        new Rule(7, 4, "Your name tag is blank. Anyone who calls you by your name: refuse them."),
        new Rule(8, 5, "Count the eyes. More than two: refuse."),
        new Rule(9, 6, "Nobody shops in aisle seven. Anyone who asks about aisle seven: refuse."),
        new Rule(10, 7, "You are the only one working tonight. Anyone wearing our uniform is NOT staff. Refuse them."),
    };

    public static bool ShouldRefuse(CustomerSpec s, int hour, out string why)
    {
        if (hour >= 7 && s.uniform) { why = "Rule 10: they were wearing our uniform."; return true; }
        if (hour >= 6 && s.asksAisle7) { why = "Rule 9: they asked about aisle seven."; return true; }
        if (hour >= 5 && s.eyes > 2) { why = "Rule 8: they had " + s.eyes + " eyes."; return true; }
        if (hour >= 4 && s.saysName) { why = "Rule 7: they called you by name."; return true; }
        if (hour >= 3 && s.faceless) { why = "Rule 6: faceless regulars are ALWAYS served."; return false; }
        if (hour >= 2 && !s.payValid) { why = "Rule 3: they paid with " + s.payment + "."; return true; }
        if (s.HasMissing) { why = "Rule 2: the scanner couldn't find an item."; return true; }
        why = "Rule 1: they were an ordinary customer.";
        return false;
    }
}
