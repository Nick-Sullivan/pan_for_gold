// Gold autopanners replace manual panning. A running gold autopanner on a tile
// beside the river generates gold at AutopanYieldPerFlow * the map's input flow
// (the gold source ships river-fed). Placement is capped per type per map.
public class AutopanGoldTest : IIntegrationTest
{
    public string Name => "economy/autopan-gold";

    // Soil tiles in region 0. Spots[0] sits beside the watered upstream river (row 6);
    // the rest are extra soil for the per-type cap check (placement needs no adjacency).
    private static readonly (int col, int row)[] Spots =
        [(3, 5), (2, 7), (4, 7), (2, 5)];

    public void Run(TestContext ctx)
    {
        var gs = GameState.Instance;
        ctx.Actions.StepPropagation();
        float input = gs.RegionData[0].InputFlow;

        // One gold autopanner -> gold generation = yield * input flow.
        ctx.Actions.BuildGoldAutopanner(Spots[0].col, Spots[0].row);
        ctx.Actions.StepPropagation();
        ctx.AssertFloat(GameState.AutopanYieldPerFlow * input, gs.GoldGen,
            "gold gen = yield * input flow for one panner", 0.01f);

        // Per-type-per-map cap: only BuildCapPerType (3) can exist.
        for (int i = 1; i < Spots.Length; i++)
            ctx.Actions.BuildGoldAutopanner(Spots[i].col, Spots[i].row);

        int gold = 0;
        for (int row = 0; row < GameState.Rows; row++)
            for (int col = 0; col < GameState.Cols; col++)
                if (GameState.MachineKind(gs.TileMachine[row, col]) == 1) gold++;
        ctx.AssertEqual(GameState.BuildCapPerType, gold, "gold autopanners capped per map");
    }
}
