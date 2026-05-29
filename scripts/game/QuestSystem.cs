public class QuestSystem
{
    public void Connect()
    {
        var gs = GameState.Instance;
        gs.ShovelsChanged += n => { if (n > 0) Complete(0); };
        gs.TileChanged += (col, row) =>
        {
            if (col != GameState.Cols - 1) return;
            var t = GameState.Instance.Tiles[row, col];
            if (t == GameState.TileType.River || t == GameState.TileType.RiverSource)
                Complete(1);
        };
    }

    private static void Complete(int index)
    {
        var gs = GameState.Instance;
        if (gs.QuestsComplete[index]) return;
        gs.QuestsComplete[index] = true;
        gs.EmitSignal(GameState.SignalName.QuestChanged, index);
    }
}
