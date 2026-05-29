using Godot;

// A fixture describes how to reach a particular game state using ONLY player
// actions (the "full setup"). Running Build from a fresh game is authoritative
// and survives changes to the save format. A snapshot serialized from that run
// is the "quick setup" — see TestContext.LoadFixture and IntegrationTestRunner's
// --regen-fixtures path.
public interface IFixture
{
    string Name { get; }
    void Build(PlayerActions actions);
}

public static class FixturePaths
{
    public static string SnapshotFor(string name)
        => ProjectSettings.GlobalizePath($"res://tests/integration/fixtures/snapshots/{name}.json");
}
