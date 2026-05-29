using System;
using System.Collections.Generic;

public enum SetupMode { Full, Quick }

// Handed to each integration test. Exposes the player-action facade, the runner
// for deterministic stepping, fixture loading, and assertion helpers. Failures
// are accumulated (not thrown) so a test reports every problem it finds.
public class TestContext
{
    public PlayerActions Actions { get; }
    public GameRunner Runner { get; }
    public List<string> Failures { get; } = [];

    public TestContext(PlayerActions actions, GameRunner runner)
    {
        Actions = actions;
        Runner = runner;
    }

    // Recreate a fixture's state. Full = replay player actions (authoritative);
    // Quick = load the snapshot generated from that replay.
    public void LoadFixture(IFixture fixture, SetupMode mode)
    {
        Runner.StartNewGame();
        if (mode == SetupMode.Full)
        {
            fixture.Build(Actions);
        }
        else
        {
            GameState.Instance.Load(FixturePaths.SnapshotFor(fixture.Name));
            Runner.StepPropagation();
        }
    }

    public void AssertTrue(bool condition, string message)
    {
        if (!condition)
            Failures.Add(message);
    }

    public void AssertEqual<T>(T expected, T actual, string what)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            Failures.Add($"{what}: expected {expected}, got {actual}");
    }

    public void AssertFloat(float expected, float actual, string what, float eps = 0.0001f)
    {
        if (Math.Abs(expected - actual) > eps)
            Failures.Add($"{what}: expected {expected}, got {actual} (eps {eps})");
    }
}
