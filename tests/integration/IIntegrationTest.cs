// An integration test boots the real game (autoloads + scene tree) and asserts
// on real GameState. Register implementations in IntegrationTestRunner.Tests.
public interface IIntegrationTest
{
    string Name { get; }
    void Run(TestContext ctx);
}
