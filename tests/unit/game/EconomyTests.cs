namespace pan_for_gold.Tests;

// Pure-logic tests for the scalar flow model (no Godot runtime). Consumption only
// counts Soil tiles beside a river that is CONNECTED to a RiverSource.
public class FlowModelTests
{
    private static GameState.TileType[,] Blank()
        => new GameState.TileType[GameState.Rows, GameState.Cols]; // all Soil (enum 0)

    [Fact]
    public void CountConsumingTiles_NoRiver_ReturnsZero()
        => Assert.Equal(0, FlowModel.CountConsumingTiles(Blank()));

    [Fact]
    public void CountConsumingTiles_DisconnectedRiver_CountsZero()
    {
        var t = Blank();
        t[5, 5] = GameState.TileType.River; // a River with no RiverSource -> not connected -> dry
        Assert.Equal(0, FlowModel.CountConsumingTiles(t));
    }

    [Fact]
    public void CountConsumingTiles_ConnectedRiver_CountsAdjacentSoil()
    {
        var t = Blank();
        t[5, 5] = GameState.TileType.RiverSource; // a source IS connected (watered)
        Assert.Equal(4, FlowModel.CountConsumingTiles(t)); // its 4 Soil neighbours
    }

    [Fact]
    public void ConnectedRiver_SpreadsFromSourceThroughRiver()
    {
        var t = Blank();
        t[5, 5] = GameState.TileType.RiverSource;
        t[5, 6] = GameState.TileType.River;  // adjacent -> connected
        t[5, 8] = GameState.TileType.River;  // gap at col 7 -> NOT connected
        var connected = FlowModel.ConnectedRiver(t);
        Assert.True(connected[5, 5]);
        Assert.True(connected[5, 6]);
        Assert.False(connected[5, 8]);
    }

    [Fact]
    public void ReachesEastEdge_ConnectedRiverToEdge_True()
    {
        var t = Blank();
        t[6, GameState.Cols - 2] = GameState.TileType.RiverSource;
        t[6, GameState.Cols - 1] = GameState.TileType.River; // reaches the east edge, connected
        Assert.True(FlowModel.ReachesEastEdge(FlowModel.ConnectedRiver(t)));
    }

    [Fact]
    public void ReachesEastEdge_GapBeforeEdge_False()
    {
        var t = Blank();
        t[6, 0] = GameState.TileType.RiverSource;
        t[6, 1] = GameState.TileType.River;
        t[6, GameState.Cols - 1] = GameState.TileType.River; // at the edge but disconnected (gap)
        Assert.False(FlowModel.ReachesEastEdge(FlowModel.ConnectedRiver(t)));
    }
}
