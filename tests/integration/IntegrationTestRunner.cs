using Godot;
using System;
using System.Collections.Generic;

// In-engine integration test harness. Boots inside Godot headless via the
// --run-tests cmdline flag (see Main), drives the real game through PlayerActions
// and asserts on GameState. Reports in the style of tests/Program.cs and sets the
// process exit code so `mise run itest` is CI-friendly.
public static class IntegrationTestRunner
{
    // Explicit registries (not reflection) — deterministic order and no Mono
    // headless trimming surprises. Add a feature => add its test here.
    public static readonly List<IIntegrationTest> Tests =
    [
        new BootInitialStateTest(),
        new DigCycleTest(),
        new DigRemovesAutopanTest(),
        new FlowOutputUnlockTest(),
        new AutopanGoldTest(),
        new ClayAndBrickTest(),
        new BrickUpkeepTest(),
        new VillageSupplyTest(),
        new VillageDiscoveryTest(),
        new SaveLoadRoundTripTest(),
        new SaveSlotTest(),
    ];

    public static readonly List<IFixture> Fixtures =
    [
        // Add fixtures here; `mise run itest-regen` writes their snapshots.
    ];

    // Hard cap so a headless run can never spin forever, regardless of engine state.
    // Fully qualified to avoid clashing with Godot.Timer.
    private static System.Threading.Timer _watchdog;

    // Godot's piped stdout is unreliably buffered on a fast headless exit, so we
    // also mirror the report to a file (flushed on close) for reliable inspection.
    private static readonly System.Text.StringBuilder _log = new();
    private static string LogPath => ProjectSettings.GlobalizePath("res://tests/integration/last-run.log");

    private static void Log(string s) { _log.AppendLine(s); GD.Print(s); }
    private static void LogErr(string s) { _log.AppendLine(s); GD.PrintErr(s); }

    public static void Run(SceneTree tree, bool regen)
    {
        // Watchdog: if we're not done in 30s, kill the OS process outright.
        _watchdog = new System.Threading.Timer(_ =>
        {
            Console.Error.WriteLine("[harness] WATCHDOG: exceeded 30s, force-exiting");
            Console.Out.Flush();
            Console.Error.Flush();
            System.Environment.Exit(124);
        }, null, 30_000, System.Threading.Timeout.Infinite);

        int exitCode = 1;
        try
        {
            var runner = tree.Root.GetNodeOrNull<GameRunner>("GameRunner");
            if (runner == null)
            {
                LogErr("[harness] GameRunner autoload not found at /root/GameRunner");
            }
            else
            {
                runner.TestMode = true;
                var actions = new PlayerActions(tree, runner);
                exitCode = regen ? Regen(runner, actions) : RunTests(runner, actions);
            }
        }
        catch (Exception e)
        {
            LogErr($"[harness] aborted: {e}");
        }
        finally
        {
            Log($"[harness] exit {exitCode}");
            try { System.IO.File.WriteAllText(LogPath, _log.ToString()); } catch { }
            // Graceful shutdown: Godot flushes its output on Quit (Environment.Exit
            // would race the flush and lose the report). The watchdog above is the
            // anti-hang failsafe if this somehow doesn't terminate.
            tree.Quit(exitCode);
        }
    }

    private static int Regen(GameRunner runner, PlayerActions actions)
    {
        var dir = ProjectSettings.GlobalizePath("res://tests/integration/fixtures/snapshots/");
        System.IO.Directory.CreateDirectory(dir);

        Log($"Regenerating {Fixtures.Count} fixture snapshot(s)...");
        foreach (var fixture in Fixtures)
        {
            runner.StartNewGame();
            fixture.Build(actions);
            GameState.Instance.Save(FixturePaths.SnapshotFor(fixture.Name));
            Log($"[saved] {fixture.Name}");
        }
        Log("Done.");
        return 0;
    }

    private static int RunTests(GameRunner runner, PlayerActions actions)
    {
        Log($"Running {Tests.Count} integration test(s)...");
        int failed = 0;

        foreach (var test in Tests)
        {
            runner.StartNewGame();
            var ctx = new TestContext(actions, runner);

            try
            {
                test.Run(ctx);
            }
            catch (Exception e)
            {
                ctx.Failures.Add($"threw {e.GetType().Name}: {e.Message}");
            }

            if (ctx.Failures.Count == 0)
            {
                Log($"[pass] {test.Name}");
            }
            else
            {
                failed++;
                LogErr($"[FAIL] {test.Name}");
                foreach (var f in ctx.Failures)
                    LogErr($"       {f}");
            }
        }

        Log($"{Tests.Count} tests: {Tests.Count - failed} passed, {failed} failed");
        return failed > 0 ? 1 : 0;
    }
}
