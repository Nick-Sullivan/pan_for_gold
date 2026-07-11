// The guided main quest line, rebuilt around the rate economy: tap the river with
// autopanners, manage flow (output > 0) to progress, route clay, fire bricks, and
// supply each village's gold demand. Definitions are the single source of truth for
// the HUD; GameState holds only the completion bits. Detection rides existing signals.
public class QuestSystem
{
    // Ordered objectives. Length must match GameState.QuestsComplete. Hints interpolate
    // live constants so they always match the real checks.
    public static readonly (string Title, string Hint)[] Defs =
    [
        ("Tap the River",            "Pick the Gold Autopanner and build it on soil next to the connected river."),
        ("Rent a Shovel",            $"Build a Shovel Rental and keep it supplied with gold ({(int)GameState.ShovelRentalGoldPerSec}/s) to unlock the dig tool."),
        ("Open the Channel",         "Dig through the gap to connect the river to the east edge — flow reaching the edge opens the next map."),
        ("Find the Next Map",        "Follow the river east to the next map and meet its village."),
        ("Feed the Clay",            "In the highlands, route the river beside the clay source so clay can be panned."),
        ("Collect Clay",             "Build a Clay Autopanner on a tile beside a clay-fed river."),
        ("Fire Bricks",              "Place a furnace and let it draw clay to fire bricks (so you can brick-line channels)."),
        ("Supply the Village",       $"Build enough gold autopanners to cover the village's demand of {(int)VillageDefs.All[0].GoldDemand}/s — that also opens the gate east."),
        ("Find the Second Village",  "When the gate opens, carve east into the next map and meet its elder."),
        ("Supply the Second Village", $"Out-produce Marl's demand of {(int)VillageDefs.All[1].GoldDemand}/s with gold autopanners on the marsh."),
    ];

    public void Connect()
    {
        var gs = GameState.Instance;

        // 0/1/5/6: production rates / shovel enablement crossing their thresholds (a gold
        // autopanner runs, a supplied shovel rental, a clay autopanner runs, a furnace fires
        // bricks). Driven by the per-tick rate recompute.
        gs.RatesChanged += () =>
        {
            if (gs.GoldGen > 0f) Complete(0);
            if (gs.ShovelsEnabled) Complete(1);
            if (gs.ClayGen > 0f) Complete(5);
            if (gs.BrickGen > 0f) Complete(6);
        };

        // 2: digging the gap connected the river to the edge, opening the next map
        // (region 1 unlocked, zone 0).
        gs.RegionUnlocked += count => { if (gs.CurrentZone == 0 && count >= 2) Complete(2); };

        // 3 / 8: travelled to the next map (a village was discovered).
        gs.VillageFound += id =>
        {
            if (id == 0) Complete(3);
            else if (id == 1) Complete(8);
        };

        // 4: a river was routed beside the highlands clay source.
        gs.TileChanged += (_, __) => { if (Economy.SourceFed(GameState.TileType.ClaySource)) Complete(4); };

        // 7 / 9: a village's gold demand is met (gen >= demand) on its map.
        gs.VillageSupplyChanged += (id, supplied) =>
        {
            if (!supplied) return;
            if (id == 0) Complete(7);
            else if (id == 1) Complete(9);
        };
    }

    // Index of the first incomplete objective, or -1 if the line is finished.
    public static int CurrentObjective()
    {
        var complete = GameState.Instance.QuestsComplete;
        for (int i = 0; i < complete.Length; i++)
            if (!complete[i]) return i;
        return -1;
    }

    private static void Complete(int index)
    {
        var gs = GameState.Instance;
        if (gs.QuestsComplete[index]) return;
        gs.QuestsComplete[index] = true;
        gs.EmitSignal(GameState.SignalName.QuestChanged, index);
    }
}
