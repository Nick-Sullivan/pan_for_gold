using Godot;

public class RiverDAG : DAG<Vector2I, RiverNode, RiverEdge>
{
    public new RiverDAG Clone() => (RiverDAG)base.Clone();
}
