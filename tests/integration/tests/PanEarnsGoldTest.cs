// A bank tile adjacent to flowing river accrues gold over ticks; panning it
// transfers that gold to the player and resets the tile to zero.
public class PanEarnsGoldTest : IIntegrationTest
{
    public string Name => "economy/pan-earns-gold";

    // Bank at (col 9, row 1) with river directly below at (9, 2).
    private const int Col = 9;
    private const int Row = 1;

    public void Run(TestContext ctx)
    {
        var gs = GameState.Instance;
        ctx.AssertEqual(GameState.TileType.Bank, gs.Tiles[Row, Col], "starting tile is Bank");

        // Tick until the bank has earned a whole gold unit. Propagation runs each
        // tick, so flow reaches the bank within a couple of ticks.
        int ticks = 0;
        while ((int)gs.TileGold[Row, Col] < 1 && ticks++ < 2000)
            ctx.Actions.StepTicks(1);

        ctx.AssertTrue((int)gs.TileGold[Row, Col] >= 1, $"bank earned gold within {ticks} ticks");

        int expectedGain = (int)gs.TileGold[Row, Col];
        int goldBefore = gs.Gold;

        ctx.Actions.Pan(Col, Row);

        ctx.AssertEqual(goldBefore + expectedGain, gs.Gold, "player gold after pan");
        ctx.AssertEqual(0, (int)gs.TileGold[Row, Col], "tile gold reset after pan");
    }
}
