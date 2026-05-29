public class RiverNode
{
    public GameState.TileType Type { get; set; }
    public float FlowRate { get; set; }

    public RiverNode(GameState.TileType type, float flowRate)
    {
        Type = type;
        FlowRate = flowRate;
    }

    public override string ToString() => $"{Type}({FlowRate})";

    public override bool Equals(object obj) =>
        obj is RiverNode other && Type == other.Type && FlowRate == other.FlowRate;

    public override int GetHashCode() => (Type, FlowRate).GetHashCode();
}
