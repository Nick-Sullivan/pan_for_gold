using Godot;

public class WaterPropagation
{
    private RiverDAG _dag = new();

    public void InitTiles()
    {
        var gs = GameState.Instance;
        for (int z = 0; z < MapLayouts.Maps.Length; z++)
        {
            var tiles = MapLayouts.BuildTiles(MapLayouts.Maps[z][0]);
            var gold  = MapLayouts.BuildGold();
            var clay  = MapLayouts.BuildClay();
            gs.GetZoneData(z).Clear();
            gs.GetZoneData(z).Add(new GameState.RegionSnapshot(tiles, gold, clay,
                new float[GameState.Rows, GameState.Cols], new float[GameState.Rows, GameState.Cols],
                new float[GameState.Rows, GameState.Cols]));
        }
        gs.CurrentZone = 0;
        gs.SwapActiveTo(0);
        _dag = new RiverDAG();
    }

    public void OnRegionSwitch(int _index)
    {
        var gs = GameState.Instance;
        _dag = FlowSteadyState.Calculate(BuildTileCells(gs), GetEntryFlows(gs));
        WriteFlowValues(gs);
        gs.EmitSignal(GameState.SignalName.FlowChanged);
    }

    public void Propagate()
    {
        var gs = GameState.Instance;
        _dag = FlowSteadyState.Calculate(BuildTileCells(gs), GetEntryFlows(gs));
        WriteFlowValues(gs);
        gs.EmitSignal(GameState.SignalName.FlowChanged);
    }

    private static float[] GetEntryFlows(GameState gs)
    {
        int prevIdx = gs.CurrentRegion - 1;
        if (prevIdx < 0)
            return null;
        var prevFlow = gs.RegionData[prevIdx].Flow;
        var flows = new float[GameState.Rows];
        for (int row = 0; row < GameState.Rows; row++)
            flows[row] = prevFlow[row, GameState.Cols - 1];
        return flows;
    }

    private static TileCell[,] BuildTileCells(GameState gs)
    {
        var cells = new TileCell[GameState.Cols, GameState.Rows];
        for (int row = 0; row < GameState.Rows; row++)
            for (int col = 0; col < GameState.Cols; col++)
                cells[col, row] = gs.Tiles[row, col];
        return cells;
    }

    private void WriteFlowValues(GameState gs)
    {
        System.Array.Clear(gs.TileFlowValues);
        foreach (var id in _dag.NodeIds)
            gs.TileFlowValues[id.Y, id.X] = _dag.GetNode(id).Value.FlowRate;
    }
}
