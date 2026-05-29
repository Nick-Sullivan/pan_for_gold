// The third main-quest step completes when the village on region 1 receives
// enough flow. Drives the QuestSystem.FlowChanged listener directly (the
// steady-state flow model is a black box): below threshold it stays incomplete;
// at threshold it completes and finishes the quest line.
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

        // No flow at the village yet -> objective 2 stays incomplete.
        gs.TileFlowValues[GameState.VillageRow, GameState.VillageCol] = 0f;
        gs.EmitSignal(GameState.SignalName.FlowChanged);
        ctx.AssertTrue(!gs.QuestsComplete[2], "quest 2 incomplete below threshold");

        // Supply enough flow -> objective 2 completes and the line finishes.
        gs.TileFlowValues[GameState.VillageRow, GameState.VillageCol] = GameState.VillageFlowThreshold;
        gs.EmitSignal(GameState.SignalName.FlowChanged);
        ctx.AssertTrue(gs.QuestsComplete[2], "quest 2 complete at threshold");
        ctx.AssertEqual(-1, QuestSystem.CurrentObjective(), "quest line finished");
    }
}
