public class RegionSystem
{
    public void Init()
    {
        var gs = GameState.Instance;
        gs.CurrentRegion = 0;
        gs.UnlockedRegions = 1;
    }

    // The next region unlocks once this map passes flow downstream — i.e. its output
    // flow is above 0 (input minus what the bare banks consume). Brick-line the channel
    // to push output positive.
    public void TryUnlock()
    {
        var gs = GameState.Instance;
        if (gs.CurrentZone != 0) return;
        if (gs.UnlockedRegions > gs.CurrentRegion + 1)
            return;
        if (gs.RegionData.Count <= gs.CurrentRegion) return;
        if (gs.RegionData[gs.CurrentRegion].OutputFlow <= 0f)
            return;

        CreateNewRegion(gs, ExitRows(gs));
        gs.UnlockedRegions++;
        gs.EmitSignal(GameState.SignalName.RegionUnlocked, gs.UnlockedRegions);
    }

    public void SwitchZone(int zone)
    {
        var gs = GameState.Instance;
        if (zone == gs.CurrentZone) return;
        gs.CurrentZone = zone;
        gs.SwapActiveTo(gs.CurrentRegion);
        gs.EmitSignal(GameState.SignalName.ZoneChanged, zone);
    }

    public void SwitchTo(int index)
    {
        var gs = GameState.Instance;
        if (index < 0 || index >= gs.UnlockedRegions || index == gs.CurrentRegion)
            return;

        gs.CurrentRegion = index;
        gs.SwapActiveTo(index);
        gs.EmitSignal(GameState.SignalName.RegionSwitched, index);
    }

    public void SyncNextEntries()
    {
        var gs = GameState.Instance;
        int nextIdx = gs.CurrentRegion + 1;
        if (nextIdx >= gs.RegionData.Count)
            return;

        var exitRows = ExitRows(gs);
        var nextTiles = gs.RegionData[nextIdx].Tiles;

        for (int row = 0; row < GameState.Rows; row++)
        {
            bool shouldBeRiver = exitRows.Contains(row);
            bool isRiver = nextTiles[row, 0] == GameState.TileType.River
                        || nextTiles[row, 0] == GameState.TileType.RiverSource;
            if (shouldBeRiver && !isRiver)
                nextTiles[row, 0] = GameState.TileType.RiverSource;
            else if (!shouldBeRiver && isRiver)
                nextTiles[row, 0] = GameState.TileType.Soil;
        }
    }

    private static System.Collections.Generic.List<int> ExitRows(GameState gs)
    {
        var rows = new System.Collections.Generic.List<int>();
        for (int row = 0; row < GameState.Rows; row++)
            if (gs.Tiles[row, GameState.Cols - 1] == GameState.TileType.River)
                rows.Add(row);
        return rows;
    }

    private static void CreateNewRegion(GameState gs, System.Collections.Generic.List<int> exitRows)
    {
        int newIndex = gs.RegionData.Count;
        var zoneMaps = MapLayouts.Maps[gs.CurrentZone];
        GameState.TileType[,] tiles;

        if (zoneMaps.Length > newIndex)
        {
            tiles = MapLayouts.BuildTiles(zoneMaps[newIndex]);
        }
        else
        {
            tiles = new GameState.TileType[GameState.Rows, GameState.Cols];
        }

        // Seed the entry (col 0). If the player carved the river to the east edge, mirror
        // those exit rows; otherwise keep the authored layout's entry (or seed row 6 for a
        // blank map) so the new region always has a source to build from.
        if (exitRows.Count > 0)
        {
            for (int row = 0; row < GameState.Rows; row++)
                tiles[row, 0] = exitRows.Contains(row)
                    ? GameState.TileType.RiverSource
                    : GameState.TileType.Soil;
        }
        else if (zoneMaps.Length <= newIndex)
        {
            tiles[6, 0] = GameState.TileType.RiverSource;
        }

        var gold = MapLayouts.BuildGold();
        var clay = MapLayouts.BuildClay();
        gs.RegionData.Add(new GameState.RegionSnapshot(tiles, gold, clay,
            new float[GameState.Rows, GameState.Cols], new float[GameState.Rows, GameState.Cols],
            new float[GameState.Rows, GameState.Cols]));
    }
}
