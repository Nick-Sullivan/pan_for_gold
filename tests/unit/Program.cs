using System.Reflection;
using Xunit.Runners;

var assemblyPath = Assembly.GetExecutingAssembly().Location;
var finished = new ManualResetEventSlim();
var failed = 0;
var total = 0;

using var runner = AssemblyRunner.WithoutAppDomain(assemblyPath);

runner.OnDiscoveryComplete = info =>
    Console.WriteLine($"Running {info.TestCasesToRun} tests...\n");

runner.OnTestFailed = info =>
{
    Interlocked.Increment(ref failed);
    Interlocked.Increment(ref total);
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"[FAIL] {info.TestDisplayName}");
    Console.WriteLine($"       {info.ExceptionMessage}");
    Console.ResetColor();
};

runner.OnTestPassed = info =>
{
    Interlocked.Increment(ref total);
    Console.WriteLine($"[pass] {info.TestDisplayName}");
};

runner.OnExecutionComplete = info =>
{
    Console.WriteLine($"\n{total} tests: {total - failed} passed, {failed} failed");
    finished.Set();
};

runner.Start();
finished.Wait();
return failed > 0 ? 1 : 0;
