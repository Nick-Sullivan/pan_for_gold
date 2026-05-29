namespace pan_for_gold.Tests;

public class DAGTests
{
    [Fact]
    public void AddNode_NodeIsRetrievable()
    {
        var dag = new DAG<string, object, object>();
        dag.AddNode("a");
        Assert.NotNull(dag.GetNode("a"));
    }

    [Fact]
    public void AddNode_StoresValue()
    {
        var dag = new DAG<string, object, object>();
        dag.AddNode("a", 99);
        Assert.Equal(99, dag.GetNode("a").Value);
    }

    [Fact]
    public void AddNode_DuplicateThrows()
    {
        var dag = new DAG<string, object, object>();
        dag.AddNode("a", 1);
        Assert.Throws<ArgumentException>(() => dag.AddNode("a", 2));
    }

    [Fact]
    public void GetNode_MissingThrows()
    {
        var dag = new DAG<string, object, object>();
        Assert.Throws<KeyNotFoundException>(() => dag.GetNode("x"));
    }

    [Fact]
    public void AddEdge_LinksParentToChild()
    {
        var dag = new DAG<string, object, object>();
        dag.AddNode("a");
        dag.AddNode("b");
        dag.AddEdge("a", "b");
        Assert.Contains("b", dag.GetChildren("a"));
    }

    [Fact]
    public void AddEdge_LinksChildToParent()
    {
        var dag = new DAG<string, object, object>();
        dag.AddNode("a");
        dag.AddNode("b");
        dag.AddEdge("a", "b");
        Assert.Contains("a", dag.GetParents("b"));
    }

    [Fact]
    public void AddEdge_MissingParentThrows()
    {
        var dag = new DAG<string, object, object>();
        dag.AddNode("b");
        Assert.Throws<KeyNotFoundException>(() => dag.AddEdge("missing", "b"));
    }

    [Fact]
    public void AddEdge_MissingChildThrows()
    {
        var dag = new DAG<string, object, object>();
        dag.AddNode("a");
        Assert.Throws<KeyNotFoundException>(() => dag.AddEdge("a", "missing"));
    }

    [Fact]
    public void AddEdge_DuplicateThrows()
    {
        var dag = new DAG<string, object, object>();
        dag.AddNode("a");
        dag.AddNode("b");
        dag.AddEdge("a", "b");
        Assert.Throws<ArgumentException>(() => dag.AddEdge("a", "b"));
    }

    [Fact]
    public void SetNode_CreatesNodeIfMissing()
    {
        var dag = new DAG<string, object, object>();
        dag.SetNode("a", 5);
        Assert.Equal(5, dag.GetNode("a").Value);
    }

    [Fact]
    public void SetNode_UpdatesValueIfExists()
    {
        var dag = new DAG<string, object, object>();
        dag.AddNode("a", 1);
        dag.SetNode("a", 2);
        Assert.Equal(2, dag.GetNode("a").Value);
    }

    [Fact]
    public void SetEdge_CreatesNodesIfMissing()
    {
        var dag = new DAG<string, object, object>();
        dag.SetEdge("a", "b");
        Assert.NotNull(dag.GetNode("a"));
        Assert.NotNull(dag.GetNode("b"));
    }

    [Fact]
    public void SetEdge_IsIdempotent()
    {
        var dag = new DAG<string, object, object>();
        dag.SetEdge("a", "b");
        dag.SetEdge("a", "b");
        Assert.Single(dag.GetChildren("a"));
        Assert.Single(dag.GetParents("b"));
    }

    [Fact]
    public void BreadthFirstOrder_SingleNode()
    {
        var dag = new DAG<string, object, object>();
        dag.AddNode("a");
        Assert.Equal(["a"], dag.BreadthFirstOrder("a"));
    }

    [Fact]
    public void BreadthFirstOrder_LinearChain()
    {
        var dag = new DAG<string, object, object>();
        dag.AddNode("a");
        dag.AddNode("b");
        dag.AddNode("c");
        dag.AddEdge("a", "b");
        dag.AddEdge("b", "c");
        Assert.Equal(["a", "b", "c"], dag.BreadthFirstOrder("a"));
    }

    [Fact]
    public void BreadthFirstOrder_BranchesBeforeDepth()
    {
        var dag = new DAG<string, object, object>();
        dag.AddNode("a");
        dag.AddNode("b");
        dag.AddNode("c");
        dag.AddNode("d");
        dag.AddEdge("a", "b");
        dag.AddEdge("a", "c");
        dag.AddEdge("b", "d");
        var result = dag.BreadthFirstOrder("a");
        Assert.Equal("a", result[0]);
        Assert.Contains("b", result[1..3]);
        Assert.Contains("c", result[1..3]);
        Assert.Equal("d", result[3]);
    }

    [Fact]
    public void BreadthFirstOrder_MissingStartReturnsEmpty()
    {
        var dag = new DAG<string, object, object>();
        Assert.Empty(dag.BreadthFirstOrder("x"));
    }

    [Fact]
    public void BreadthFirstOrder_DoesNotRepeatNodes()
    {
        var dag = new DAG<string, object, object>();
        dag.AddNode("a");
        dag.AddNode("b");
        dag.AddNode("c");
        dag.AddEdge("a", "b");
        dag.AddEdge("a", "c");
        dag.AddEdge("b", "c");
        var result = dag.BreadthFirstOrder("a");
        Assert.Equal(result.Count, result.Distinct().Count());
    }

    [Fact]
    public void Clone_IsDeepCopy()
    {
        var dag = new DAG<string, object, object>();
        dag.AddNode("a", "val");
        dag.AddNode("b");
        dag.AddEdge("a", "b");

        var copy = dag.Clone();

        Assert.Equal("val", copy.GetNode("a").Value);
        Assert.Contains("b", copy.GetChildren("a"));
        Assert.Contains("a", copy.GetParents("b"));

        copy.GetNode("a").ChildEdges.Clear();
        Assert.Contains("b", dag.GetChildren("a"));
    }
}
