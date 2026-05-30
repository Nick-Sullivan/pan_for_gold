// A material is pannable in the lowlands only while a river runs beside its
// source in the highlands. Gold ships fed (river beside the gold source); clay
// turns on when a river is routed beside the clay source; routing the river off
// the gold source turns gold off. The two are independent.
public class ClaySwapTest : IIntegrationTest
{
    public string Name => "economy/source-gates-material";

    private const int BankCol = 9, BankRow = 1;

    public void Run(TestContext ctx)
    {
        var gs = GameState.Instance;
        var highlands = gs.GetZoneData(1)[0].Tiles; // highlands region 0

        // Default: a river sits beside the gold source, none beside the clay source.
        ctx.Actions.StepTicks(5);
        ctx.AssertTrue(gs.TileGold[BankRow, BankCol] > 0f, "gold pannable while a river is beside the gold source");
        ctx.AssertFloat(0f, gs.TileClay[BankRow, BankCol], "no clay while no river is beside the clay source");

        // Route a river beside the clay source at (col 4, row 7) -> clay turns on.
        highlands[6, 4] = GameState.TileType.River;
        float clayBefore = gs.TileClay[BankRow, BankCol];
        ctx.Actions.StepTicks(5);
        ctx.AssertTrue(gs.TileClay[BankRow, BankCol] > clayBefore, "clay pannable once a river borders the clay source");

        // Route the river away from the gold source -> gold turns off (clay stays).
        highlands[2, 3] = GameState.TileType.Bank;
        highlands[3, 2] = GameState.TileType.Bank;
        float goldBefore = gs.TileGold[BankRow, BankCol];
        float clayMid = gs.TileClay[BankRow, BankCol];
        ctx.Actions.StepTicks(5);
        ctx.AssertFloat(goldBefore, gs.TileGold[BankRow, BankCol], "gold stops once no river borders the gold source");
        ctx.AssertTrue(gs.TileClay[BankRow, BankCol] > clayMid, "clay keeps accruing (independent of gold)");
    }
}
