public class TileEditor
{
    public bool CanDig(int col, int row)
    {
        var gs = GameState.Instance;
        return gs.Shovels > 0
            && gs.Tiles[row, col] != GameState.TileType.Stone
            && gs.Tiles[row, col] != GameState.TileType.Village
            && gs.Tiles[row, col] != GameState.TileType.Gate;
    }

    public void Dig(int col, int row)
    {
        var gs = GameState.Instance;
        var newType = gs.Tiles[row, col] switch
        {
            GameState.TileType.Soil => GameState.TileType.River,
            GameState.TileType.River => GameState.TileType.Bank,
            GameState.TileType.Bank => GameState.TileType.Soil,
            GameState.TileType.Brick => GameState.TileType.Soil,
            _ => gs.Tiles[row, col],
        };

        gs.Tiles[row, col] = newType;
        gs.TileGold[row, col] = 0f;
        gs.TileFlowValues[row, col] = 0f;
        gs.EmitSignal(GameState.SignalName.TileChanged, col, row);
    }

    // Brick can be laid on a Soil/Bank tile that borders the river, to stop the
    // river losing flow to that neighbour.
    public bool CanPlaceBrick(int col, int row)
    {
        var gs = GameState.Instance;
        if (gs.Bricks <= 0) return false;
        var t = gs.Tiles[row, col];
        if (t != GameState.TileType.Soil && t != GameState.TileType.Bank) return false;
        return AdjRiver(col, row);
    }

    public void PlaceBrick(int col, int row)
    {
        var gs = GameState.Instance;
        gs.Tiles[row, col] = GameState.TileType.Brick;
        gs.TileGold[row, col] = 0f;
        gs.TileClay[row, col] = 0f;
        gs.TileFlowValues[row, col] = 0f;
        gs.EmitSignal(GameState.SignalName.TileChanged, col, row);
    }

    private static bool AdjRiver(int col, int row)
    {
        var gs = GameState.Instance;
        (int dc, int dr)[] dirs = [(1, 0), (-1, 0), (0, 1), (0, -1)];
        foreach (var (dc, dr) in dirs)
        {
            int nc = col + dc, nr = row + dr;
            if (nc < 0 || nc >= GameState.Cols || nr < 0 || nr >= GameState.Rows) continue;
            var t = gs.Tiles[nr, nc];
            if (t == GameState.TileType.River || t == GameState.TileType.RiverSource) return true;
        }
        return false;
    }
}
