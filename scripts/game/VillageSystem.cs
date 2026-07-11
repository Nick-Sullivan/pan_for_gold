// Detects the first time the player reaches any village's region and marks that
// village discovered. The HUD reacts by showing the village character's dialogue
// (and, for the first village, revealing the Highlands toggle). Villages and their
// regions are defined in VillageDefs. Mirrors QuestSystem/GateSystem.
public class VillageSystem
{
    public void Connect()
    {
        GameState.Instance.RegionSwitched += OnRegionSwitched;
    }

    private void OnRegionSwitched(int index)
    {
        var gs = GameState.Instance;
        var village = VillageDefs.ForRegion(gs.CurrentZone, index);
        if (village == null) return;

        int id = VillageDefs.IndexOf(village);
        if (gs.VillagesDiscovered[id]) return;

        gs.VillagesDiscovered[id] = true;

        // Meeting the first village unlocks the furnace tool (the elder explains it).
        if (id == 0 && !gs.HasFurnace)
        {
            gs.HasFurnace = true;
            gs.EmitSignal(GameState.SignalName.FurnaceChanged, true);
        }

        gs.EmitSignal(GameState.SignalName.VillageFound, id);
    }
}
