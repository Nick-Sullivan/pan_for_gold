using Godot;
using System;

public class Economy
{
    public void Pan(int col, int row)
    {
        var gs = GameState.Instance;
        if (gs.Tiles[row, col] != GameState.TileType.Bank) return;

        int gold = (int)gs.TileGold[row, col];
        gs.TileGold[row, col] = 0f;
        gs.EmitSignal(GameState.SignalName.TileGoldChanged, col, row, 0);
        if (gold > 0)
        {
            gs.Gold += gold;
            gs.EmitSignal(GameState.SignalName.GoldChanged, gs.Gold);
        }

        int clay = (int)gs.TileClay[row, col];
        gs.TileClay[row, col] = 0f;
        gs.EmitSignal(GameState.SignalName.TileClayChanged, col, row, 0);
        if (clay > 0)
        {
            gs.Clay += clay;
            gs.EmitSignal(GameState.SignalName.ClayChanged, gs.Clay);
        }
    }

    public void BuyShovel()
    {
        var gs = GameState.Instance;
        if (gs.Gold < GameState.ShovelCost)
            return;
        gs.Gold -= GameState.ShovelCost;
        gs.EmitSignal(GameState.SignalName.GoldChanged, gs.Gold);
        gs.Shovels++;
        gs.EmitSignal(GameState.SignalName.ShovelsChanged, gs.Shovels);
    }

    public static float FlowMultiplier(float flow, float maxFlow)
        => (float)Math.Clamp(flow / maxFlow, 0.0, 1.0);

    public void TickGold(double delta)
    {
        var gs = GameState.Instance;
        for (int regionIdx = 0; regionIdx < gs.RegionData.Count; regionIdx++)
        {
            var snap = gs.RegionData[regionIdx];
            bool isActive = regionIdx == gs.CurrentRegion;
            var tiles = snap.Tiles;
            var gold = snap.Gold;

            bool sourceActive = isActive && HasActiveSource(GameState.TileType.GoldSource, tiles, gs.TileFlowValues);

            for (int row = 0; row < GameState.Rows; row++)
                for (int col = 0; col < GameState.Cols; col++)
                {
                    if (tiles[row, col] != GameState.TileType.Bank) continue;
                    if (!sourceActive && !AdjRiver(col, row, tiles)) continue;

                    int oldInt = (int)gold[row, col];
                    float baseRate = isActive ? gs.RiverSpeed : 1.0f;
                    float tileFlow = (!isActive || sourceActive) ? GameState.MaxBankFlow : gs.TileFlowValues[row, col];
                    float rate = baseRate * FlowMultiplier(tileFlow, GameState.MaxBankFlow);
                    gold[row, col] = Mathf.Min(
                        gold[row, col] + (float)delta * rate / GameState.RefillTime * GameState.MaxTileGold,
                        GameState.MaxTileGold
                    );
                    int newInt = (int)gold[row, col];
                    if (newInt != oldInt && isActive)
                        gs.EmitSignal(GameState.SignalName.TileGoldChanged, col, row, newInt);
                }
        }
    }

    public void TickClay(double delta)
    {
        var gs = GameState.Instance;

        bool hasClaySource = false;
        for (int z = 0; z < MapLayouts.Maps.Length && !hasClaySource; z++)
            foreach (var snap in gs.GetZoneData(z))
                for (int r = 0; r < GameState.Rows && !hasClaySource; r++)
                    for (int c = 0; c < GameState.Cols && !hasClaySource; c++)
                        if (snap.Tiles[r, c] == GameState.TileType.ClaySource) hasClaySource = true;
        if (!hasClaySource) return;

        for (int z = 0; z < MapLayouts.Maps.Length; z++)
        {
            var zoneData = gs.GetZoneData(z);
            for (int regionIdx = 0; regionIdx < zoneData.Count; regionIdx++)
            {
                var snap = zoneData[regionIdx];
                bool isActive = z == gs.CurrentZone && regionIdx == gs.CurrentRegion;
                var tiles = snap.Tiles;
                var clay = snap.Clay;

                for (int row = 0; row < GameState.Rows; row++)
                    for (int col = 0; col < GameState.Cols; col++)
                    {
                        if (tiles[row, col] != GameState.TileType.Bank) continue;

                        int oldInt = (int)clay[row, col];
                        float baseRate = isActive ? gs.RiverSpeed : 1.0f;
                        clay[row, col] = Mathf.Min(
                            clay[row, col] + (float)delta * baseRate / GameState.RefillTime * GameState.MaxTileClay,
                            GameState.MaxTileClay
                        );
                        int newInt = (int)clay[row, col];
                        if (newInt != oldInt && isActive)
                            gs.EmitSignal(GameState.SignalName.TileClayChanged, col, row, newInt);
                    }
            }
        }
    }

    private static bool HasActiveSource(GameState.TileType sourceType, GameState.TileType[,] tiles, float[,] flowValues)
    {
        for (int row = 0; row < GameState.Rows; row++)
            for (int col = 0; col < GameState.Cols; col++)
            {
                if (tiles[row, col] != sourceType) continue;
                if (AdjFlowingRiver(col, row, tiles, flowValues)) return true;
            }
        return false;
    }

    private static bool AdjRiver(int col, int row, GameState.TileType[,] tiles)
    {
        (int dc, int dr)[] dirs = [(1, 0), (-1, 0), (0, 1), (0, -1)];
        foreach (var (dc, dr) in dirs)
        {
            int nc = col + dc, nr = row + dr;
            if (nc < 0 || nc >= GameState.Cols || nr < 0 || nr >= GameState.Rows) continue;
            var t = tiles[nr, nc];
            if (t == GameState.TileType.River || t == GameState.TileType.RiverSource) return true;
        }
        return false;
    }

    private static bool AdjFlowingRiver(int col, int row, GameState.TileType[,] tiles, float[,] flowValues)
    {
        (int dc, int dr)[] dirs = [(1, 0), (-1, 0), (0, 1), (0, -1)];
        foreach (var (dc, dr) in dirs)
        {
            int nc = col + dc, nr = row + dr;
            if (nc < 0 || nc >= GameState.Cols || nr < 0 || nr >= GameState.Rows) continue;
            var t = tiles[nr, nc];
            if (t != GameState.TileType.River && t != GameState.TileType.RiverSource) continue;
            if (flowValues == null || flowValues[nr, nc] > 0) return true;
        }
        return false;
    }
}
