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
        // Listen to RatesChanged: it fires after Economy refreshes VillageSupplied each tick.
        GameState.Instance.RatesChanged += OnRatesChanged;
    }

    private void OnRatesChanged()
    {
        var gs = GameState.Instance;
        if (gs.RegionData.Count <= gs.CurrentRegion)
            return;

        // Only regions whose village was authored with an east gate gate on gold supply;
        // terminal regions (e.g. region 2) have a Stone edge and must be left alone.
        var village = VillageDefs.ForRegion(gs.CurrentZone, gs.CurrentRegion);
        if (village == null || !village.HasEastGate)
            return;

        // The gate opens once the village's gold demand is met (and shuts if it lapses).
        // Water only reaches the next map once a river is carved through the open gate.
        int vid = VillageDefs.IndexOf(village);
        bool shouldOpen = village.GoldDemand > 0f && vid >= 0 && gs.VillageSupplied[vid];

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
