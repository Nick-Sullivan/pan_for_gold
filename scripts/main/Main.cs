using Godot;
using System.Linq;

public partial class Main : Node2D
{
    public override void _Ready()
    {
        // Instantiate Grid and HUD directly as C# objects
        var grid = new Grid();
        AddChild(grid);

        var hud = new HUD();
        AddChild(hud);

        var cmdArgs = OS.GetCmdlineUserArgs();
        if (cmdArgs.Contains("--screenshot"))
        {
            TakeScreenshot();
        }
    }

    private async void TakeScreenshot()
    {
        // Wait a couple of frames so everything is rendered, then save and quit
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var img = GetViewport().GetTexture().GetImage();
        img.SavePng("screenshot.png");
        GetTree().Quit();
    }
}