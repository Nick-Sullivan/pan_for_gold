// The guided main quest line. Definitions live here (single source of truth for
// the HUD Quests tab and the objective banner); GameState holds only the
// completion bits. Completion is detected from existing signals — no new system.
public class QuestSystem
{
    // Ordered objectives. Length must match GameState.QuestsComplete.
    public static readonly (string Title, string Hint)[] Defs =
    [
        ("Buy a Shovel",        "Pan banks for gold, then buy a shovel in the Shop tab."),
        ("Reach the East Edge", "Dig a river channel that exits the right edge of the map."),
        ("Supply the Village",  "Guide ≥100 flow to the village on the next map."),
    ];

    public void Connect()
    {
        var gs = GameState.Instance;

        // 0: any shovel purchased.
        gs.ShovelsChanged += n => { if (n > 0) Complete(0); };

        // 1: the river reached the east edge — i.e. region 1 unlocked (zone 0).
        gs.RegionUnlocked += count =>
        {
            if (gs.CurrentZone == 0 && count >= 2) Complete(1);
        };

        // 2: enough flow delivered to the village on the second map.
        gs.FlowChanged += OnFlowChanged;
    }

    private void OnFlowChanged()
    {
        var gs = GameState.Instance;
        if (gs.CurrentZone != 0 || gs.CurrentRegion != 1 || gs.RegionData.Count <= 1)
            return;
        if (gs.TileFlowValues[GameState.VillageRow, GameState.VillageCol] >= GameState.VillageFlowThreshold)
            Complete(2);
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
