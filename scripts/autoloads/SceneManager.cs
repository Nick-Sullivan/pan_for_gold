using Godot;

public partial class SceneManager : Node
{
    // Singleton autoload for scene management
    public override void _Ready()
    {
        GD.Print("SceneManager initialized");
    }

    public void ChangeScene(string path)
    {
        GetTree().ChangeSceneToFile(path);
    }
}