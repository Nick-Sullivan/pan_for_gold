// Clay must not accumulate just because a clay source exists on the map — it
// only flows once a river is fed past that source. Regression for clay piling
// up on lowlands banks with no river near the highlands clay source.
public class ClaySourceTest : IIntegrationTest
{
    public string Name => "economy/clay-needs-active-source";

    // A lowlands bank tile, and the highlands clay source's diggable neighbours.
    private const int BankCol = 9, BankRow = 1;

    public void Run(TestContext ctx)
    {
        var gs = GameState.Instance;

        // Fresh game: the highlands clay source has no river beside it, so no clay
        // accumulates anywhere even after many ticks.
        ctx.Actions.StepTicks(10);
        ctx.AssertFloat(0f, gs.TileClay[BankRow, BankCol], "no clay on lowlands bank while source is unfed");
        ctx.AssertEqual(0, gs.Clay, "no clay collected while source is unfed");

        // Feed the clay source: buy a shovel, go to the highlands, dig a channel
        // from the river network up to the clay source at (col 4, row 7).
        ctx.Actions.PanUntilGold(GameState.ShovelCost, BankCol, BankRow);
        ctx.Actions.BuyShovel();
        ctx.Actions.SwitchZone(1);
        ctx.Actions.Dig(3, 6); // soil -> river, links to the source-fed network
        ctx.Actions.Dig(4, 6); // soil -> river, now adjacent to the clay source
        ctx.Actions.StepPropagation();
        ctx.Actions.StepTicks(3);

        // The clay source is now fed, so banks accumulate clay again. The lowlands
        // region is inactive now, so check its stored snapshot directly.
        float lowlandsBankClay = gs.GetZoneData(0)[0].Clay[BankRow, BankCol];
        ctx.AssertTrue(lowlandsBankClay > 0f, "clay accrues once the source is fed by a river");
    }
}
