// The guided main quest line. Definitions live here (single source of truth for
// the HUD Quests tab and the objective banner); GameState holds only the
// completion bits. Completion is detected from existing signals — no new system.
public class QuestSystem
{
    // Ordered objectives. Length must match GameState.QuestsComplete. Hints
    // interpolate the live game constants so they always match the real checks.
    public static readonly (string Title, string Hint)[] Defs =
    [
        ("Pan for Gold",       $"Pan the river banks until you have {GameState.ShovelCost} gold."),
        ("Buy a Shovel",       "Spend your gold on a shovel in the Shop tab."),
        ("Carve the Channel",  "Fill in river channels so enough flow reaches the east edge to open the next map."),
        ("Find the Next Map",  "Follow the river east to the next map and meet its village."),
        ("Feed the Clay",      "In the highlands, route the river beside the clay source so clay flows to the lowlands."),
        ("Fire a Brick",       $"Buy a furnace, then fire {GameState.BrickClayCost} clay into a brick."),
        ("Supply the Village", $"Line the channel with brick so at least {(int)GameState.VillageFlowThreshold} flow reaches the village."),
    ];

    public void Connect()
    {
        var gs = GameState.Instance;

        // 0: panned enough gold to afford a shovel.
        gs.GoldChanged += v => { if (v >= GameState.ShovelCost) Complete(0); };

        // 1: a shovel purchased.
        gs.ShovelsChanged += n => { if (n > 0) Complete(1); };

        // 2: the river reached the east edge — i.e. region 1 unlocked (zone 0).
        gs.RegionUnlocked += count => { if (gs.CurrentZone == 0 && count >= 2) Complete(2); };

        // 3: travelled to the next map (the village was discovered).
        gs.VillageFound += () => Complete(3);

        // 4: a river was routed beside the highlands clay source.
        gs.TileChanged += (_, __) => { if (Economy.SourceFed(GameState.TileType.ClaySource)) Complete(4); };

        // 5: a brick was fired.
        gs.BricksChanged += n => { if (n > 0) Complete(5); };

        // 6: enough flow delivered to the village on the second map.
        gs.FlowChanged += OnFlowChanged;
    }

    private void OnFlowChanged()
    {
        var gs = GameState.Instance;
        if (gs.CurrentZone != 0 || gs.CurrentRegion != 1 || gs.RegionData.Count <= 1)
            return;
        if (gs.TileFlowValues[GameState.VillageRow, GameState.VillageCol] >= GameState.VillageFlowThreshold)
            Complete(6);
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
