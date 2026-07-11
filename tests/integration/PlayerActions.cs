using Godot;

// Drives the game exactly the way a player does: by emitting the same Grid/HUD
// signals the real input handlers emit, so GameRunner's handlers and every game
// system run unchanged. Ticking is deterministic via the GameRunner test hooks.
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

    public void PlaceBrick(int col, int row)
        => GridNode.EmitSignal(Grid.SignalName.BrickRequested, col, row);

    // Furnace tool click: places a furnace on bare soil, or toggles an existing one.
    public void UseFurnace(int col, int row)
        => GridNode.EmitSignal(Grid.SignalName.FurnaceRequested, col, row);

    // Build a gold/clay autopanner on a Soil/Bank tile beside a river.
    public void BuildGoldAutopanner(int col, int row)
        => GridNode.EmitSignal(Grid.SignalName.AutopanRequested, col, row, 1);

    public void BuildClayAutopanner(int col, int row)
        => GridNode.EmitSignal(Grid.SignalName.AutopanRequested, col, row, 2);

    // Toggle an existing machine (kind 0 = whatever sits on the tile).
    public void ToggleMachine(int col, int row)
        => GridNode.EmitSignal(Grid.SignalName.AutopanRequested, col, row, 0);

    // Remove an autopanner with the dig tool (kind -1). Requires the dig tool to be active
    // in the real game; here we emit the request directly as the Grid would.
    public void RemoveMachine(int col, int row)
        => GridNode.EmitSignal(Grid.SignalName.AutopanRequested, col, row, -1);

    // Build a Shovel Rental on a Soil tile.
    public void PlaceShovelRental(int col, int row)
        => GridNode.EmitSignal(Grid.SignalName.ShovelRentalRequested, col, row);

    // Convenience: stand up the early-game "rent a shovel" loop on region 0 — a gold
    // autopanner on the watered upstream river plus a Shovel Rental it can pay for — so
    // GameState.ShovelsEnabled flips true and the dig tool works. Region 0 must be active.
    public void EnableShovels()
    {
        BuildGoldAutopanner(3, 5); // soil beside the watered upstream river (row 6)
        PlaceShovelRental(10, 10); // any soil; draws gold so the dig tool unlocks
        StepPropagation();
    }

    public void ToggleVillage(int col, int row)
        => GridNode.EmitSignal(Grid.SignalName.VillageToggleRequested, col, row);

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

    // Force a full flow + rate recompute now (independent of the tick interval).
    public void StepPropagation() => _runner.StepPropagation();
}
