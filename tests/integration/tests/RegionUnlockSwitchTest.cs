// Zone 0 Region 0's river already reaches the east edge (col 13, row 6), so the
// first dig triggers TryUnlock and opens Region 1. Switching to it swaps in the
// village/gate map with its col-0 entry synced to the exit row.
public class RegionUnlockSwitchTest : IIntegrationTest
{
    public string Name => "region/unlock-and-switch";

    public void Run(TestContext ctx)
    {
        var gs = GameState.Instance;
        ctx.AssertEqual(1, gs.UnlockedRegions, "one region unlocked at start");

        // Earn a shovel and dig once; OnDig runs TryUnlock, which sees the river
        // exit and creates Region 1.
        ctx.Actions.PanUntilGold(GameState.ShovelCost, 9, 1);
        ctx.Actions.BuyShovel();
        ctx.Actions.Dig(6, 12);

        ctx.AssertEqual(2, gs.UnlockedRegions, "Region 1 unlocked after dig");

        // Switch to the new region and verify its tiles are now active.
        ctx.Actions.SwitchRegion(1);
        ctx.AssertEqual(1, gs.CurrentRegion, "switched to Region 1");

        ctx.AssertEqual(GameState.TileType.Village, gs.Tiles[0, 7], "Village landmark at (7,0)");
        ctx.AssertEqual(GameState.TileType.Gate, gs.Tiles[6, 13], "Gate down the east edge");

        // Entry column (col 0) is synced from Region 0's exit row (row 6).
        ctx.AssertEqual(GameState.TileType.RiverSource, gs.Tiles[6, 0], "entry RiverSource at exit row");
        ctx.AssertEqual(GameState.TileType.Soil, gs.Tiles[5, 0], "non-exit entry row stays Soil");
    }
}
