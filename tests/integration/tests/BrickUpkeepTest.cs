// Each laid Brick consumes brick/sec; the furnace's brick output must sustain them.
// Sets up clay -> furnace (BrickGen), then lays bricks until BrickUse hits the furnace's
// output ceiling and further placement is blocked.
public class BrickUpkeepTest : IIntegrationTest
{
    public string Name => "economy/brick-upkeep";

    public void Run(TestContext ctx)
    {
        var gs = GameState.Instance;

        // Rent a shovel (region 0) so digging works, then switch clay on by routing the
        // highlands river beside the clay source.
        ctx.Actions.EnableShovels();
        ctx.Actions.SwitchZone(1);
        ctx.Actions.Dig(4, 6);
        ctx.Actions.SwitchZone(0); // back to lowlands region 0 (plenty of soil)
        ctx.Actions.StepPropagation();

        // Clay autopanner beside the connected upstream river, plus a furnace -> brick output.
        ctx.Actions.BuildClayAutopanner(2, 5); // (col 2,row 5) beside connected (2,6)
        ctx.Actions.UseFurnace(11, 11);
        ctx.Actions.StepPropagation();
        ctx.AssertTrue(gs.BrickGen > 0f, $"furnace produces bricks (BrickGen={gs.BrickGen})");
        ctx.AssertFloat(0f, gs.BrickUse, "no bricks laid yet -> no brick use");

        // Lay bricks along row 12 (all Soil) until the furnace can't sustain more.
        int laid = 0;
        for (int col = 1; col <= 12; col++)
        {
            ctx.Actions.PlaceBrick(col, 12);
            if (gs.Tiles[12, col] == GameState.TileType.Brick) laid++;
        }

        int expected = (int)(gs.BrickGen / GameState.BrickUpkeepPerSec); // furnace capacity
        ctx.AssertEqual(expected, laid, "bricks laid are capped by furnace brick output");
        ctx.AssertFloat(laid * GameState.BrickUpkeepPerSec, gs.BrickUse, "brick use = laid * upkeep");
        ctx.AssertTrue(gs.Tiles[12, expected + 1] == GameState.TileType.Soil,
            "the brick past capacity was rejected (stays Soil)");
    }
}
