using Godot;
using System.Linq;

// The opening screen: New Game / Continue across three save slots. Boots first
// (project.godot main_scene), then hands off to the game scene via SceneManager.
// UI is built in code, matching HUD.cs conventions (no layout .tscn).
[GlobalClass]
public partial class TitleScreen : Control
{
    private const string GameScene = "res://scenes/main/main.tscn";

    private static readonly Color Bg = new(0.10f, 0.10f, 0.12f, 1.0f);
    private static readonly Color Gold = new(0.85f, 0.70f, 0.20f);
    private static readonly Color Hint = new(0.5f, 0.5f, 0.5f);

    private VBoxContainer _slotsBox;

    public override void _Ready()
    {
        // Test/screenshot harnesses boot the project main_scene (now the title).
        // Skip the menu and hand straight off to the game scene, which keeps the
        // existing --run-tests / --screenshot handling in main.gd.
        var args = OS.GetCmdlineUserArgs();
        if (args.Contains("--run-tests") || args.Contains("--screenshot"))
        {
            SceneManager.Instance.ChangeScene(GameScene);
            return;
        }

        BuildUi();
    }

    private void BuildUi()
    {
        var bg = new ColorRect { Color = Bg };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var center = new CenterContainer();
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var vbox = new VBoxContainer();
        vbox.CustomMinimumSize = new Vector2(420, 0);
        vbox.AddThemeConstantOverride("separation", 14);
        center.AddChild(vbox);

        var title = new Label();
        title.Text = "Pan for Gold";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeFontSizeOverride("font_size", 48);
        title.AddThemeColorOverride("font_color", Gold);
        vbox.AddChild(title);

        vbox.AddChild(new HSeparator());

        _slotsBox = new VBoxContainer();
        _slotsBox.AddThemeConstantOverride("separation", 10);
        vbox.AddChild(_slotsBox);

        RefreshSlots();
    }

    private void RefreshSlots()
    {
        foreach (var child in _slotsBox.GetChildren())
            child.QueueFree();

        for (int i = 0; i < SaveSystem.SlotCount; i++)
            _slotsBox.AddChild(BuildSlotRow(i));
    }

    private Control BuildSlotRow(int slot)
    {
        var info = GameRunner.Instance.Save.ReadInfo(slot);

        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.14f, 0.14f, 0.17f, 1.0f);
        style.SetCornerRadiusAll(6);
        style.SetContentMarginAll(10);

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", style);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        panel.AddChild(row);

        var label = new Label();
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        label.VerticalAlignment = VerticalAlignment.Center;
        if (info.Exists)
        {
            label.Text = $"Slot {slot + 1}\nGold: {info.Gold}  ·  Region {info.Region + 1}  ·  {info.LastWrite:yyyy-MM-dd HH:mm}";
        }
        else
        {
            label.Text = $"Slot {slot + 1}\nEmpty";
            label.AddThemeColorOverride("font_color", Hint);
        }
        row.AddChild(label);

        if (info.Exists)
        {
            var continueBtn = new Button { Text = "Continue" };
            continueBtn.CustomMinimumSize = new Vector2(90, 44);
            continueBtn.Pressed += () => LoadAndPlay(slot);
            row.AddChild(continueBtn);

            var newBtn = new Button { Text = "New" };
            newBtn.CustomMinimumSize = new Vector2(60, 44);
            newBtn.Pressed += () => NewAndPlay(slot);
            row.AddChild(newBtn);

            var deleteBtn = new Button { Text = "Delete" };
            deleteBtn.CustomMinimumSize = new Vector2(70, 44);
            deleteBtn.Pressed += () => { GameRunner.Instance.Save.Delete(slot); RefreshSlots(); };
            row.AddChild(deleteBtn);
        }
        else
        {
            var newBtn = new Button { Text = "New Game" };
            newBtn.CustomMinimumSize = new Vector2(120, 44);
            newBtn.Pressed += () => NewAndPlay(slot);
            row.AddChild(newBtn);
        }

        return panel;
    }

    private void NewAndPlay(int slot)
    {
        GameRunner.Instance.NewGameInSlot(slot);
        SceneManager.Instance.ChangeScene(GameScene);
    }

    private void LoadAndPlay(int slot)
    {
        GameRunner.Instance.LoadSlot(slot);
        SceneManager.Instance.ChangeScene(GameScene);
    }
}
