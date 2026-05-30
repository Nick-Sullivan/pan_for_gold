// The opening quest line advances from player actions: pan enough gold (0), buy
// a shovel (1), then carve the channel so the river reaches the next map (2).
public class QuestShovelEdgeTest : IIntegrationTest
{
    public string Name => "quest/opening-line";

    public void Run(TestContext ctx)
    {
        var gs = GameState.Instance;
        ctx.AssertEqual(0, QuestSystem.CurrentObjective(), "objective starts at 0 (pan for gold)");

        // 0: pan for gold.
        ctx.Actions.PanUntilGold(GameState.ShovelCost, 9, 1);
        ctx.AssertTrue(gs.QuestsComplete[0], "quest 0 complete after panning enough gold");
        ctx.AssertEqual(1, QuestSystem.CurrentObjective(), "objective advances to 1 (buy a shovel)");

        // 1: buy a shovel.
        ctx.Actions.BuyShovel();
        ctx.AssertTrue(gs.QuestsComplete[1], "quest 1 complete after buying a shovel");
        ctx.AssertEqual(2, QuestSystem.CurrentObjective(), "objective advances to 2 (carve the channel)");

        // 2: carve the channel -> region 1 unlocks.
        ctx.Actions.Dig(6, 12);
        ctx.AssertEqual(2, gs.UnlockedRegions, "region 1 unlocked");
        ctx.AssertTrue(gs.QuestsComplete[2], "quest 2 complete once the river reaches the next map");
        ctx.AssertEqual(3, QuestSystem.CurrentObjective(), "objective advances to 3 (find the next map)");
    }
}
