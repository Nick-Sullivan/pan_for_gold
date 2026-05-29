using Godot;

// Mutating the game, saving it, starting a fresh game, then loading restores the
// full state. Exercises ToSnapshot/ApplySnapshot via Save/Load — the save-slot
// and fixture-snapshot foundation.
public class SaveLoadRoundTripTest : IIntegrationTest
{
    public string Name => "persistence/save-load-roundtrip";

    private static string SavePath
        => ProjectSettings.GlobalizePath("res://tests/integration/last-test-save.json");

    public void Run(TestContext ctx)
    {
        var gs = GameState.Instance;

        // Build up some non-trivial state: gold, a shovel, and a reshaped tile.
        ctx.Actions.PanUntilGold(GameState.ShovelCost + 5, 9, 1);
        ctx.Actions.BuyShovel();
        ctx.Actions.Dig(6, 12); // Soil -> River

        int goldBefore = gs.Gold;
        int shovelsBefore = gs.Shovels;
        int unlockedBefore = gs.UnlockedRegions;
        var tilesBefore = (GameState.TileType[,])gs.Tiles.Clone();

        gs.Save(SavePath);

        // Wipe to a fresh game, then restore from disk.
        ctx.Runner.StartNewGame();
        ctx.AssertEqual(0, gs.Gold, "gold wiped by new game");

        gs.Load(SavePath);

        ctx.AssertEqual(goldBefore, gs.Gold, "gold restored");
        ctx.AssertEqual(shovelsBefore, gs.Shovels, "shovels restored");
        ctx.AssertEqual(unlockedBefore, gs.UnlockedRegions, "unlocked regions restored");

        for (int row = 0; row < GameState.Rows; row++)
            for (int col = 0; col < GameState.Cols; col++)
                ctx.AssertEqual(tilesBefore[row, col], gs.Tiles[row, col], $"Tile[{row},{col}] restored");
    }
}
