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
            _ => gs.Tiles[row, col],
        };

        gs.Tiles[row, col] = newType;
        gs.TileGold[row, col] = 0f;
        gs.TileFlowValues[row, col] = 0f;
        gs.EmitSignal(GameState.SignalName.TileChanged, col, row);
    }
}
