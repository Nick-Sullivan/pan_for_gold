using System;
using System.Collections.Generic;
using System.Linq;

public class DAG<TId, TNodeValue, TEdgeValue> where TId : notnull
{
    private readonly Dictionary<TId, DAGNode<TId, TNodeValue, TEdgeValue>> nodes = [];
    private readonly Dictionary<(TId, TId), DAGEdge<TId, TEdgeValue>> edges = [];

    public void AddNode(TId id, TNodeValue value = default)
    {
        if (nodes.ContainsKey(id))
            throw new ArgumentException($"Node '{id}' already exists");
        nodes[id] = new DAGNode<TId, TNodeValue, TEdgeValue>(id, value);
    }

    public void SetNode(TId id, TNodeValue value = default)
    {
        if (nodes.ContainsKey(id))
            nodes[id].Value = value;
        else
            nodes[id] = new DAGNode<TId, TNodeValue, TEdgeValue>(id, value);
    }

    public void RemoveNode(TId id)
    {
        if (!nodes.TryGetValue(id, out var node))
            throw new KeyNotFoundException($"Node '{id}' does not exist");

        foreach (var edge in node.ParentEdges.Concat(node.ChildEdges).ToList())
        {
            var key = (edge.Source, edge.Destination);
            edges.Remove(key);
            if (nodes.TryGetValue(edge.Source, out var parent))
                parent.ChildEdges.Remove(edge);
            if (nodes.TryGetValue(edge.Destination, out var child))
                child.ParentEdges.Remove(edge);
        }

        nodes.Remove(id);
    }

    public int NumNodes => nodes.Count;
    public List<TId> NodeIds => nodes.Keys.ToList();

    public bool ContainsNode(TId id) => nodes.ContainsKey(id);

    public bool ContainsEdge(TId parentId, TId childId) => edges.ContainsKey((parentId, childId));

    public DAGNode<TId, TNodeValue, TEdgeValue> GetNode(TId id)
    {
        if (!nodes.TryGetValue(id, out var node))
            throw new KeyNotFoundException($"Node '{id}' does not exist");
        return node;
    }

    public void AddEdge(TId parentId, TId childId, TEdgeValue value = default)
    {
        if (!nodes.TryGetValue(parentId, out var parent))
            throw new KeyNotFoundException($"Node '{parentId}' does not exist");
        if (!nodes.TryGetValue(childId, out var child))
            throw new KeyNotFoundException($"Node '{childId}' does not exist");

        var key = (parentId, childId);
        if (edges.ContainsKey(key))
            throw new ArgumentException($"Edge '{parentId}' -> '{childId}' already exists");

        var edge = new DAGEdge<TId, TEdgeValue>(parentId, childId, value);
        edges[key] = edge;
        parent.ChildEdges.Add(edge);
        child.ParentEdges.Add(edge);
    }

    public void SetEdge(TId parentId, TId childId, TEdgeValue value = default)
    {
        if (!nodes.ContainsKey(parentId))
            nodes[parentId] = new DAGNode<TId, TNodeValue, TEdgeValue>(parentId);
        if (!nodes.ContainsKey(childId))
            nodes[childId] = new DAGNode<TId, TNodeValue, TEdgeValue>(childId);

        var key = (parentId, childId);
        var parent = nodes[parentId];
        var child = nodes[childId];

        if (edges.TryGetValue(key, out var existing))
        {
            existing.Value = value;
        }
        else
        {
            var edge = new DAGEdge<TId, TEdgeValue>(parentId, childId, value);
            edges[key] = edge;
            parent.ChildEdges.Add(edge);
            child.ParentEdges.Add(edge);
        }
    }

    public void RemoveEdge(TId parentId, TId childId)
    {
        if (!nodes.TryGetValue(parentId, out var parent))
            throw new KeyNotFoundException($"Node '{parentId}' does not exist");
        if (!nodes.TryGetValue(childId, out var child))
            throw new KeyNotFoundException($"Node '{childId}' does not exist");

        var key = (parentId, childId);
        if (!edges.TryGetValue(key, out var edge))
            throw new KeyNotFoundException($"Edge '{parentId}' -> '{childId}' does not exist");

        edges.Remove(key);
        parent.ChildEdges.Remove(edge);
        child.ParentEdges.Remove(edge);
    }

    public DAGEdge<TId, TEdgeValue> GetEdge(TId parentId, TId childId)
    {
        if (!edges.TryGetValue((parentId, childId), out var edge))
            throw new KeyNotFoundException($"Edge '{parentId}' -> '{childId}' does not exist");
        return edge;
    }

    public List<TId> GetParents(TId id) =>
        GetNode(id).ParentEdges.Select(e => e.Source).ToList();

    public List<TId> GetChildren(TId id) =>
        GetNode(id).ChildEdges.Select(e => e.Destination).ToList();

    public List<DAGEdge<TId, TEdgeValue>> GetParentEdges(TId id) =>
        GetNode(id).ParentEdges;

    public List<DAGEdge<TId, TEdgeValue>> GetChildEdges(TId id) =>
        GetNode(id).ChildEdges;

    public List<TId> BreadthFirstOrder(TId startId)
    {
        var result = new List<TId>();
        if (!nodes.ContainsKey(startId))
            return result;

        var queue = new Queue<TId>();
        queue.Enqueue(startId);
        var visited = new HashSet<TId>();

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            if (!visited.Add(currentId))
                continue;

            result.Add(currentId);

            foreach (var childId in GetChildren(currentId))
                if (!visited.Contains(childId))
                    queue.Enqueue(childId);
        }

        return result;
    }

    public override bool Equals(object obj)
    {
        if (obj is not DAG<TId, TNodeValue, TEdgeValue> other) return false;
        if (NumNodes != other.NumNodes) return false;
        foreach (var id in NodeIds)
        {
            if (!other.ContainsNode(id)) return false;
            if (!Equals(GetNode(id).Value, other.GetNode(id).Value)) return false;
            var myChildren = GetChildEdges(id);
            var otherChildren = other.GetChildEdges(id);
            if (myChildren.Count != otherChildren.Count) return false;
            foreach (var edge in myChildren)
            {
                if (!other.ContainsEdge(edge.Source, edge.Destination)) return false;
                if (!Equals(edge.Value, other.GetEdge(edge.Source, edge.Destination).Value)) return false;
            }
        }
        return true;
    }

    public override int GetHashCode() => NumNodes;

    public override string ToString()
    {
        var lines = new System.Text.StringBuilder();
        foreach (var id in NodeIds.OrderBy(id => id.ToString()))
        {
            var node = GetNode(id);
            lines.Append($"  {id}: {node.Value}");
            foreach (var edge in GetChildEdges(id))
            {
                lines.Append($" -> {edge.Destination}({edge.Value})");
            }
            lines.AppendLine();
        }
        return $"{GetType().Name} {{\n{lines}}}";
    }

    public DAG<TId, TNodeValue, TEdgeValue> Clone()
    {
        var copy = (DAG<TId, TNodeValue, TEdgeValue>)Activator.CreateInstance(GetType());

        foreach (var node in nodes.Values)
            copy.nodes[node.Id] = new DAGNode<TId, TNodeValue, TEdgeValue>(node.Id, node.Value);

        foreach (var (key, edge) in edges)
        {
            var clonedEdge = new DAGEdge<TId, TEdgeValue>(edge.Source, edge.Destination, edge.Value);
            copy.edges[key] = clonedEdge;
            copy.nodes[edge.Source].ChildEdges.Add(clonedEdge);
            copy.nodes[edge.Destination].ParentEdges.Add(clonedEdge);
        }

        return copy;
    }
}
