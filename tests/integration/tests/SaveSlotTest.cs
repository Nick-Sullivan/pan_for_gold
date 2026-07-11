using Godot;

// The save-slot system end to end: an empty slot reads as empty; New Game in a
// slot writes a file and marks it active; the HUD Save button persists the
// active slot; loading the slot into a wiped game restores it; Delete clears it.
// Uses a temp save dir so the player's real saves are never touched.
public class SaveSlotTest : IIntegrationTest
{
    public string Name => "persistence/save-slots";

    private static string TempDir
        => ProjectSettings.GlobalizePath("res://tests/integration/tmp-saves");

    public void Run(TestContext ctx)
    {
        var gs = GameState.Instance;
        var save = ctx.Runner.Save;
        string originalDir = save.BaseDir;
        save.BaseDir = TempDir;
        CleanDir();

        try
        {
            // An untouched slot reads as empty.
            ctx.AssertTrue(!save.ReadInfo(0).Exists, "slot 0 empty before new game");

            // New Game writes the slot and makes it the active game.
            ctx.Runner.NewGameInSlot(0);
            ctx.AssertEqual(0, ctx.Runner.ActiveSlot, "ActiveSlot after NewGameInSlot");
            ctx.AssertTrue(save.Exists(0), "slot 0 file written by NewGameInSlot");

            // Mutate: rent a shovel, reshape a tile, build a machine.
            ctx.Actions.EnableShovels();
            ctx.Actions.Dig(6, 12); // Soil -> River
            ctx.Actions.BuildGoldAutopanner(6, 11);

            var tilesBefore = (GameState.TileType[,])gs.Tiles.Clone();
            var machineBefore = (float[,])gs.TileMachine.Clone();

            // Persist via the HUD Save button (exercises HUD -> GameRunner wiring).
            ctx.Actions.Save();
            ctx.AssertTrue(save.ReadInfo(0).Exists, "slot summary reads as existing after save");

            // Wipe, then load the slot back.
            ctx.Runner.StartNewGame();
            ctx.AssertFloat(0f, gs.TileMachine[11, 6], "machine wiped by new game");

            ctx.Runner.LoadSlot(0);
            for (int row = 0; row < GameState.Rows; row++)
                for (int col = 0; col < GameState.Cols; col++)
                {
                    ctx.AssertEqual(tilesBefore[row, col], gs.Tiles[row, col], $"Tile[{row},{col}] restored");
                    ctx.AssertFloat(machineBefore[row, col], gs.TileMachine[row, col], $"Machine[{row},{col}] restored");
                }

            // Delete clears the slot.
            save.Delete(0);
            ctx.AssertTrue(!save.ReadInfo(0).Exists, "slot 0 empty after delete");
        }
        finally
        {
            CleanDir();
            save.BaseDir = originalDir;
        }
    }

    private static void CleanDir()
    {
        if (System.IO.Directory.Exists(TempDir))
            System.IO.Directory.Delete(TempDir, recursive: true);
    }
}
