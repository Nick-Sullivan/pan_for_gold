// Premise verifier (the flow engine is a black box): a river loses flow to Bank/
// Soil neighbours, so lining a channel with Brick should preserve more flow
// downstream. Builds an isolated straight channel, measures end-of-channel flow
// with Soil banks vs Brick banks, and asserts brick yields strictly more.
// If this fails, the "no flow-engine change needed" assumption is wrong.
public class BrickReducesLossTest : IIntegrationTest
{
    public string Name => "flow/brick-reduces-loss";

    private const int Row = 2;     // channel row
    private const int LastCol = 11; // channel runs col 0 (source) .. 11

    public void Run(TestContext ctx)
    {
        var gs = GameState.Instance;

        // Clear an isolated band (rows 0..4) so only our channel carries flow there.
        for (int r = 0; r <= 4; r++)
            for (int c = 0; c < GameState.Cols; c++)
                gs.Tiles[r, c] = GameState.TileType.Stone;

        // Straight channel fed by a source at the left edge.
        gs.Tiles[Row, 0] = GameState.TileType.RiverSource;
        for (int c = 1; c <= LastCol; c++)
            gs.Tiles[Row, c] = GameState.TileType.River;

        // Banks above and below start as Soil (flow-lossy).
        for (int c = 1; c <= LastCol; c++)
        {
            gs.Tiles[Row - 1, c] = GameState.TileType.Soil;
            gs.Tiles[Row + 1, c] = GameState.TileType.Soil;
        }

        ctx.Actions.StepPropagation();
        float soilFlow = gs.TileFlowValues[Row, LastCol];

        // Line the channel with brick instead.
        for (int c = 1; c <= LastCol; c++)
        {
            gs.Tiles[Row - 1, c] = GameState.TileType.Brick;
            gs.Tiles[Row + 1, c] = GameState.TileType.Brick;
        }

        ctx.Actions.StepPropagation();
        float brickFlow = gs.TileFlowValues[Row, LastCol];

        ctx.AssertTrue(brickFlow > 0f, $"channel carries flow (brickFlow={brickFlow})");
        ctx.AssertTrue(brickFlow > soilFlow,
            $"brick preserves more flow than soil banks (soil={soilFlow}, brick={brickFlow})");
    }
}
