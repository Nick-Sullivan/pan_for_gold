using Godot;

// Scalar per-map flow model. Replaces the per-tile DAG in FlowPropagation /
// FlowSteadyState / WaterPropagation.Propagate (all left in the tree, unused).
//
// Each region has a single InputFlow. OutputFlow = Input minus the flow consumed by
// Soil/Bank tiles adjacent to a river (Brick-lined banks are exempt — that's how you
// keep a channel's flow up). A region's OutputFlow becomes the next region's InputFlow;
// the first region of each zone starts from BaseInflow. The next region unlocks when a
// region's OutputFlow > 0 (handled in GameRunner/RegionSystem).
public class FlowModel
{
    public void Recompute()
    {
        var gs = GameState.Instance;
        for (int z = 0; z < MapLayouts.Maps.Length; z++)
        {
            var zoneData = gs.GetZoneData(z);
            float input = GameState.BaseInflow;
            for (int r = 0; r < zoneData.Count; r++)
            {
                var snap = zoneData[r];
                snap.InputFlow = input;
                var connected = ConnectedRiver(snap.Tiles);
                float consumption = CountConsumingTiles(snap.Tiles, connected) * GameState.FlowCostPerTile;
                // Water only leaves the map if a connected river actually reaches the east
                // edge — a gap in the channel means no output downstream (and no unlock).
                float output = ReachesEastEdge(connected) ? Mathf.Max(0f, input - consumption) : 0f;
                snap.OutputFlow = output;
                WriteWateredFlow(snap, connected, input);
                input = output; // cascade downstream
            }
        }
        gs.EmitSignal(GameState.SignalName.FlowChanged);
    }

    // Which river tiles are connected to a RiverSource (BFS over orthogonal river
    // neighbours). Only connected river "has water"; disconnected segments are dry.
    public static bool[,] ConnectedRiver(GameState.TileType[,] tiles)
    {
        var connected = new bool[GameState.Rows, GameState.Cols];
        var queue = new System.Collections.Generic.Queue<Vector2I>();
        for (int row = 0; row < GameState.Rows; row++)
            for (int col = 0; col < GameState.Cols; col++)
                if (tiles[row, col] == GameState.TileType.RiverSource)
                {
                    connected[row, col] = true;
                    queue.Enqueue(new Vector2I(col, row));
                }

        (int dc, int dr)[] dirs = [(1, 0), (-1, 0), (0, 1), (0, -1)];
        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            foreach (var (dc, dr) in dirs)
            {
                int nc = p.X + dc, nr = p.Y + dr;
                if (nc < 0 || nc >= GameState.Cols || nr < 0 || nr >= GameState.Rows) continue;
                if (connected[nr, nc]) continue;
                var t = tiles[nr, nc];
                if (t != GameState.TileType.River && t != GameState.TileType.RiverSource) continue;
                connected[nr, nc] = true;
                queue.Enqueue(new Vector2I(nc, nr));
            }
        }
        return connected;
    }

    // True if any CONNECTED (watered) river tile sits on the east edge (col Cols-1), i.e.
    // the river runs all the way through and carries flow to the next map.
    public static bool ReachesEastEdge(bool[,] connected)
    {
        for (int row = 0; row < GameState.Rows; row++)
            if (connected[row, GameState.Cols - 1]) return true;
        return false;
    }

    // Output flow of the active region (drives unlock / gate / progression).
    public float ActiveOutputFlow()
    {
        var gs = GameState.Instance;
        return gs.RegionData.Count > gs.CurrentRegion
            ? gs.RegionData[gs.CurrentRegion].OutputFlow
            : 0f;
    }

    // Count of Soil tiles orthogonally adjacent to a CONNECTED (watered) river tile —
    // each such tile consumes flow. Computes connectivity itself (testable on raw tiles).
    public static int CountConsumingTiles(GameState.TileType[,] tiles)
        => CountConsumingTiles(tiles, ConnectedRiver(tiles));

    public static int CountConsumingTiles(GameState.TileType[,] tiles, bool[,] connected)
    {
        int count = 0;
        for (int row = 0; row < GameState.Rows; row++)
            for (int col = 0; col < GameState.Cols; col++)
            {
                if (tiles[row, col] != GameState.TileType.Soil) continue;
                if (AdjConnectedRiver(col, row, connected)) count++;
            }
        return count;
    }

    private static bool AdjConnectedRiver(int col, int row, bool[,] connected)
    {
        (int dc, int dr)[] dirs = [(1, 0), (-1, 0), (0, 1), (0, -1)];
        foreach (var (dc, dr) in dirs)
        {
            int nc = col + dc, nr = row + dr;
            if (nc < 0 || nc >= GameState.Cols || nr < 0 || nr >= GameState.Rows) continue;
            if (connected[nr, nc]) return true;
        }
        return false;
    }

    // Fill per-tile Flow: a CONNECTED river tile gets the map's input flow ("watered");
    // everything else (disconnected river, land) gets 0, so the shader renders dry.
    private static void WriteWateredFlow(GameState.RegionSnapshot snap, bool[,] connected, float input)
    {
        for (int row = 0; row < GameState.Rows; row++)
            for (int col = 0; col < GameState.Cols; col++)
                snap.Flow[row, col] = connected[row, col] ? input : 0f;
    }
}
