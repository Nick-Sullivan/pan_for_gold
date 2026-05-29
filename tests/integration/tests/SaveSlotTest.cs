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

            // Mutate: earn gold, buy a shovel, reshape a tile.
            ctx.Actions.PanUntilGold(GameState.ShovelCost, 9, 1);
            ctx.Actions.BuyShovel();
            ctx.Actions.Dig(6, 12); // Soil -> River

            int goldBefore = gs.Gold;
            int shovelsBefore = gs.Shovels;
            var tilesBefore = (GameState.TileType[,])gs.Tiles.Clone();

            // Persist via the HUD Save button (exercises HUD -> GameRunner wiring).
            ctx.Actions.Save();
            ctx.AssertEqual(goldBefore, save.ReadInfo(0).Gold, "slot summary reflects saved gold");

            // Wipe, then load the slot back.
            ctx.Runner.StartNewGame();
            ctx.AssertEqual(0, gs.Gold, "gold wiped by new game");

            ctx.Runner.LoadSlot(0);
            ctx.AssertEqual(goldBefore, gs.Gold, "gold restored from slot");
            ctx.AssertEqual(shovelsBefore, gs.Shovels, "shovels restored from slot");
            for (int row = 0; row < GameState.Rows; row++)
                for (int col = 0; col < GameState.Cols; col++)
                    ctx.AssertEqual(tilesBefore[row, col], gs.Tiles[row, col], $"Tile[{row},{col}] restored");

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
