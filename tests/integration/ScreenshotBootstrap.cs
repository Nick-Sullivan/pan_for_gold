using Godot;
using System;

// Launched by main.gd / TitleScreen when "--shot <scenario>" is on the command
// line (a real, non-headless run — headless has no renderer). Drives the game to
// the named state via PlayerActions, then saves the rendered frame to
// res://screenshots/<scenario>.png and quits. Lets a screenshot be inspected
// without watching the window live.
public partial class ScreenshotBootstrap : Node
{
    private int _frame;
    private string _scenario = "overworld";

    public override void _Ready()
    {
        var args = OS.GetCmdlineUserArgs();
        int i = Array.IndexOf(args, "--shot");
        if (i >= 0 && i + 1 < args.Length)
            _scenario = args[i + 1];
    }

    public override void _Process(double delta)
    {
        _frame++;
        // Frame 2: GameRunner.ConnectViewSignals (deferred in main.gd) has run, so
        // PlayerActions signals reach the systems. Build the scenario state.
        if (_frame == 2)
        {
            try { Setup(_scenario); }
            catch (Exception e) { GD.PrintErr($"[shot] setup failed: {e}"); }
        }
        // Give the renderer a few frames to draw the new state, then capture.
        if (_frame >= 8)
        {
            SetProcess(false);
            Capture(_scenario);
            GetTree().Quit();
        }
    }

    private void Setup(string scenario)
    {
        var runner = GameRunner.Instance;
        if (runner == null || scenario == "title")
            return; // title scene captures itself; nothing to drive

        runner.TestMode = true; // step deterministically, no real-time drift
        var a = new PlayerActions(GetTree(), runner);

        switch (scenario)
        {
            case "overworld":
                break;
            case "unlocked": // region 0 with region 1 unlocked (gap dug, flow reaches edge) -> east arrow
                runner.StepPropagation();
                a.EnableShovels();
                a.Dig(6, 6);
                a.Dig(7, 6);
                runner.StepPropagation();
                break;
            case "village": // region 1: village sign, west arrow, discovery dialog
                a.EnableShovels();
                a.Dig(6, 6);
                a.Dig(7, 6);
                runner.StepPropagation();
                a.SwitchRegion(1);
                break;
            case "village2": // region 2: second village (teal tile) + Marl's dialogue
                a.EnableShovels();
                a.Dig(6, 6);
                a.Dig(7, 6);
                runner.StepPropagation();
                a.SwitchRegion(1);
                // Supply village 0 (off the river path) to open the east gate, then carve a
                // river across row 6 to the now-open edge so region 1's output unlocks region 2.
                a.BuildGoldAutopanner(0, 5);
                runner.StepPropagation();
                for (int c = 1; c <= 13; c++) a.Dig(c, 6);
                runner.StepPropagation();
                a.SwitchRegion(2);
                break;
            case "furnace": // region 0 with a lit furnace placed + the Furnace tool revealed
            {
                var gs = GameState.Instance;
                gs.HasFurnace = true;
                gs.EmitSignal(GameState.SignalName.FurnaceChanged, true);
                a.UseFurnace(11, 11); // (col 11, row 11) is bare soil
                a.StepTicks(2);
                break;
            }
            case "build": // region 0: Build tab open with gold + clay autopanners + a lit furnace
            {
                var gs = GameState.Instance;
                gs.HasFurnace = true;
                gs.EmitSignal(GameState.SignalName.FurnaceChanged, true);
                runner.StepPropagation();
                a.BuildGoldAutopanner(3, 5);   // Soil beside the watered upstream river
                a.BuildClayAutopanner(2, 7);   // Soil beside the river
                a.UseFurnace(11, 11);          // bare soil
                a.StepTicks(2);
                // Open the Build tab (keyboard shortcut 2) so its buttons show.
                GetViewport().PushInput(new InputEventKey { Keycode = Key.Key2, Pressed = true });
                runner.StepPropagation();
                return;
            }
            case "hover": // region 0 with the cursor over the east arrow (hover anim)
                runner.StepPropagation();
                // East arrow centre = TileCenter(13,6) + colVec*1.4.
                GetViewport().PushInput(new InputEventMouseMotion { Position = new Vector2(1010, 551) });
                return;
        }
        runner.StepPropagation();
    }

    // Hide the village discovery modal (its full-screen dim is the HUD's only
    // ColorRect child) so a scenario can show the map after triggering VillageFound.
    private void HideDialog()
    {
        var hud = GetTree().GetFirstNodeInGroup("hud");
        if (hud == null) return;
        foreach (var child in hud.GetChildren())
            if (child is ColorRect cr) cr.Visible = false;
    }

    private void Capture(string scenario)
    {
        var img = GetViewport().GetTexture().GetImage();
        var dir = ProjectSettings.GlobalizePath("res://screenshots/");
        System.IO.Directory.CreateDirectory(dir);
        var path = $"{dir}{scenario}.png";
        img.SavePng(path);
        GD.Print($"[shot] saved {path}");
    }
}
