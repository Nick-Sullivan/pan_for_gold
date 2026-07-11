// The village is "discovered" the first time the player enters region 1 (zone 0)
// — not merely when it unlocks. Discovery is a persisted, one-shot flag that the
// HUD uses to show the village dialogue and reveal the Highlands toggle.
public class VillageDiscoveryTest : IIntegrationTest
{
    public string Name => "village/discovery";

    public void Run(TestContext ctx)
    {
        var gs = GameState.Instance;
        ctx.AssertTrue(!gs.VillageDiscovered, "not discovered at start");

        // Region 1 unlocks once region 0 passes flow downstream — which needs the river's
        // gap dug so flow reaches the east edge. Rent a shovel, dig the gap.
        ctx.Actions.EnableShovels();
        ctx.Actions.Dig(6, 6);
        ctx.Actions.Dig(7, 6);
        ctx.Actions.StepPropagation();
        ctx.AssertEqual(2, gs.UnlockedRegions, "region 1 unlocked once the dug river reaches the edge");
        ctx.AssertTrue(!gs.VillageDiscovered, "still not discovered before entering the village");

        // Entering the village discovers it.
        ctx.Actions.SwitchRegion(1);
        ctx.AssertEqual(1, gs.CurrentRegion, "in region 1");
        ctx.AssertTrue(gs.VillageDiscovered, "discovered after entering region 1");
        ctx.AssertTrue(gs.QuestsComplete[3], "discovery completes the 'find the next map' quest");

        // Leaving and returning does not reset or re-trigger discovery.
        ctx.Actions.SwitchRegion(0);
        ctx.Actions.SwitchRegion(1);
        ctx.AssertTrue(gs.VillageDiscovered, "stays discovered after revisiting");

        // The flag persists across a snapshot round-trip (save/load).
        var snap = gs.ToSnapshot();
        ctx.Runner.StartNewGame();
        ctx.AssertTrue(!gs.VillageDiscovered, "new game clears discovery");
        gs.ApplySnapshot(snap);
        ctx.AssertTrue(gs.VillageDiscovered, "discovery restored from snapshot");
    }
}
