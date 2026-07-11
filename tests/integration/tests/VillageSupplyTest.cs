// A village is supplied when this map's gold generation meets its demand. Reach
// region 1 (rent a shovel + dig the gap to unlock it), build gold autopanners on its
// entry channel until generation covers the village's demand (supplied -> quest 7),
// then toggle the village's trade off.
public class VillageSupplyTest : IIntegrationTest
{
    public string Name => "village/gold-supply";

    public void Run(TestContext ctx)
    {
        var gs = GameState.Instance;
        var v0 = VillageDefs.All[0];

        // Open the first map's channel: rent a shovel, dig the gap to the edge.
        ctx.Actions.EnableShovels();
        ctx.Actions.Dig(6, 6);
        ctx.Actions.Dig(7, 6);
        ctx.Actions.StepPropagation();
        ctx.AssertEqual(2, gs.UnlockedRegions, "region 1 unlocked after digging the gap");
        ctx.Actions.SwitchRegion(1);
        ctx.AssertTrue(gs.VillagesDiscovered[0], "village 0 discovered on entry");
        ctx.AssertTrue(!gs.VillageSupplied[0], "not supplied before any autopanners");

        // The entry river enters at (col 0, row 6); build gold autopanners around it.
        ctx.Actions.BuildGoldAutopanner(1, 6);
        ctx.Actions.BuildGoldAutopanner(0, 5);
        ctx.Actions.BuildGoldAutopanner(0, 7);
        ctx.Actions.StepPropagation();

        ctx.AssertTrue(gs.GoldGen >= v0.GoldDemand,
            $"gold gen covers demand (gen {gs.GoldGen}, demand {v0.GoldDemand})");
        ctx.AssertTrue(gs.VillageSupplied[0], "village supplied once gen >= demand");
        ctx.AssertTrue(gs.QuestsComplete[7], "supplying village 0 completes quest 7");

        // Toggling the village off stops the trade — it no longer counts as supplied.
        ctx.Actions.ToggleVillage(v0.Col, v0.Row);
        ctx.AssertTrue(!gs.VillageSupplyOn[0], "village drain toggled off");
        ctx.AssertTrue(!gs.VillageSupplied[0], "not supplied while the trade is off");
    }
}
