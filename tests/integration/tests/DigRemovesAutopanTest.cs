// Clicking an autopanner with the dig tool removes it (rather than toggling it). The
// dig tool itself is gated on a supplied Shovel Rental, so removal only works once
// shovels are rented.
public class DigRemovesAutopanTest : IIntegrationTest
{
    public string Name => "tile/dig-removes-autopan";

    public void Run(TestContext ctx)
    {
        var gs = GameState.Instance;

        // A gold autopanner on the watered upstream river.
        ctx.Actions.BuildGoldAutopanner(2, 5); // (col 2, row 5) beside connected (2,6)
        ctx.Actions.StepPropagation();
        ctx.AssertTrue(gs.TileMachine[5, 2] != 0f, "autopanner placed");

        // Without rented shovels the dig-remove request is ignored.
        ctx.Actions.RemoveMachine(2, 5);
        ctx.AssertTrue(gs.TileMachine[5, 2] != 0f, "not removed while shovels are locked");

        // Rent a shovel, then dig-clicking the panner removes it (tile stays Soil).
        ctx.Actions.EnableShovels();
        ctx.Actions.RemoveMachine(2, 5);
        ctx.AssertFloat(0f, gs.TileMachine[5, 2], "autopanner removed by the dig tool");
        ctx.AssertEqual(GameState.TileType.Soil, gs.Tiles[5, 2], "tile underneath stays Soil");
    }
}
