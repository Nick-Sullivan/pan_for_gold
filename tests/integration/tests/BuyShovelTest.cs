// Buying a shovel is a no-op when broke; with enough gold it debits ShovelCost
// and grants one shovel.
public class BuyShovelTest : IIntegrationTest
{
    public string Name => "economy/buy-shovel";

    public void Run(TestContext ctx)
    {
        var gs = GameState.Instance;

        // Broke path: a fresh game has no gold, so buying does nothing.
        ctx.AssertEqual(0, gs.Gold, "starting gold");
        ctx.Actions.BuyShovel();
        ctx.AssertEqual(0, gs.Shovels, "shovels unchanged when broke");
        ctx.AssertEqual(0, gs.Gold, "gold unchanged when broke");

        // Earn at least one shovel's worth, then buy.
        ctx.Actions.PanUntilGold(GameState.ShovelCost, 9, 1);
        ctx.AssertTrue(gs.Gold >= GameState.ShovelCost, "earned enough gold to buy a shovel");

        int goldBefore = gs.Gold;
        ctx.Actions.BuyShovel();

        ctx.AssertEqual(1, gs.Shovels, "shovel granted");
        ctx.AssertEqual(goldBefore - GameState.ShovelCost, gs.Gold, "gold debited by ShovelCost");
    }
}
