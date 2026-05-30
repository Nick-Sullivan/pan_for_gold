// The furnace converts clay into brick. Buying it costs gold; making a brick
// costs clay; both are gated (no furnace -> no brick; not enough clay -> no brick).
public class FurnaceBrickTest : IIntegrationTest
{
    public string Name => "economy/furnace-and-brick";

    public void Run(TestContext ctx)
    {
        var gs = GameState.Instance;

        // Without a furnace, making a brick does nothing even with clay on hand.
        gs.Clay = 10;
        ctx.Actions.MakeBrick();
        ctx.AssertEqual(0, gs.Bricks, "no brick without a furnace");
        ctx.AssertEqual(10, gs.Clay, "clay untouched without a furnace");

        // Buying the furnace debits gold and sets the flag.
        gs.Gold = GameState.FurnaceCost;
        ctx.Actions.BuyFurnace();
        ctx.AssertTrue(gs.HasFurnace, "furnace owned after purchase");
        ctx.AssertEqual(0, gs.Gold, "gold debited by FurnaceCost");

        // Convert clay into bricks.
        ctx.Actions.MakeBrick();
        ctx.Actions.MakeBrick();
        ctx.AssertEqual(2, gs.Bricks, "two bricks made");
        ctx.AssertEqual(10 - 2 * GameState.BrickClayCost, gs.Clay, "clay debited per brick");
        ctx.AssertTrue(gs.QuestsComplete[5], "firing a brick completes the brick quest");

        // Not enough clay -> no brick.
        gs.Clay = 0;
        ctx.Actions.MakeBrick();
        ctx.AssertEqual(2, gs.Bricks, "no brick when clay is insufficient");
    }
}
