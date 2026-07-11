using Godot;

// Rate-based economy (no accumulation). Every tick, Recompute() derives each map's
// generation/consumption rates from its scalar input flow and the buildings on it:
//   gold gen = (running gold autopanners) * AutopanYieldPerFlow * input flow   [if gold source fed]
//   clay gen = (running clay autopanners) * AutopanYieldPerFlow * input flow   [if clay source fed]
//   clay use = (running furnaces) * FurnaceClayPerSec ; brick gen if clay covers it
//   gold use = sum of discovered, supply-on village demand on that map
// The current map's rates are published to GameState for the HUD; every map's villages
// are evaluated for "supplied" (gen >= demand) so off-screen progress still counts.
public class Economy
{
    public void Recompute()
    {
        var gs = GameState.Instance;
        bool goldFed = SourceFed(GameState.TileType.GoldSource);
        bool clayFed = SourceFed(GameState.TileType.ClaySource);
        bool shovelsEnabled = false;

        for (int z = 0; z < MapLayouts.Maps.Length; z++)
        {
            var zoneData = gs.GetZoneData(z);
            for (int r = 0; r < zoneData.Count; r++)
            {
                var snap = zoneData[r];
                float input = snap.InputFlow;
                CountBuildings(snap, out int goldMachines, out int clayMachines, out int furnaces, out int bricks, out int rentals);

                float goldGen = goldFed ? goldMachines * GameState.AutopanYieldPerFlow * input : 0f;
                float clayGen = clayFed ? clayMachines * GameState.AutopanYieldPerFlow * input : 0f;
                float clayUse = furnaces * GameState.FurnaceClayPerSec;
                bool clayCovered = furnaces > 0 && clayGen >= clayUse;
                float brickGen = clayCovered ? furnaces * GameState.BrickPerFurnacePerSec : 0f;
                float brickUse = bricks * GameState.BrickUpkeepPerSec;

                // Shovel Rentals draw gold; while this map's gold gen covers its rentals the
                // dig tool is enabled (OR'd across maps below).
                float rentalUse = rentals * GameState.ShovelRentalGoldPerSec;
                if (rentals > 0 && goldGen >= rentalUse) shovelsEnabled = true;

                var village = VillageDefs.ForRegion(z, r);
                int vid = village != null ? VillageDefs.IndexOf(village) : -1;
                bool demanding = village != null && village.GoldDemand > 0f && vid >= 0
                    && gs.VillagesDiscovered[vid] && gs.VillageSupplyOn[vid];
                float goldUse = (demanding ? village.GoldDemand : 0f) + rentalUse;

                bool isActive = z == gs.CurrentZone && r == gs.CurrentRegion;
                if (isActive)
                {
                    gs.GoldGen = goldGen; gs.GoldUse = goldUse;
                    gs.ClayGen = clayGen; gs.ClayUse = clayUse;
                    gs.BrickGen = brickGen; gs.BrickUse = brickUse;
                }

                // Per-village supplied state (gen meets demand) — drives quest 6/8 and tint.
                if (village != null && village.GoldDemand > 0f && vid >= 0)
                {
                    bool supplied = demanding && goldGen >= village.GoldDemand;
                    if (supplied != gs.VillageSupplied[vid])
                    {
                        gs.VillageSupplied[vid] = supplied;
                        gs.EmitSignal(GameState.SignalName.VillageSupplyChanged, vid, supplied);
                        if (isActive)
                            gs.EmitSignal(GameState.SignalName.TileChanged, village.Col, village.Row);
                    }
                }
            }
        }

        gs.ShovelsEnabled = shovelsEnabled;
        gs.EmitSignal(GameState.SignalName.RatesChanged);
    }

    // Counts on a map: running autopanners by kind that are ACTIVE (adjacent to a watered
    // river), enabled furnaces, laid Brick tiles, and Shovel Rentals. Paused/inactive
    // machines don't count.
    private static void CountBuildings(GameState.RegionSnapshot snap, out int gold, out int clay, out int furnaces, out int bricks, out int rentals)
    {
        gold = clay = furnaces = bricks = rentals = 0;
        for (int row = 0; row < GameState.Rows; row++)
            for (int col = 0; col < GameState.Cols; col++)
            {
                float m = snap.Machine[row, col];
                if (GameState.MachineRunning(m) && AdjWateredRiver(snap, col, row))
                {
                    int k = GameState.MachineKind(m);
                    if (k == 1) gold++;
                    else if (k == 2) clay++;
                }
                if (snap.Tiles[row, col] == GameState.TileType.Furnace && snap.Furnace[row, col] >= 0f)
                    furnaces++;
                if (snap.Tiles[row, col] == GameState.TileType.Brick) bricks++;
                if (snap.Tiles[row, col] == GameState.TileType.ShovelRental) rentals++;
            }
    }

    // True if a tile is orthogonally adjacent to a watered river (snap.Flow > 0, set by
    // FlowModel only on river tiles connected to a source).
    private static bool AdjWateredRiver(GameState.RegionSnapshot snap, int col, int row)
    {
        (int dc, int dr)[] dirs = [(1, 0), (-1, 0), (0, 1), (0, -1)];
        foreach (var (dc, dr) in dirs)
        {
            int nc = col + dc, nr = row + dr;
            if (nc < 0 || nc >= GameState.Cols || nr < 0 || nr >= GameState.Rows) continue;
            if (snap.Flow[nr, nc] > 0f) return true;
        }
        return false;
    }

    // A material is producible only when its source (gold/clay, in the highlands) has a
    // river tile next to it anywhere. Route the river beside a source to "switch it on".
    // Gold ships fed; clay needs the highlands routed (quest "feed the clay").
    public static bool SourceFed(GameState.TileType sourceType)
    {
        var gs = GameState.Instance;
        for (int z = 0; z < MapLayouts.Maps.Length; z++)
            foreach (var snap in gs.GetZoneData(z))
                for (int row = 0; row < GameState.Rows; row++)
                    for (int col = 0; col < GameState.Cols; col++)
                        if (snap.Tiles[row, col] == sourceType && AdjRiver(col, row, snap.Tiles))
                            return true;
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
}
