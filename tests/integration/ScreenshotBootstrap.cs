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
            case "unlocked": // region 0 with region 1 unlocked -> east arrow shows
                Earn(a);
                a.Dig(6, 12);
                break;
            case "village": // region 1: village sign, west arrow, discovery dialog
                Earn(a);
                a.Dig(6, 12);
                a.SwitchRegion(1);
                break;
            case "hover": // region 0 with the cursor over the east arrow (hover anim)
                Earn(a);
                a.Dig(6, 12);
                runner.StepPropagation();
                // East arrow centre = TileCenter(13,6) + colVec*1.4.
                GetViewport().PushInput(new InputEventMouseMotion { Position = new Vector2(1010, 551) });
                return;
        }
        runner.StepPropagation();
    }

    private static void Earn(PlayerActions a)
    {
        a.PanUntilGold(GameState.ShovelCost, 9, 1);
        a.BuyShovel();
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
