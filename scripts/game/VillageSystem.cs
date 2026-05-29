// Detects the first time the player reaches the village (zone 0, region 1) and
// marks it discovered. The HUD reacts by showing the village character's dialogue
// and revealing the Highlands toggle. Mirrors QuestSystem/GateSystem.
public class VillageSystem
{
    public void Connect()
    {
        GameState.Instance.RegionSwitched += OnRegionSwitched;
    }

    private void OnRegionSwitched(int index)
    {
        var gs = GameState.Instance;
        if (gs.VillageDiscovered) return;
        if (gs.CurrentZone != 0 || index != 1) return;

        gs.VillageDiscovered = true;
        gs.EmitSignal(GameState.SignalName.VillageFound);
    }
}
