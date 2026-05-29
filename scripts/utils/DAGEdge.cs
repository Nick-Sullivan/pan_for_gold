public class DAGEdge<TId, TValue>
{
    public TValue Value { get; set; }
    public TId Source { get; set; }
    public TId Destination { get; set; }

    public DAGEdge(TId source, TId destination, TValue value = default)
    {
        Source = source;
        Destination = destination;
        Value = value;
    }
}
