using Godot;

namespace pan_for_gold.Tests;

public class RiverDAGTests
{
    [Fact]
    public void AddNode_CoordIsRetrievable()
    {
        var dag = new RiverDAG();
        dag.AddNode(new Vector2I(1, 2));
        Assert.NotNull(dag.GetNode(new Vector2I(1, 2)));
    }

    [Fact]
    public void AddEdge_LinksCoords()
    {
        var dag = new RiverDAG();
        dag.AddNode(new Vector2I(0, 0));
        dag.AddNode(new Vector2I(1, 0));
        dag.AddEdge(new Vector2I(0, 0), new Vector2I(1, 0));
        Assert.Contains(new Vector2I(1, 0), dag.GetChildren(new Vector2I(0, 0)));
    }

    [Fact]
    public void BreadthFirstOrder_ReturnsCoords()
    {
        var dag = new RiverDAG();
        dag.AddNode(new Vector2I(0, 0));
        dag.AddNode(new Vector2I(1, 0));
        dag.AddNode(new Vector2I(2, 0));
        dag.AddEdge(new Vector2I(0, 0), new Vector2I(1, 0));
        dag.AddEdge(new Vector2I(1, 0), new Vector2I(2, 0));
        Assert.Equal(
            [new Vector2I(0, 0), new Vector2I(1, 0), new Vector2I(2, 0)],
            dag.BreadthFirstOrder(new Vector2I(0, 0))
        );
    }
}
