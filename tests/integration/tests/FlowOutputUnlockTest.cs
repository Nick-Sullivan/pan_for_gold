// The scalar flow model end to end: region 0 has a gap in its river, so it passes NO
// output downstream (no unlock) until the player digs the gap to connect the river to
// the east edge. Brick-lining a bank beside the river removes a flow consumer, raising
// the output by exactly FlowCostPerTile.
public class FlowOutputUnlockTest : IIntegrationTest
{
    public string Name => "flow/output-unlock";

    public void Run(TestContext ctx)
    {
        var gs = GameState.Instance;

        ctx.Actions.StepPropagation();
        var r0 = gs.RegionData[0];

        // The authored river stops at the gap (row 6, cols 6-7) and never reaches the east
        // edge, so no flow leaves the map and the next map stays locked.
        ctx.AssertFloat(GameState.BaseInflow, r0.InputFlow, "region 0 input = base inflow");
        ctx.AssertFloat(0f, r0.OutputFlow, "no output while the river gap is unfilled");
        ctx.AssertEqual(1, gs.UnlockedRegions, "next map locked until the river reaches the edge");

        // Rent a shovel, then dig the 2-tile gap to connect the river through to col 13.
        ctx.Actions.EnableShovels();
        ctx.Actions.Dig(6, 6);
        ctx.Actions.Dig(7, 6);
        ctx.Actions.StepPropagation();

        ctx.AssertTrue(r0.OutputFlow > 0f, $"region 0 output > 0 once connected to the edge (got {r0.OutputFlow})");
        ctx.AssertEqual(2, gs.UnlockedRegions, "region 1 unlocks because the river now reaches the edge");

        // (2,5) is Soil adjacent to the connected river — a flow consumer. Brick it: output
        // rises by one tile's worth of flow cost.
        ctx.AssertEqual(GameState.TileType.Soil, gs.Tiles[5, 2], "(2,5) starts as Soil beside the river");
        float before = r0.OutputFlow;
        gs.Tiles[5, 2] = GameState.TileType.Brick;
        ctx.Actions.StepPropagation();
        ctx.AssertFloat(before + GameState.FlowCostPerTile, gs.RegionData[0].OutputFlow,
            "bricking a river-adjacent tile raises output by FlowCostPerTile");
    }
}
