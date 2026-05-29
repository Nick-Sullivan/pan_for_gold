using Godot;
using static GameState.TileType;

namespace pan_for_gold.Tests;

public class FlowSteadyStateTests
{
    private static Vector2I V(int col, int row) => TestTiles.V(col, row);
    private static TileCell S() => TestTiles.S();
    private static TileCell R() => TestTiles.R();
    private static TileCell RS() => TestTiles.RS();
    private static TileCell[,] Grid(params TileCell[][] rows) => TestTiles.Grid(rows);

    [Fact]
    public void WhenNoTiles_ReturnsEmptyDAG()
    {
        var river = FlowSteadyState.Calculate(new TileCell[0, 0]);
        Assert.Equal(0, river.NumNodes);
    }

    [Fact]
    public void WhenNoSources_AllNodesHaveZeroFlow()
    {
        var tiles = Grid([S(), R(), S()]);
        var river = FlowSteadyState.Calculate(tiles);
        Assert.Equal(0, river.GetNode(V(0, 0)).Value.FlowRate);
        Assert.Equal(0, river.GetNode(V(1, 0)).Value.FlowRate);
        Assert.Equal(0, river.GetNode(V(2, 0)).Value.FlowRate);
    }

    [Fact]
    public void WhenSingleSource_SourceNodeHasFullFlow()
    {
        var tiles = Grid([RS()]);
        var river = FlowSteadyState.Calculate(tiles);
        Assert.Equal(1000, river.GetNode(V(0, 0)).Value.FlowRate);
    }

    [Fact]
    public void WhenSourceAdjacentToSoil_GivesFlowToSoil()
    {
        var tiles = Grid([RS(), S()]);
        var river = FlowSteadyState.Calculate(tiles);
        Assert.Equal(1000, river.GetNode(V(0, 0)).Value.FlowRate);
        Assert.Equal(20, river.GetNode(V(1, 0)).Value.FlowRate);
        Assert.Equal(20, river.GetEdge(V(0, 0), V(1, 0)).Value.FlowRate);
    }

    [Fact]
    public void WhenSourceAdjacentToRiver_PassesFlowDownstream()
    {
        var tiles = Grid([RS(), R()]);
        var river = FlowSteadyState.Calculate(tiles);
        Assert.Equal(1000, river.GetNode(V(0, 0)).Value.FlowRate);
        Assert.Equal(1000, river.GetNode(V(1, 0)).Value.FlowRate);
        Assert.Equal(1000, river.GetEdge(V(0, 0), V(1, 0)).Value.FlowRate);
    }

    [Fact]
    public void WhenRiverSplitsInTwo_FlowIsHalved()
    {
        var tiles = Grid(
            [S(), S(), S()],
            [RS(), R(), R()],
            [S(), R(), S()]
        );
        var river = FlowSteadyState.Calculate(tiles);
        Assert.Equal(1000, river.GetNode(V(0, 1)).Value.FlowRate);
        Assert.Equal(960, river.GetNode(V(1, 1)).Value.FlowRate);
        Assert.Equal(470, river.GetNode(V(2, 1)).Value.FlowRate);
        Assert.Equal(470, river.GetNode(V(1, 2)).Value.FlowRate);
        Assert.Equal(20, river.GetNode(V(0, 0)).Value.FlowRate);
        Assert.Equal(20, river.GetNode(V(1, 0)).Value.FlowRate);
        Assert.Equal(20, river.GetNode(V(2, 0)).Value.FlowRate);
        Assert.Equal(40, river.GetNode(V(0, 2)).Value.FlowRate);
        Assert.Equal(40, river.GetNode(V(2, 2)).Value.FlowRate);

        Assert.Equal(960, river.GetEdge(V(0, 1), V(1, 1)).Value.FlowRate);
        Assert.Equal(470, river.GetEdge(V(1, 1), V(2, 1)).Value.FlowRate);
        Assert.Equal(470, river.GetEdge(V(1, 1), V(1, 2)).Value.FlowRate);
    }

    [Fact]
    public void WhenRiversRecombine_FlowAccumulates()
    {
        var tiles = Grid(
            [R(), RS(), R()],
            [R(), S(), R()],
            [R(), S(), R()],
            [R(), R(), R()]
        );
        var river = FlowSteadyState.Calculate(tiles);
        Assert.Equal(1000, river.GetNode(V(1, 0)).Value.FlowRate);
        // Left
        Assert.Equal(490, river.GetNode(V(0, 0)).Value.FlowRate);
        Assert.Equal(490, river.GetNode(V(0, 1)).Value.FlowRate);
        Assert.Equal(470, river.GetNode(V(0, 2)).Value.FlowRate);
        Assert.Equal(450, river.GetNode(V(0, 3)).Value.FlowRate);
        // Right
        Assert.Equal(490, river.GetNode(V(2, 0)).Value.FlowRate);
        Assert.Equal(490, river.GetNode(V(2, 1)).Value.FlowRate);
        Assert.Equal(470, river.GetNode(V(2, 2)).Value.FlowRate);
        Assert.Equal(450, river.GetNode(V(2, 3)).Value.FlowRate);
        // Combined
        Assert.Equal(900, river.GetNode(V(1, 3)).Value.FlowRate);
        Assert.Equal(2, river.GetParentEdges(V(1, 3)).Count);
    }

    [Fact]
    public void WhenRiversRecombine_FlowAccumulates2()
    {
        var tiles = Grid(
            [RS(), R(), R(), R()],
            [S(), R(), S(), R()],
            [S(), R(), R(), R()]
        );
        var river = FlowSteadyState.Calculate(tiles);
        Assert.Equal(1000, river.GetNode(V(0, 0)).Value.FlowRate);
        Assert.Equal(980, river.GetNode(V(1, 0)).Value.FlowRate);
        // Down
        Assert.Equal(490, river.GetNode(V(1, 1)).Value.FlowRate);
        Assert.Equal(450, river.GetNode(V(1, 2)).Value.FlowRate);
        // Right
        Assert.Equal(490, river.GetNode(V(2, 0)).Value.FlowRate);
        Assert.Equal(470, river.GetNode(V(3, 0)).Value.FlowRate);
        Assert.Equal(470, river.GetNode(V(3, 1)).Value.FlowRate);
        Assert.Equal(450, river.GetNode(V(3, 2)).Value.FlowRate);
        // Combined
        Assert.Equal(880, river.GetNode(V(2, 2)).Value.FlowRate);
    }

}
