using Godot;

public partial class SceneManager : Node
{
    public static SceneManager Instance { get; private set; }

    // Singleton autoload for scene management
    public override void _Ready()
    {
        Instance = this;
        GD.Print("SceneManager initialized");
    }

    public void ChangeScene(string path)
    {
        // Deferred: callers may invoke this from _Ready or a signal handler while
        // the tree is busy adding/removing nodes, where an immediate scene swap
        // would fail ("Parent node is busy adding/removing children").
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);
    }
}