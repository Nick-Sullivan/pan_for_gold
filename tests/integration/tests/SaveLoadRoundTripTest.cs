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

        // Build up some non-trivial state: rent a shovel, reshape a tile, add a gold
        // autopanner beside it.
        ctx.Actions.EnableShovels();
        ctx.Actions.Dig(6, 12);              // Soil -> River
        ctx.Actions.BuildGoldAutopanner(6, 11); // beside the new river tile
        ctx.Actions.StepPropagation();

        int unlockedBefore = gs.UnlockedRegions;
        var tilesBefore = (GameState.TileType[,])gs.Tiles.Clone();
        var machineBefore = (float[,])gs.TileMachine.Clone();

        gs.Save(SavePath);

        // Wipe to a fresh game, then restore from disk.
        ctx.Runner.StartNewGame();
        ctx.AssertFloat(0f, gs.TileMachine[11, 6], "machine wiped by new game");

        gs.Load(SavePath);

        ctx.AssertEqual(unlockedBefore, gs.UnlockedRegions, "unlocked regions restored");
        for (int row = 0; row < GameState.Rows; row++)
            for (int col = 0; col < GameState.Cols; col++)
            {
                ctx.AssertEqual(tilesBefore[row, col], gs.Tiles[row, col], $"Tile[{row},{col}] restored");
                ctx.AssertFloat(machineBefore[row, col], gs.TileMachine[row, col], $"Machine[{row},{col}] restored");
            }
    }
}
