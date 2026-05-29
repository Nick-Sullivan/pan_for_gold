namespace pan_for_gold.Tests;

public class DAGNodeTests
{
    [Fact]
    public void Constructor_SetsId()
    {
        var node = new DAGNode<string, object, object>("a");
        Assert.Equal("a", node.Id);
    }

    [Fact]
    public void Constructor_DefaultValueIsNull()
    {
        var node = new DAGNode<string, object, object>("a");
        Assert.Null(node.Value);
    }

    [Fact]
    public void Constructor_SetsValue()
    {
        var node = new DAGNode<string, object, object>("a", 42);
        Assert.Equal(42, node.Value);
    }

    [Fact]
    public void Constructor_ParentEdgesIsEmpty()
    {
        var node = new DAGNode<string, object, object>("a");
        Assert.Empty(node.ParentEdges);
    }

    [Fact]
    public void Constructor_ChildEdgesIsEmpty()
    {
        var node = new DAGNode<string, object, object>("a");
        Assert.Empty(node.ChildEdges);
    }

    [Fact]
    public void ParentEdges_CanAddAndRetrieve()
    {
        var node = new DAGNode<string, object, object>("child");
        node.ParentEdges.Add(new DAGEdge<string, object>("parent1", "child"));
        node.ParentEdges.Add(new DAGEdge<string, object>("parent2", "child"));
        Assert.Equal(["parent1", "parent2"], node.ParentEdges.Select(e => e.Source));
    }

    [Fact]
    public void ChildEdges_CanAddAndRetrieve()
    {
        var node = new DAGNode<string, object, object>("parent");
        node.ChildEdges.Add(new DAGEdge<string, object>("parent", "child1"));
        node.ChildEdges.Add(new DAGEdge<string, object>("parent", "child2"));
        Assert.Equal(["child1", "child2"], node.ChildEdges.Select(e => e.Destination));
    }

    [Fact]
    public void Value_CanBeReassigned()
    {
        var node = new DAGNode<string, object, object>("a", "initial");
        node.Value = "updated";
        Assert.Equal("updated", node.Value);
    }
}
