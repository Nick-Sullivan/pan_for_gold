// Digging needs a supplied Shovel Rental (ShovelsEnabled). Once unlocked, digging a soil
// tile toggles Soil -> River -> Soil (no Bank). Stone tiles are guarded by CanDig.
public class DigCycleTest : IIntegrationTest
{
    public string Name => "tile/dig-cycle";

    // Bare soil to reshape, and an edge Stone tile that must stay put.
    private const int SoilCol = 6, SoilRow = 12;
    private const int StoneCol = 0, StoneRow = 0;

    public void Run(TestContext ctx)
    {
        var gs = GameState.Instance;

        ctx.AssertEqual(GameState.TileType.Soil, gs.Tiles[SoilRow, SoilCol], "starts as Soil");

        // Dig is locked until a Shovel Rental is supplied with gold.
        ctx.AssertTrue(!gs.ShovelsEnabled, "shovels locked at start");
        ctx.Actions.Dig(SoilCol, SoilRow);
        ctx.AssertEqual(GameState.TileType.Soil, gs.Tiles[SoilRow, SoilCol], "dig does nothing while locked");

        ctx.Actions.EnableShovels();
        ctx.AssertTrue(gs.ShovelsEnabled, "shovels unlocked after renting");

        ctx.Actions.Dig(SoilCol, SoilRow);
        ctx.AssertEqual(GameState.TileType.River, gs.Tiles[SoilRow, SoilCol], "Soil -> River");

        ctx.Actions.Dig(SoilCol, SoilRow);
        ctx.AssertEqual(GameState.TileType.Soil, gs.Tiles[SoilRow, SoilCol], "River -> Soil");

        // Stone is un-diggable.
        ctx.Actions.Dig(StoneCol, StoneRow);
        ctx.AssertEqual(GameState.TileType.Stone, gs.Tiles[StoneRow, StoneCol], "Stone unchanged");
    }
}
