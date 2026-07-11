using Godot;

// Single source of truth for every village: where it sits, what it needs, who its
// elder is, and how its tile is tinted. Replaces the scattered single-village
// constants (GameState.VillageRow/Col) so adding a village is one entry here plus a
// map layout. Lookups are keyed by (zone, region) since a region holds at most one
// village. The id is the index into All (also the bit index in
// GameState.VillagesDiscovered and the QuestSystem wiring).
public sealed record VillageDef(
    int Zone,
    int Region,
    int Row,
    int Col,
    string Name,
    string Dialogue,
    float FlowThreshold,
    Color TileColor,
    bool HasEastGate,
    // Gold/sec the village drains from the pool while its supply is on. 0 means the
    // village is satisfied by river flow (FlowThreshold) instead of a gold supply.
    float GoldDemand = 0f);

public static class VillageDefs
{
    public static readonly VillageDef[] All =
    [
        // Village 0 — the first lowlands village. Trades for a steady stream of gold.
        new VillageDef(
            Zone: 0, Region: 1, Row: 0, Col: 7,
            Name: "Sediment, Village Elder",
            Dialogue:
                "Welcome, river-shaper. We live by trade and need a steady stream of gold — "
                + "five coin a second. Build gold autopanners beside the river to pan it for us; "
                + "the stronger the river's flow, the more each one yields. Bare banks bleed the "
                + "river's strength, so line the channel with brick to keep the flow up. Keep us "
                + "supplied and the gate east will open.",
            FlowThreshold: 100f,
            TileColor: new Color(0.85f, 0.65f, 0.15f),
            HasEastGate: true,
            GoldDemand: 5f),

        // Village 1 — the second lowlands village, past the gate. It no longer asks
        // for raw flow; it needs a steady stream of gold, panned automatically by
        // machines built on the river and delivered down the channel.
        new VillageDef(
            Zone: 0, Region: 2, Row: 0, Col: 7,
            Name: "Marl, the Reed-Weaver",
            Dialogue:
                "Downstream folk, well met. I weave the reeds of the Lowmarsh, and we are a "
                + "thirstier market than Sediment — ten coin a second, or the reeds go unsold. "
                + "Our marsh sits far from the source, so the river arrives weak; line the long "
                + "channel with brick to keep its flow, then build gold autopanners beside it. "
                + "Click our village to halt the trade whenever the river runs thin.",
            FlowThreshold: 150f,
            TileColor: new Color(0.15f, 0.65f, 0.70f),
            HasEastGate: false,
            GoldDemand: 10f),
    ];

    public static int Count => All.Length;

    // The village in the given region, or null if that region has none.
    public static VillageDef ForRegion(int zone, int region)
    {
        foreach (var v in All)
            if (v.Zone == zone && v.Region == region)
                return v;
        return null;
    }

    public static int IndexOf(VillageDef def) => System.Array.IndexOf(All, def);

    // Default amber, used when a region has no village def (shouldn't happen for a
    // Village tile, but keeps the renderers total).
    public static readonly Color DefaultColor = new(0.85f, 0.65f, 0.15f);

    // Tint for Village tiles in the currently active region. Null-safe so the pure
    // unit-test renderers (no GameState autoload) fall back to the default amber.
    public static Color ActiveColor()
    {
        var gs = GameState.Instance;
        if (gs == null) return DefaultColor;
        return ForRegion(gs.CurrentZone, gs.CurrentRegion)?.TileColor ?? DefaultColor;
    }
}
