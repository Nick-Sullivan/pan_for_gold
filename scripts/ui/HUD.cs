using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class HUD : CanvasLayer
{
    [Signal] public delegate void BuyShovelRequestedEventHandler();
    [Signal] public delegate void ToolSelectedEventHandler(int tool);
    [Signal] public delegate void RegionSelectedEventHandler(int index);
    [Signal] public delegate void ZoneSwitchRequestedEventHandler(int zone);
    [Signal] public delegate void SaveRequestedEventHandler();

    private const int Mhw = 22;
    private const int Mhh = 11;

    private Label _goldLabel;
    private Label _clayLabel;
    private Button _buyButton;
    private Label _shopEmptyLabel;
    private Button _toolPanBtn;
    private Button _toolShovelBtn;
    private Control _mapContainer;
    private HBoxContainer _zoneToggle;
    private Button _lowlandsBtn;
    private Button _highlandsBtn;

    private readonly List<Polygon2D> _regionDiamonds = [];
    private readonly List<Control> _tabContents = [];
    private readonly List<Button> _tabButtons = [];
    private readonly Label[] _questIndicators = new Label[2];
    private readonly HBoxContainer[] _questRows = new HBoxContainer[2];

    private Control _villageSign;
    private Label _villageFlowLabel;
    private Label _villageGateLabel;

    public override void _Ready()
    {
        AddToGroup("hud");
        BuildUi();

        var gs = GameState.Instance;
        gs.GoldChanged += OnGoldChanged;
        gs.ClayChanged += v => _clayLabel.Text = $"Clay: {v}";
        gs.ShovelsChanged += OnShovelsChanged;
        gs.ToolChanged += OnToolChanged;
        gs.RegionUnlocked += OnRegionUnlocked;
        gs.RegionSwitched += OnRegionSwitched;
        gs.ZoneChanged += OnZoneChanged;
        gs.QuestChanged += OnQuestChanged;
        gs.FlowChanged += OnFlowChangedSign;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            switch (key.Keycode)
            {
                case Key.Key1: SwitchTab(0); break;
                case Key.Key2: SwitchTab(1); break;
                case Key.Key3: SwitchTab(2); break;
                case Key.Key4: SwitchTab(3); break;
            }
        }
    }

    private void SwitchTab(int index)
    {
        for (int i = 0; i < _tabContents.Count; i++)
        {
            _tabContents[i].Visible = i == index;
            _tabButtons[i].ButtonPressed = i == index;
        }
    }

    private void BuildUi()
    {
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.10f, 0.10f, 0.12f, 1.0f);
        style.SetCornerRadiusAll(6);

        var panel = new PanelContainer();
        panel.Position = new Vector2(950, 10);
        panel.AddThemeStyleboxOverride("panel", style);
        AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.CustomMinimumSize = new Vector2(290, 0);
        vbox.AddThemeConstantOverride("separation", 6);
        margin.AddChild(vbox);

        // Gold label — always visible
        _goldLabel = new Label();
        _goldLabel.Text = "Gold: 0";
        _goldLabel.AddThemeFontSizeOverride("font_size", 24);
        vbox.AddChild(_goldLabel);

        _clayLabel = new Label();
        _clayLabel.Text = "Clay: 0";
        _clayLabel.AddThemeFontSizeOverride("font_size", 24);
        vbox.AddChild(_clayLabel);

        vbox.AddChild(new HSeparator());

        // Tab bar
        var tabHbox = new HBoxContainer();
        tabHbox.AddThemeConstantOverride("separation", 4);
        vbox.AddChild(tabHbox);

        var tabGroup = new ButtonGroup();
        string[] tabLabels = ["Equip", "Shop", "Map", "Quests"];
        for (int i = 0; i < 4; i++)
        {
            var btn = new Button();
            btn.Text = tabLabels[i];
            btn.ToggleMode = true;
            btn.ButtonGroup = tabGroup;
            btn.ButtonPressed = i == 0;
            btn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            int idx = i;
            btn.Pressed += () => SwitchTab(idx);
            tabHbox.AddChild(btn);
            _tabButtons.Add(btn);
        }

        // Tab 1 — Equipment
        var equipBox = new VBoxContainer();
        equipBox.AddThemeConstantOverride("separation", 6);
        vbox.AddChild(equipBox);
        _tabContents.Add(equipBox);

        var toolHbox = new HBoxContainer();
        toolHbox.AddThemeConstantOverride("separation", 6);
        equipBox.AddChild(toolHbox);

        var toolGroup = new ButtonGroup();

        _toolPanBtn = new Button();
        _toolPanBtn.Text = "Pan";
        _toolPanBtn.CustomMinimumSize = new Vector2(80, 48);
        _toolPanBtn.ToggleMode = true;
        _toolPanBtn.ButtonPressed = true;
        _toolPanBtn.ButtonGroup = toolGroup;
        _toolPanBtn.Pressed += () => EmitSignal(SignalName.ToolSelected, (int)GameState.ActiveTool.Pan);
        toolHbox.AddChild(_toolPanBtn);

        _toolShovelBtn = new Button();
        _toolShovelBtn.Text = "Shovel";
        _toolShovelBtn.CustomMinimumSize = new Vector2(80, 48);
        _toolShovelBtn.ToggleMode = true;
        _toolShovelBtn.ButtonGroup = toolGroup;
        _toolShovelBtn.Disabled = true;
        _toolShovelBtn.Visible = false;
        _toolShovelBtn.Pressed += () => EmitSignal(SignalName.ToolSelected, (int)GameState.ActiveTool.Shovel);
        toolHbox.AddChild(_toolShovelBtn);

        equipBox.AddChild(new HSeparator());

        var saveBtn = new Button();
        saveBtn.Text = "Save";
        saveBtn.CustomMinimumSize = new Vector2(0, 40);
        saveBtn.Pressed += () => EmitSignal(SignalName.SaveRequested);
        equipBox.AddChild(saveBtn);

        // Tab 2 — Shop
        var shopBox = new VBoxContainer();
        shopBox.AddThemeConstantOverride("separation", 6);
        shopBox.Visible = false;
        vbox.AddChild(shopBox);
        _tabContents.Add(shopBox);

        _buyButton = new Button();
        _buyButton.Text = $"Buy Shovel ({GameState.ShovelCost}g)";
        _buyButton.CustomMinimumSize = new Vector2(0, 44);
        _buyButton.Disabled = true;
        _buyButton.Pressed += () => EmitSignal(SignalName.BuyShovelRequested);
        shopBox.AddChild(_buyButton);

        _shopEmptyLabel = new Label();
        _shopEmptyLabel.Text = "No items available";
        _shopEmptyLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
        _shopEmptyLabel.Visible = false;
        shopBox.AddChild(_shopEmptyLabel);

        // Tab 3 — Map
        var mapBox = new VBoxContainer();
        mapBox.AddThemeConstantOverride("separation", 6);
        mapBox.Visible = false;
        vbox.AddChild(mapBox);
        _tabContents.Add(mapBox);

        _zoneToggle = new HBoxContainer();
        _zoneToggle.AddThemeConstantOverride("separation", 4);
        _zoneToggle.Visible = false;
        mapBox.AddChild(_zoneToggle);

        var zoneGroup = new ButtonGroup();

        _lowlandsBtn = new Button();
        _lowlandsBtn.Text = "Lowlands";
        _lowlandsBtn.ToggleMode = true;
        _lowlandsBtn.ButtonPressed = true;
        _lowlandsBtn.ButtonGroup = zoneGroup;
        _lowlandsBtn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _lowlandsBtn.Pressed += () => EmitSignal(SignalName.ZoneSwitchRequested, 0);
        _zoneToggle.AddChild(_lowlandsBtn);

        _highlandsBtn = new Button();
        _highlandsBtn.Text = "Highlands";
        _highlandsBtn.ToggleMode = true;
        _highlandsBtn.ButtonGroup = zoneGroup;
        _highlandsBtn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _highlandsBtn.Pressed += () => EmitSignal(SignalName.ZoneSwitchRequested, 1);
        _zoneToggle.AddChild(_highlandsBtn);

        _mapContainer = new Control();
        _mapContainer.CustomMinimumSize = new Vector2(290, Mhh * 2 + 4);
        _mapContainer.MouseFilter = Control.MouseFilterEnum.Stop;
        _mapContainer.GuiInput += OnMapInput;
        mapBox.AddChild(_mapContainer);

        AddRegionDiamond(0);

        // Tab 4 — Quests
        var questBox = new VBoxContainer();
        questBox.AddThemeConstantOverride("separation", 10);
        questBox.Visible = false;
        vbox.AddChild(questBox);
        _tabContents.Add(questBox);

        (string title, string desc)[] questDefs =
        [
            ("Buy a Shovel",        "Purchase a shovel from the Shop tab."),
            ("Reach the Right Edge","Guide the river to exit at the east edge."),
        ];

        var gs2 = GameState.Instance;
        for (int i = 0; i < questDefs.Length; i++)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);
            questBox.AddChild(row);
            _questRows[i] = row;

            var indicator = new Label();
            indicator.Text = gs2.QuestsComplete[i] ? "✓" : "○";
            indicator.AddThemeColorOverride("font_color",
                gs2.QuestsComplete[i]
                    ? new Color(0.85f, 0.70f, 0.20f)
                    : new Color(0.5f, 0.5f, 0.5f));
            row.AddChild(indicator);
            _questIndicators[i] = indicator;

            var textCol = new VBoxContainer();
            textCol.AddThemeConstantOverride("separation", 2);
            textCol.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(textCol);

            var titleLabel = new Label();
            titleLabel.Text = questDefs[i].title;
            textCol.AddChild(titleLabel);

            var descLabel = new Label();
            descLabel.Text = questDefs[i].desc;
            descLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
            descLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            textCol.AddChild(descLabel);
        }

        BuildVillageSign();
    }

    private void BuildVillageSign()
    {
        var signStyle = new StyleBoxFlat();
        signStyle.BgColor = new Color(0.10f, 0.10f, 0.12f, 0.90f);
        signStyle.SetCornerRadiusAll(6);
        signStyle.SetContentMarginAll(10);

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", signStyle);
        panel.AnchorLeft = 0f;
        panel.AnchorRight = 0f;
        panel.AnchorTop = 0f;
        panel.AnchorBottom = 0f;
        panel.Position = new Vector2(640, 10);
        AddChild(panel);
        _villageSign = panel;

        var vb = new VBoxContainer();
        vb.AddThemeConstantOverride("separation", 4);
        panel.AddChild(vb);

        var title = new Label();
        title.Text = "Village";
        title.AddThemeFontSizeOverride("font_size", 14);
        title.AddThemeColorOverride("font_color", new Color(0.85f, 0.65f, 0.15f));
        vb.AddChild(title);

        var req = new Label();
        req.Text = $"Needs ≥{(int)GameState.VillageFlowThreshold} flow";
        req.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        vb.AddChild(req);

        _villageFlowLabel = new Label();
        _villageFlowLabel.Text = "Flow: 0";
        _villageFlowLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
        vb.AddChild(_villageFlowLabel);

        _villageGateLabel = new Label();
        _villageGateLabel.Text = "Gate: CLOSED";
        _villageGateLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.3f, 0.3f));
        vb.AddChild(_villageGateLabel);

        _villageSign.Visible = false;
    }

    private Vector2[] DiamondAt(int index)
    {
        float cx = Mhw + index * Mhw;
        float cy = Mhh + index * Mhh;
        return [
            new(cx,        cy - Mhh),
            new(cx + Mhw,  cy),
            new(cx,        cy + Mhh),
            new(cx - Mhw,  cy),
        ];
    }

    private void AddRegionDiamond(int index)
    {
        var poly = new Polygon2D();
        poly.Polygon = DiamondAt(index);
        poly.Color = DiamondColor(index);
        _mapContainer.AddChild(poly);
        _regionDiamonds.Add(poly);
        _mapContainer.CustomMinimumSize = new Vector2((index + 2) * Mhw, (index + 2) * Mhh);
    }

    private Color DiamondColor(int index)
        => index == GameState.Instance.CurrentRegion
            ? new Color(0.75f, 0.60f, 0.30f)
            : new Color(0.35f, 0.28f, 0.18f);

    private void UpdateRegionDiamonds()
    {
        for (int i = 0; i < _regionDiamonds.Count; i++)
            _regionDiamonds[i].Color = DiamondColor(i);
    }

    private void OnMapInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton btn || !btn.Pressed || btn.ButtonIndex != MouseButton.Left)
            return;
        for (int i = 0; i < _regionDiamonds.Count; i++)
        {
            if (Geometry2D.IsPointInPolygon(btn.Position, _regionDiamonds[i].Polygon))
            {
                EmitSignal(SignalName.RegionSelected, i);
                return;
            }
        }
    }

    private void OnGoldChanged(int newValue)
    {
        _goldLabel.Text = $"Gold: {newValue}";
        _buyButton.Disabled = newValue < GameState.ShovelCost;
    }

    private void OnShovelsChanged(int newValue)
    {
        _toolShovelBtn.Visible = newValue > 0;
        _toolShovelBtn.Disabled = newValue == 0;
        _buyButton.Visible = newValue == 0;
        _shopEmptyLabel.Visible = newValue > 0;
    }

    private void OnToolChanged(int tool)
    {
        _toolPanBtn.ButtonPressed = tool == (int)GameState.ActiveTool.Pan;
        _toolShovelBtn.ButtonPressed = tool == (int)GameState.ActiveTool.Shovel;
    }

    private void OnRegionUnlocked(int count)
    {
        AddRegionDiamond(count - 1);
        if (count >= 2 && GameState.Instance.CurrentZone == 0)
            _zoneToggle.Visible = true;
    }

    private void OnRegionSwitched(int index)
    {
        UpdateRegionDiamonds();
        var showVillage = GameState.Instance.CurrentZone == 0 && index == 1;
        _villageSign.Visible = showVillage;
        if (showVillage) UpdateVillageSign();
    }

    private void OnZoneChanged(int zone)
    {
        _lowlandsBtn.ButtonPressed  = zone == 0;
        _highlandsBtn.ButtonPressed = zone == 1;

        foreach (var d in _regionDiamonds)
            d.QueueFree();
        _regionDiamonds.Clear();

        var gs = GameState.Instance;
        for (int i = 0; i < gs.UnlockedRegions; i++)
            AddRegionDiamond(i);

        _villageSign.Visible = false;
    }

    private void OnQuestChanged(int index)
    {
        _questRows[index].Visible = false;
    }

    private void OnFlowChangedSign()
    {
        if (GameState.Instance.CurrentRegion != 1) return;
        UpdateVillageSign();
    }

    private void UpdateVillageSign()
    {
        var gs = GameState.Instance;
        if (gs.RegionData.Count <= 1) return;
        float flow = gs.TileFlowValues[0, 7];
        bool open = flow >= GameState.VillageFlowThreshold;
        _villageFlowLabel.Text = $"Flow: {(int)flow}";
        _villageGateLabel.Text = open ? "Gate: OPEN" : "Gate: CLOSED";
        _villageGateLabel.AddThemeColorOverride("font_color",
            open ? new Color(0.3f, 0.85f, 0.3f) : new Color(0.8f, 0.3f, 0.3f));
    }
}
