// Clay is source-gated like gold: a clay autopanner only produces once the highlands
// river is routed beside the clay source. A furnace on the same map then draws that
// clay and produces bricks (BrickGen > 0), which is what lets you brick-line channels.
public class ClayAndBrickTest : IIntegrationTest
{
    public string Name => "economy/clay-and-brick";

    public void Run(TestContext ctx)
    {
        var gs = GameState.Instance;

        // Rent a shovel on region 0 first so digging works (the highlands has no rental).
        ctx.Actions.EnableShovels();

        // Work in the highlands (zone 1), where the gold/clay sources live.
        ctx.Actions.SwitchZone(1);
        ctx.AssertEqual(1, gs.CurrentZone, "in the highlands");
        ctx.AssertTrue(!Economy.SourceFed(GameState.TileType.ClaySource), "clay source not fed yet");

        // The clay source sits at (col 4, row 7). Dig a river beside it (Soil -> River)
        // to switch clay on (SourceFed is topology-based).
        ctx.Actions.Dig(4, 6); // (col 4, row 6), just above the source
        ctx.AssertTrue(Economy.SourceFed(GameState.TileType.ClaySource), "clay source fed after routing");
        ctx.AssertTrue(gs.QuestsComplete[4], "routing the clay source completes 'Feed the Clay'");

        // Build a clay autopanner on Soil beside the CONNECTED main river ((2,6) traces
        // back to the source) so it actually produces. ((4,6) we dug is an isolated stub.)
        ctx.Actions.BuildClayAutopanner(3, 6); // (col 3, row 6) beside connected (2,6)
        ctx.Actions.StepPropagation();
        ctx.AssertTrue(gs.ClayGen > 0f, $"clay gen > 0 with a clay autopanner (got {gs.ClayGen})");
        ctx.AssertTrue(gs.QuestsComplete[5], "building a clay autopanner completes 'Collect Clay'");

        // A furnace on bare soil (col 5, row 6) draws clay and fires bricks.
        ctx.Actions.UseFurnace(5, 6);
        ctx.Actions.StepPropagation();
        ctx.AssertTrue(gs.BrickGen > 0f, $"brick gen > 0 with a clay-fed furnace (got {gs.BrickGen})");
        ctx.AssertTrue(gs.QuestsComplete[6], "a firing furnace completes 'Fire Bricks'");
    }
}
