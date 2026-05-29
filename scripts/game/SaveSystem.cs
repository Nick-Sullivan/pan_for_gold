using Godot;
using System;
using System.IO;
using System.Text.Json;

// Slot-based persistence over GameState.Save/Load. Owns the save directory and
// the slot file naming; the actual (de)serialization lives in GameState. Kept a
// plain C# class (like Economy/TileEditor) so it is unit/integration testable
// without a Godot node — BaseDir is settable so tests can target a temp dir and
// never touch the player's real saves.
public class SaveSystem
{
    public const int SlotCount = 3;

    // Default to user:// so real saves persist per-user; tests override this.
    public string BaseDir = ProjectSettings.GlobalizePath("user://saves");

    public readonly record struct SlotInfo(
        bool Exists, int Gold, int Clay, int Zone, int Region, DateTime LastWrite);

    public string SlotPath(int slot) => Path.Combine(BaseDir, $"slot_{slot}.json");

    public bool Exists(int slot) => File.Exists(SlotPath(slot));

    public void Delete(int slot)
    {
        if (Exists(slot))
            File.Delete(SlotPath(slot));
    }

    public void Save(int slot)
    {
        Directory.CreateDirectory(BaseDir);
        GameState.Instance.Save(SlotPath(slot));
    }

    public void Load(int slot) => GameState.Instance.Load(SlotPath(slot));

    // Lightweight summary for the title-screen slot rows. Reads the file directly
    // (does not mutate GameState) and tolerates a missing/corrupt slot.
    public SlotInfo ReadInfo(int slot)
    {
        var path = SlotPath(slot);
        if (!File.Exists(path))
            return new SlotInfo(false, 0, 0, 0, 0, default);

        try
        {
            var snap = JsonSerializer.Deserialize<GameState.Snapshot>(File.ReadAllText(path));
            int zone = snap.CurrentZone;
            int region = (snap.Zones != null && zone < snap.Zones.Length)
                ? snap.Zones[zone].CurrentRegion
                : 0;
            return new SlotInfo(true, snap.Gold, snap.Clay, zone, region, File.GetLastWriteTime(path));
        }
        catch
        {
            return new SlotInfo(false, 0, 0, 0, 0, default);
        }
    }
}
