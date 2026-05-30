using Godot;

// Drives the game exactly the way a player does: by emitting the same Grid/HUD
// signals the real input handlers emit, so GameRunner's handlers and every game
// system run unchanged. This is the vocabulary "full setup" fixtures are built
// from. Ticking is deterministic via the GameRunner test hooks.
public class PlayerActions
{
    private readonly SceneTree _tree;
    private readonly GameRunner _runner;

    public PlayerActions(SceneTree tree, GameRunner runner)
    {
        _tree = tree;
        _runner = runner;
    }

    private Grid GridNode => _tree.GetFirstNodeInGroup("grid") as Grid;
    private HUD HudNode => _tree.GetFirstNodeInGroup("hud") as HUD;

    public void Dig(int col, int row)
        => GridNode.EmitSignal(Grid.SignalName.DigRequested, col, row);

    public void Pan(int col, int row)
        => GridNode.EmitSignal(Grid.SignalName.PanRequested, col, row);

    public void BuyShovel()
        => HudNode.EmitSignal(HUD.SignalName.BuyShovelRequested);

    public void BuyFurnace()
        => HudNode.EmitSignal(HUD.SignalName.BuyFurnaceRequested);

    public void MakeBrick()
        => HudNode.EmitSignal(HUD.SignalName.MakeBrickRequested);

    public void PlaceBrick(int col, int row)
        => GridNode.EmitSignal(Grid.SignalName.BrickRequested, col, row);

    public void SetTool(int tool)
        => HudNode.EmitSignal(HUD.SignalName.ToolSelected, tool);

    public void SwitchRegion(int index)
        => HudNode.EmitSignal(HUD.SignalName.RegionSelected, index);

    public void SwitchZone(int zone)
        => HudNode.EmitSignal(HUD.SignalName.ZoneSwitchRequested, zone);

    public void Save()
        => HudNode.EmitSignal(HUD.SignalName.SaveRequested);

    // Advance the simulation by n deterministic ticks.
    public void StepTicks(int n, double dt = 0.2)
    {
        for (int i = 0; i < n; i++)
            _runner.Tick(dt);
    }

    public void StepPropagation() => _runner.StepPropagation();

    // Tick a bank tile until it has earned at least `target` gold, then pan it.
    // Guarded by a tick budget so a misconfigured tile can't loop forever.
    public void PanUntilGold(int target, int col, int row, int maxTicks = 2000)
    {
        var gs = GameState.Instance;
        int ticks = 0;
        while (gs.Gold < target && ticks++ < maxTicks)
        {
            StepTicks(1);
            if ((int)gs.TileGold[row, col] > 0)
                Pan(col, row);
        }
    }
}
