public class TileEditor
{
    // Digging is free now (no shovel currency in the rate economy).
    public bool CanDig(int col, int row)
    {
        var gs = GameState.Instance;
        return gs.Tiles[row, col] != GameState.TileType.Stone
            && gs.Tiles[row, col] != GameState.TileType.Village
            && gs.Tiles[row, col] != GameState.TileType.Gate;
    }

    public void Dig(int col, int row)
    {
        var gs = GameState.Instance;
        var newType = gs.Tiles[row, col] switch
        {
            GameState.TileType.Soil => GameState.TileType.River,
            GameState.TileType.River => GameState.TileType.Soil,
            GameState.TileType.Brick => GameState.TileType.Soil,
            GameState.TileType.ShovelRental => GameState.TileType.Soil, // demolish a misplaced rental
            // Furnaces/autopanners aren't demolished by the shovel — clicking one with any
            // tool toggles it instead (handled before tool dispatch in Grid).
            _ => gs.Tiles[row, col],
        };

        gs.Tiles[row, col] = newType;
        gs.TileFurnace[row, col] = 0f;
        gs.TileMachine[row, col] = 0f;
        gs.EmitSignal(GameState.SignalName.TileChanged, col, row);
    }

    // Brick is laid on Soil. Each laid brick consumes brick/sec (BrickUse), so you can
    // only add one while the furnaces have spare brick output to sustain it.
    public bool CanPlaceBrick(int col, int row)
    {
        var gs = GameState.Instance;
        if (gs.Tiles[row, col] != GameState.TileType.Soil) return false;
        return gs.BrickGen >= gs.BrickUse + GameState.BrickUpkeepPerSec;
    }

    public void PlaceBrick(int col, int row)
    {
        var gs = GameState.Instance;
        gs.Tiles[row, col] = GameState.TileType.Brick;
        gs.TileMachine[row, col] = 0f;
        gs.EmitSignal(GameState.SignalName.TileChanged, col, row);
    }

    // Furnaces are free but capped per map. Placed on bare Soil.
    public bool CanPlaceFurnace(int col, int row)
    {
        var gs = GameState.Instance;
        if (gs.Tiles[row, col] != GameState.TileType.Soil) return false;
        return CountFurnaces() < GameState.BuildCapPerType;
    }

    public void PlaceFurnace(int col, int row)
    {
        var gs = GameState.Instance;
        gs.Tiles[row, col] = GameState.TileType.Furnace;
        gs.TileMachine[row, col] = 0f;
        gs.TileFurnace[row, col] = 0f; // enabled, zero progress
        gs.EmitSignal(GameState.SignalName.TileChanged, col, row);
    }

    // Autopanners (kind 1 = gold, 2 = clay) are built on any Soil tile, free but capped per
    // type per map. They only PRODUCE while beside a connected (watered) river (Economy),
    // but placement itself has no adjacency requirement. The machine is an overlay.
    public bool CanPlaceAutopan(int col, int row, int kind)
    {
        var gs = GameState.Instance;
        if (gs.TileMachine[row, col] != 0f) return false; // already a machine here
        if (gs.Tiles[row, col] != GameState.TileType.Soil) return false;
        return CountMachineKind(kind) < GameState.BuildCapPerType;
    }

    public void PlaceAutopan(int col, int row, int kind)
    {
        var gs = GameState.Instance;
        gs.TileMachine[row, col] = kind; // running, kind in {1,2}
        gs.EmitSignal(GameState.SignalName.TileChanged, col, row);
    }

    // Toggle a built machine between running (+kind) and paused (-kind), preserving kind.
    public void ToggleMachine(int col, int row)
    {
        var gs = GameState.Instance;
        if (gs.TileMachine[row, col] == 0f) return;
        gs.TileMachine[row, col] = -gs.TileMachine[row, col];
        gs.EmitSignal(GameState.SignalName.TileChanged, col, row);
    }

    // Remove an autopanner overlay entirely (dig tool on a machine). The land tile underneath
    // is unchanged.
    public void RemoveMachine(int col, int row)
    {
        var gs = GameState.Instance;
        if (gs.TileMachine[row, col] == 0f) return;
        gs.TileMachine[row, col] = 0f;
        gs.EmitSignal(GameState.SignalName.TileChanged, col, row);
    }

    // Shovel Rentals are built on bare Soil, free but capped per map. While supplied with
    // gold (Economy) they enable the dig tool.
    public bool CanPlaceShovelRental(int col, int row)
    {
        var gs = GameState.Instance;
        if (gs.Tiles[row, col] != GameState.TileType.Soil) return false;
        return CountShovelRentals() < GameState.BuildCapPerType;
    }

    public void PlaceShovelRental(int col, int row)
    {
        var gs = GameState.Instance;
        gs.Tiles[row, col] = GameState.TileType.ShovelRental;
        gs.TileMachine[row, col] = 0f;
        gs.TileFurnace[row, col] = 0f;
        gs.EmitSignal(GameState.SignalName.TileChanged, col, row);
    }

    // Toggle an existing furnace between enabled and disabled, preserving progress.
    public void ToggleFurnace(int col, int row)
    {
        var gs = GameState.Instance;
        if (gs.Tiles[row, col] != GameState.TileType.Furnace) return;
        gs.TileFurnace[row, col] = -gs.TileFurnace[row, col] - 1f;
        gs.EmitSignal(GameState.SignalName.TileChanged, col, row);
    }

    private static int CountMachineKind(int kind)
    {
        var gs = GameState.Instance;
        int n = 0;
        for (int row = 0; row < GameState.Rows; row++)
            for (int col = 0; col < GameState.Cols; col++)
                if (GameState.MachineKind(gs.TileMachine[row, col]) == kind) n++;
        return n;
    }

    private static int CountFurnaces()
    {
        var gs = GameState.Instance;
        int n = 0;
        for (int row = 0; row < GameState.Rows; row++)
            for (int col = 0; col < GameState.Cols; col++)
                if (gs.Tiles[row, col] == GameState.TileType.Furnace) n++;
        return n;
    }

    private static int CountShovelRentals()
    {
        var gs = GameState.Instance;
        int n = 0;
        for (int row = 0; row < GameState.Rows; row++)
            for (int col = 0; col < GameState.Cols; col++)
                if (gs.Tiles[row, col] == GameState.TileType.ShovelRental) n++;
        return n;
    }
}
