// Brick is placed (consuming inventory) on a Soil/Bank tile bordering the river;
// it is rejected on tiles not next to a river, and can be dug back to Soil.
public class PlaceBrickTest : IIntegrationTest
{
    public string Name => "tile/place-brick";

    public void Run(TestContext ctx)
    {
        var gs = GameState.Instance;
        gs.Bricks = 2;

        // (col 3, row 3) is Soil with river at (col 2, row 3) beside it.
        ctx.AssertEqual(GameState.TileType.Soil, gs.Tiles[3, 3], "target starts as Soil");
        ctx.AssertEqual(GameState.TileType.River, gs.Tiles[3, 2], "river borders the target");
        ctx.Actions.PlaceBrick(3, 3);
        ctx.AssertEqual(GameState.TileType.Brick, gs.Tiles[3, 3], "brick placed on river-adjacent soil");
        ctx.AssertEqual(1, gs.Bricks, "one brick consumed");

        // (col 12, row 12) is Soil far from any river -> rejected.
        ctx.AssertEqual(GameState.TileType.Soil, gs.Tiles[12, 12], "far tile is Soil");
        ctx.Actions.PlaceBrick(12, 12);
        ctx.AssertEqual(GameState.TileType.Soil, gs.Tiles[12, 12], "no brick on non-river-adjacent tile");
        ctx.AssertEqual(1, gs.Bricks, "brick not consumed on rejected placement");

        // A brick can be dug back to soil (needs a shovel).
        gs.Shovels = 1;
        ctx.Actions.Dig(3, 3);
        ctx.AssertEqual(GameState.TileType.Soil, gs.Tiles[3, 3], "brick dug back to soil");
    }
}
