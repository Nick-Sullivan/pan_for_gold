// With a shovel in hand, digging a soil tile rotates Soil -> River -> Bank ->
// Soil and clears its gold/flow. Stone tiles are guarded by CanDig and never
// change.
public class DigCycleTest : IIntegrationTest
{
    public string Name => "tile/dig-cycle";

    // Bare soil to reshape, and an edge Stone tile that must stay put.
    private const int SoilCol = 6, SoilRow = 12;
    private const int StoneCol = 0, StoneRow = 0;

    public void Run(TestContext ctx)
    {
        var gs = GameState.Instance;

        // A shovel is required before any dig is allowed.
        ctx.Actions.PanUntilGold(GameState.ShovelCost, 9, 1);
        ctx.Actions.BuyShovel();
        ctx.AssertEqual(1, gs.Shovels, "have a shovel before digging");

        ctx.AssertEqual(GameState.TileType.Soil, gs.Tiles[SoilRow, SoilCol], "starts as Soil");

        ctx.Actions.Dig(SoilCol, SoilRow);
        ctx.AssertEqual(GameState.TileType.River, gs.Tiles[SoilRow, SoilCol], "Soil -> River");

        ctx.Actions.Dig(SoilCol, SoilRow);
        ctx.AssertEqual(GameState.TileType.Bank, gs.Tiles[SoilRow, SoilCol], "River -> Bank");

        ctx.Actions.Dig(SoilCol, SoilRow);
        ctx.AssertEqual(GameState.TileType.Soil, gs.Tiles[SoilRow, SoilCol], "Bank -> Soil");

        // Digging clears any accrued gold/flow on the tile.
        ctx.AssertEqual(0, (int)gs.TileGold[SoilRow, SoilCol], "tile gold cleared by dig");
        ctx.AssertFloat(0f, gs.TileFlowValues[SoilRow, SoilCol], "tile flow cleared by dig");

        // Stone is un-diggable even with a shovel.
        ctx.Actions.Dig(StoneCol, StoneRow);
        ctx.AssertEqual(GameState.TileType.Stone, gs.Tiles[StoneRow, StoneCol], "Stone unchanged");
    }
}
