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
    [Signal] public delegate void BuyFurnaceRequestedEventHandler();
    [Signal] public delegate void MakeBrickRequestedEventHandler();

    private const int Mhw = 22;
    private const int Mhh = 11;

    private Label _goldLabel;
    private Label _clayLabel;
    private Label _brickLabel;
    private Button _buyButton;
    private Button _furnaceButton;
    private Button _makeBrickButton;
    private Label _shopEmptyLabel;
    private Button _toolPanBtn;
    private Button _toolShovelBtn;
    private Button _toolBrickBtn;
    private Control _mapContainer;
    private HBoxContainer _zoneToggle;
    private Button _lowlandsBtn;
    private Button _highlandsBtn;

    private readonly List<Polygon2D> _regionDiamonds = [];
    private readonly List<Control> _tabContents = [];
    private readonly List<Button> _tabButtons = [];
    private readonly Label[] _questIndicators = new Label[QuestSystem.Defs.Length];
    private readonly HBoxContainer[] _questRows = new HBoxContainer[QuestSystem.Defs.Length];

    private Control _villageSign;
    private Label _villageFlowLabel;
    private Label _villageGateLabel;

    private Control _objectivePanel;
    private Label _objectiveTitle;
    private Label _objectiveHint;

    private Control _villageDialog;
    private Label _villageDialogName;
    private Label _villageDialogText;

    public override void _Ready()
    {
        AddToGroup("hud");
        BuildUi();

        var gs = GameState.Instance;
        gs.GoldChanged += OnGoldChanged;
        gs.ClayChanged += OnClayChanged;
        gs.ShovelsChanged += OnShovelsChanged;
        gs.BricksChanged += OnBricksChanged;
        gs.FurnaceChanged += OnFurnaceChanged;
        gs.ToolChanged += OnToolChanged;
        gs.RegionUnlocked += OnRegionUnlocked;
        gs.RegionSwitched += OnRegionSwitched;
        gs.ZoneChanged += OnZoneChanged;
        gs.QuestChanged += OnQuestChanged;
        gs.FlowChanged += OnFlowChangedSign;
        gs.VillageFound += OnVillageDiscovered;

        // A save where the village is already discovered keeps the Highlands
        // toggle available (without replaying the discovery dialogue).
        _zoneToggle.Visible = gs.VillageDiscovered;

        // Initialise the UI from current state. On load, ApplySnapshot's signals
        // fired on the title screen before this game-scene HUD existed.
        OnGoldChanged(gs.Gold);
        OnClayChanged(gs.Clay);
        OnShovelsChanged(gs.Shovels);
        OnFurnaceChanged(gs.HasFurnace);
        OnBricksChanged(gs.Bricks);
        OnToolChanged((int)gs.Tool);
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

        _brickLabel = new Label();
        _brickLabel.Text = "Bricks: 0";
        _brickLabel.AddThemeFontSizeOverride("font_size", 24);
        _brickLabel.Visible = false;
        vbox.AddChild(_brickLabel);

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

        _toolBrickBtn = new Button();
        _toolBrickBtn.Text = "Brick";
        _toolBrickBtn.CustomMinimumSize = new Vector2(80, 48);
        _toolBrickBtn.ToggleMode = true;
        _toolBrickBtn.ButtonGroup = toolGroup;
        _toolBrickBtn.Disabled = true;
        _toolBrickBtn.Visible = false;
        _toolBrickBtn.Pressed += () => EmitSignal(SignalName.ToolSelected, (int)GameState.ActiveTool.Brick);
        toolHbox.AddChild(_toolBrickBtn);

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

        _furnaceButton = new Button();
        _furnaceButton.Text = $"Buy Furnace ({GameState.FurnaceCost}g)";
        _furnaceButton.CustomMinimumSize = new Vector2(0, 44);
        _furnaceButton.Disabled = true;
        _furnaceButton.Pressed += () => EmitSignal(SignalName.BuyFurnaceRequested);
        shopBox.AddChild(_furnaceButton);

        _makeBrickButton = new Button();
        _makeBrickButton.Text = $"Make Brick ({GameState.BrickClayCost} clay)";
        _makeBrickButton.CustomMinimumSize = new Vector2(0, 44);
        _makeBrickButton.Visible = false;
        _makeBrickButton.Pressed += () => EmitSignal(SignalName.MakeBrickRequested);
        shopBox.AddChild(_makeBrickButton);

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

        var gs2 = GameState.Instance;
        for (int i = 0; i < QuestSystem.Defs.Length; i++)
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
            titleLabel.Text = QuestSystem.Defs[i].Title;
            textCol.AddChild(titleLabel);

            var descLabel = new Label();
            descLabel.Text = QuestSystem.Defs[i].Hint;
            descLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
            descLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            textCol.AddChild(descLabel);
        }

        BuildVillageSign();
        BuildObjectiveBanner();
        BuildVillageDialog();
    }

    private void BuildVillageDialog()
    {
        // Full-screen dim that also blocks clicks behind the modal while shown.
        var dim = new ColorRect();
        dim.Color = new Color(0, 0, 0, 0.5f);
        dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        dim.MouseFilter = Control.MouseFilterEnum.Stop;
        AddChild(dim);
        _villageDialog = dim;

        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        dim.AddChild(center);

        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.10f, 0.10f, 0.12f, 0.98f);
        style.SetCornerRadiusAll(8);
        style.SetContentMarginAll(16);
        style.BorderColor = new Color(0.85f, 0.65f, 0.15f);
        style.SetBorderWidthAll(2);

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", style);
        center.AddChild(panel);

        var vb = new VBoxContainer();
        vb.AddThemeConstantOverride("separation", 10);
        vb.CustomMinimumSize = new Vector2(440, 0);
        panel.AddChild(vb);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 10);
        vb.AddChild(header);

        // Placeholder portrait (per-village art is future work).
        var portrait = new ColorRect();
        portrait.Color = new Color(0.55f, 0.45f, 0.30f);
        portrait.CustomMinimumSize = new Vector2(48, 48);
        header.AddChild(portrait);

        _villageDialogName = new Label();
        _villageDialogName.AddThemeFontSizeOverride("font_size", 20);
        _villageDialogName.AddThemeColorOverride("font_color", new Color(0.85f, 0.65f, 0.15f));
        _villageDialogName.VerticalAlignment = VerticalAlignment.Center;
        header.AddChild(_villageDialogName);

        _villageDialogText = new Label();
        _villageDialogText.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _villageDialogText.CustomMinimumSize = new Vector2(440, 0);
        _villageDialogText.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
        vb.AddChild(_villageDialogText);

        var continueBtn = new Button();
        continueBtn.Text = "Continue";
        continueBtn.CustomMinimumSize = new Vector2(0, 40);
        continueBtn.Pressed += () => _villageDialog.Visible = false;
        vb.AddChild(continueBtn);

        _villageDialog.Visible = false;
    }

    private void OnVillageDiscovered()
    {
        _villageDialogName.Text = "Sediment, Village Elder";
        _villageDialogText.Text =
            "Our river loses its strength against bare soil and banks long before it reaches us. "
            + $"We need at least {(int)GameState.VillageFlowThreshold} flow. "
            + "Bring clay down from the highlands, fire it into brick, and line the channel with it — "
            + "water holds its flow when hemmed in by brick. Only then will we have enough.";
        _villageDialog.Visible = true;
        _zoneToggle.Visible = true;
        // The village explains the furnace, so reveal it in the shop now.
        OnFurnaceChanged(GameState.Instance.HasFurnace);
    }

    private void BuildObjectiveBanner()
    {
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.10f, 0.10f, 0.12f, 0.90f);
        style.SetCornerRadiusAll(6);
        style.SetContentMarginAll(10);

        var panel = new PanelContainer();
        panel.Position = new Vector2(10, 10);
        panel.AddThemeStyleboxOverride("panel", style);
        AddChild(panel);
        _objectivePanel = panel;

        var vb = new VBoxContainer();
        vb.AddThemeConstantOverride("separation", 2);
        vb.CustomMinimumSize = new Vector2(260, 0);
        panel.AddChild(vb);

        var heading = new Label();
        heading.Text = "Objective";
        heading.AddThemeFontSizeOverride("font_size", 14);
        heading.AddThemeColorOverride("font_color", new Color(0.85f, 0.70f, 0.20f));
        vb.AddChild(heading);

        _objectiveTitle = new Label();
        _objectiveTitle.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
        vb.AddChild(_objectiveTitle);

        _objectiveHint = new Label();
        _objectiveHint.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        _objectiveHint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        vb.AddChild(_objectiveHint);

        UpdateObjectiveBanner();
    }

    private void UpdateObjectiveBanner()
    {
        int i = QuestSystem.CurrentObjective();
        if (i < 0)
        {
            _objectiveTitle.Text = "All objectives complete!";
            _objectiveHint.Visible = false;
        }
        else
        {
            _objectiveTitle.Text = QuestSystem.Defs[i].Title;
            _objectiveHint.Text = QuestSystem.Defs[i].Hint;
            _objectiveHint.Visible = true;
        }
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
        _furnaceButton.Disabled = newValue < GameState.FurnaceCost;
    }

    private void OnClayChanged(int newValue)
    {
        _clayLabel.Text = $"Clay: {newValue}";
        _makeBrickButton.Disabled = newValue < GameState.BrickClayCost;
    }

    private void OnShovelsChanged(int newValue)
    {
        _toolShovelBtn.Visible = newValue > 0;
        _toolShovelBtn.Disabled = newValue == 0;
        _buyButton.Visible = newValue == 0;
    }

    private void OnBricksChanged(int newValue)
    {
        _brickLabel.Text = $"Bricks: {newValue}";
        _toolBrickBtn.Disabled = newValue == 0;
    }

    private void OnFurnaceChanged(bool hasFurnace)
    {
        // The furnace only appears in the shop once the village has explained it.
        _furnaceButton.Visible = !hasFurnace && GameState.Instance.VillageDiscovered;
        _makeBrickButton.Visible = hasFurnace;
        _brickLabel.Visible = hasFurnace;
        _toolBrickBtn.Visible = hasFurnace;
    }

    private void OnToolChanged(int tool)
    {
        _toolPanBtn.ButtonPressed = tool == (int)GameState.ActiveTool.Pan;
        _toolShovelBtn.ButtonPressed = tool == (int)GameState.ActiveTool.Shovel;
        _toolBrickBtn.ButtonPressed = tool == (int)GameState.ActiveTool.Brick;
    }

    private void OnRegionUnlocked(int count)
    {
        AddRegionDiamond(count - 1);
        // The Highlands toggle is revealed on village discovery (OnVillageDiscovered),
        // not merely on unlocking region 1.
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
        _questIndicators[index].Text = "✓";
        _questIndicators[index].AddThemeColorOverride("font_color", new Color(0.85f, 0.70f, 0.20f));
        UpdateObjectiveBanner();
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
