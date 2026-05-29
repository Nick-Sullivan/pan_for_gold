// The first two main-quest steps advance from player actions: buying a shovel
// completes objective 0, and unlocking region 1 (river reaches the east edge)
// completes objective 1. The current objective advances each time.
public class QuestShovelEdgeTest : IIntegrationTest
{
    public string Name => "quest/shovel-and-edge";

    public void Run(TestContext ctx)
    {
        var gs = GameState.Instance;

        ctx.AssertTrue(!gs.QuestsComplete[0], "quest 0 incomplete at start");
        ctx.AssertTrue(!gs.QuestsComplete[1], "quest 1 incomplete at start");
        ctx.AssertEqual(0, QuestSystem.CurrentObjective(), "current objective starts at 0");

        // Objective 0: buy a shovel.
        ctx.Actions.PanUntilGold(GameState.ShovelCost, 9, 1);
        ctx.Actions.BuyShovel();
        ctx.AssertTrue(gs.QuestsComplete[0], "quest 0 complete after buying shovel");
        ctx.AssertEqual(1, QuestSystem.CurrentObjective(), "current objective advances to 1");

        // Objective 1: the river reaches the east edge -> region 1 unlocks.
        ctx.Actions.Dig(6, 12);
        ctx.AssertEqual(2, gs.UnlockedRegions, "region 1 unlocked");
        ctx.AssertTrue(gs.QuestsComplete[1], "quest 1 complete after unlock");
        ctx.AssertEqual(2, QuestSystem.CurrentObjective(), "current objective advances to 2");
    }
}
