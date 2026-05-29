public class GateSystem
{
    private readonly RegionSystem _regions;
    public bool IsGateOpen { get; private set; }

    public GateSystem(RegionSystem regions)
    {
        _regions = regions;
    }

    public void Connect()
    {
        GameState.Instance.FlowChanged += OnFlowChanged;
    }

    private void OnFlowChanged()
    {
        var gs = GameState.Instance;
        if (gs.CurrentRegion != 1 || gs.RegionData.Count <= 1)
            return;

        float villageFlow = gs.TileFlowValues[GameState.VillageRow, GameState.VillageCol];
        bool shouldOpen = villageFlow >= GameState.VillageFlowThreshold;

        if (shouldOpen == IsGateOpen)
            return;

        IsGateOpen = shouldOpen;

        for (int row = 0; row < GameState.Rows; row++)
        {
            bool isCorner = row == 0 || row == GameState.Rows - 1;
            var t = gs.Tiles[row, GameState.Cols - 1];

            if (shouldOpen && t == GameState.TileType.Gate)
            {
                gs.Tiles[row, GameState.Cols - 1] = isCorner
                    ? GameState.TileType.Stone
                    : GameState.TileType.Soil;
                gs.TileFlowValues[row, GameState.Cols - 1] = 0f;
                gs.EmitSignal(GameState.SignalName.TileChanged, GameState.Cols - 1, row);
            }
            else if (!shouldOpen && !isCorner && t != GameState.TileType.Gate)
            {
                gs.Tiles[row, GameState.Cols - 1] = GameState.TileType.Gate;
                gs.TileFlowValues[row, GameState.Cols - 1] = 0f;
                gs.EmitSignal(GameState.SignalName.TileChanged, GameState.Cols - 1, row);
            }
        }
    }
}
