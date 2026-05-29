using System.Collections.Generic;

public class DAGNode<TId, TNodeValue, TEdgeValue>
{
    public TId Id { get; set; }
    public TNodeValue Value { get; set; }
    public List<DAGEdge<TId, TEdgeValue>> ParentEdges { get; set; } = [];
    public List<DAGEdge<TId, TEdgeValue>> ChildEdges { get; set; } = [];

    public DAGNode(TId id, TNodeValue value = default)
    {
        Id = id;
        Value = value;
    }
}
