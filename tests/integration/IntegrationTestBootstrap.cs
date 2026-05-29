using Godot;
using System.Linq;

// Launched by main.gd when --run-tests is on the command line. Waits a couple of
// frames (so main.gd's deferred GameRunner.ConnectViewSignals call runs) then
// hands off to the IntegrationTestRunner. Lives as a Node because GDScript can
// instantiate a C# script and add it to the tree, but can't call a static C#
// method directly.
public partial class IntegrationTestBootstrap : Node
{
    private int _frame = 0;
    private bool _regen;

    public override void _Ready()
    {
        _regen = OS.GetCmdlineUserArgs().Contains("--regen-fixtures");
    }

    public override void _Process(double delta)
    {
        _frame++;
        if (_frame < 2) return;
        SetProcess(false);
        IntegrationTestRunner.Run(GetTree(), _regen);
    }
}
