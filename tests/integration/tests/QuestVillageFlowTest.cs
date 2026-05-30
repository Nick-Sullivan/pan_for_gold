// Reaching region 1 completes the "find the next map" quest (3); delivering
// enough flow to the village completes the final "supply the village" quest (6).
public class QuestVillageFlowTest : IIntegrationTest
{
    public string Name => "quest/village-flow";

    public void Run(TestContext ctx)
    {
        var gs = GameState.Instance;

        // Reach region 1: buy a shovel, dig to unlock, switch to it.
        ctx.Actions.PanUntilGold(GameState.ShovelCost, 9, 1);
        ctx.Actions.BuyShovel();
        ctx.Actions.Dig(6, 12);
        ctx.Actions.SwitchRegion(1);
        ctx.AssertEqual(1, gs.CurrentRegion, "in region 1");
        ctx.AssertTrue(gs.QuestsComplete[3], "finding the next map completes quest 3");

        // Below threshold -> the village quest stays incomplete.
        gs.TileFlowValues[GameState.VillageRow, GameState.VillageCol] = 0f;
        gs.EmitSignal(GameState.SignalName.FlowChanged);
        ctx.AssertTrue(!gs.QuestsComplete[6], "quest 6 incomplete below the flow threshold");

        // At threshold -> the village is supplied.
        gs.TileFlowValues[GameState.VillageRow, GameState.VillageCol] = GameState.VillageFlowThreshold;
        gs.EmitSignal(GameState.SignalName.FlowChanged);
        ctx.AssertTrue(gs.QuestsComplete[6], "quest 6 complete at the flow threshold");
    }
}
