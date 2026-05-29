// Proves the harness can boot the real game and read real state: after a fresh
// new game we are in Zone 0 / Region 0 with an empty economy and the starting
// map matches MapLayouts.
public class BootInitialStateTest : IIntegrationTest
{
    public string Name => "boot/initial-state";

    public void Run(TestContext ctx)
    {
        var gs = GameState.Instance;

        ctx.AssertEqual(0, gs.CurrentZone, "CurrentZone");
        ctx.AssertEqual(0, gs.CurrentRegion, "CurrentRegion");
        ctx.AssertEqual(1, gs.UnlockedRegions, "UnlockedRegions");

        ctx.AssertEqual(0, gs.Gold, "Gold");
        ctx.AssertEqual(0, gs.Clay, "Clay");
        ctx.AssertEqual(0, gs.Shovels, "Shovels");
        ctx.AssertEqual(GameState.ActiveTool.Pan, gs.Tool, "Tool");

        // Starting tiles match the parsed layout for Zone 0, Region 0.
        var expected = MapLayouts.BuildTiles(MapLayouts.Maps[0][0]);
        for (int row = 0; row < GameState.Rows; row++)
            for (int col = 0; col < GameState.Cols; col++)
                ctx.AssertEqual(expected[row, col], gs.Tiles[row, col], $"Tile[{row},{col}]");
    }
}
