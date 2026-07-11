using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class HUD : CanvasLayer
{
    [Signal] public delegate void ToolSelectedEventHandler(int tool);
    [Signal] public delegate void RegionSelectedEventHandler(int index);
    [Signal] public delegate void ZoneSwitchRequestedEventHandler(int zone);
    [Signal] public delegate void SaveRequestedEventHandler();

    private const int Mhw = 22;
    private const int Mhh = 11;

    private Label _goldLabel;
    private Label _clayLabel;
    private Label _brickLabel;
    private Label _flowLabel;
    private Button _toolShovelBtn;
    private Button _toolBrickBtn;
    private Button _toolFurnaceBtn;
    private Button _toolAutopanGoldBtn;
    private Button _toolAutopanClayBtn;
    private Button _toolShovelRentalBtn;
    private Control _mapContainer;
    private HBoxContainer _zoneToggle;
    private Button _lowlandsBtn;
    private Button _highlandsBtn;

    private readonly List<Polygon2D> _regionDiamonds = [];
    private readonly List<Control> _tabContents = [];
    private readonly List<Button> _tabButtons = [];

    private Control _villageSign;
    private Label _villageTitleLabel;
    private Label _villageReqLabel;
    private Label _villageFlowLabel;
    private Label _villageGateLabel;

    private Control _objectivePanel;
    private Label _objectiveTitle;
    private Label _objectiveHint;

    private Control _villageDialog;
    private Label _villageDialogName;
    private Label _villageDialogText;
    private ColorRect _villageDialogPortrait;
    private StyleBoxFlat _villageDialogBorder;

    public override void _Ready()
    {
        AddToGroup("hud");
        BuildUi();

        var gs = GameState.Instance;
        gs.RatesChanged += OnRatesChanged;
        gs.FurnaceChanged += OnFurnaceChanged;
        gs.ToolChanged += OnToolChanged;
        gs.RegionUnlocked += OnRegionUnlocked;
        gs.RegionSwitched += OnRegionSwitched;
        gs.ZoneChanged += OnZoneChanged;
        gs.QuestChanged += OnQuestChanged;
        gs.FlowChanged += OnFlowChangedSign;
        gs.VillageFound += OnVillageDiscovered;
        gs.VillageSupplyChanged += (_, __) => OnFlowChangedSign();

        // A save where the village is already discovered keeps the Highlands
        // toggle available (without replaying the discovery dialogue).
        _zoneToggle.Visible = gs.VillageDiscovered;

        // Initialise the UI from current state. On load, ApplySnapshot's signals
        // fired on the title screen before this game-scene HUD existed.
        OnFurnaceChanged(gs.HasFurnace);
        OnRatesChanged();
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
        // Compact panel, anchored flush to the right edge (~1280 wide).
        panel.Position = new Vector2(1042, 10);
        panel.AddThemeStyleboxOverride("panel", style);
        AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.CustomMinimumSize = new Vector2(212, 0);
        vbox.AddThemeConstantOverride("separation", 3);
        margin.AddChild(vbox);

        // Rate readouts for the current map (generation vs consumption). No accumulation.
        _flowLabel = new Label();
        _flowLabel.Text = "Flow: in 0 / out 0";
        _flowLabel.AddThemeFontSizeOverride("font_size", 14);
        _flowLabel.AddThemeColorOverride("font_color", new Color(0.55f, 0.75f, 1.0f));
        vbox.AddChild(_flowLabel);

        _goldLabel = new Label();
        _goldLabel.Text = "Gold: +0/s − 0/s";
        _goldLabel.AddThemeFontSizeOverride("font_size", 15);
        vbox.AddChild(_goldLabel);

        _clayLabel = new Label();
        _clayLabel.Text = "Clay: +0/s − 0/s";
        _clayLabel.AddThemeFontSizeOverride("font_size", 15);
        vbox.AddChild(_clayLabel);

        _brickLabel = new Label();
        _brickLabel.Text = "Bricks: +0/s − 0/s";
        _brickLabel.AddThemeFontSizeOverride("font_size", 15);
        _brickLabel.Visible = false;
        vbox.AddChild(_brickLabel);

        vbox.AddChild(new HSeparator());

        // Tab bar
        var tabHbox = new HBoxContainer();
        tabHbox.AddThemeConstantOverride("separation", 4);
        vbox.AddChild(tabHbox);

        var tabGroup = new ButtonGroup();
        string[] tabLabels = ["Equip", "Build", "Map"];
        for (int i = 0; i < tabLabels.Length; i++)
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

        _toolShovelBtn = new Button();
        _toolShovelBtn.Text = "Dig";
        _toolShovelBtn.CustomMinimumSize = new Vector2(60, 32);
        _toolShovelBtn.ToggleMode = true;
        _toolShovelBtn.ButtonPressed = true;
        _toolShovelBtn.ButtonGroup = toolGroup;
        _toolShovelBtn.Pressed += () => EmitSignal(SignalName.ToolSelected, (int)GameState.ActiveTool.Shovel);
        toolHbox.AddChild(_toolShovelBtn);

        _toolBrickBtn = new Button();
        _toolBrickBtn.Text = "Brick";
        _toolBrickBtn.CustomMinimumSize = new Vector2(60, 32);
        _toolBrickBtn.ToggleMode = true;
        _toolBrickBtn.ButtonGroup = toolGroup;
        _toolBrickBtn.Visible = false;
        _toolBrickBtn.Pressed += () => EmitSignal(SignalName.ToolSelected, (int)GameState.ActiveTool.Brick);
        toolHbox.AddChild(_toolBrickBtn);

        equipBox.AddChild(new HSeparator());

        var saveBtn = new Button();
        saveBtn.Text = "Save";
        saveBtn.CustomMinimumSize = new Vector2(0, 40);
        saveBtn.Pressed += () => EmitSignal(SignalName.SaveRequested);
        equipBox.AddChild(saveBtn);

        // Tab 2 — Build. Gold Autopanner is available from the start (it taps the river
        // for gold); Furnace + Clay Autopanner unlock with the first village. All buttons
        // share the tool ButtonGroup so only one tool is ever active.
        var buildBox = new VBoxContainer();
        buildBox.AddThemeConstantOverride("separation", 6);
        buildBox.Visible = false;
        vbox.AddChild(buildBox);
        _tabContents.Add(buildBox);

        var buildHbox = new HBoxContainer();
        buildHbox.AddThemeConstantOverride("separation", 6);
        buildBox.AddChild(buildHbox);

        _toolAutopanGoldBtn = new Button();
        _toolAutopanGoldBtn.CustomMinimumSize = new Vector2(0, 32);
        _toolAutopanGoldBtn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _toolAutopanGoldBtn.ToggleMode = true;
        _toolAutopanGoldBtn.ButtonGroup = toolGroup;
        _toolAutopanGoldBtn.Pressed += () => EmitSignal(SignalName.ToolSelected, (int)GameState.ActiveTool.AutopanGold);
        buildHbox.AddChild(_toolAutopanGoldBtn);

        _toolAutopanClayBtn = new Button();
        _toolAutopanClayBtn.CustomMinimumSize = new Vector2(0, 32);
        _toolAutopanClayBtn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _toolAutopanClayBtn.ToggleMode = true;
        _toolAutopanClayBtn.ButtonGroup = toolGroup;
        _toolAutopanClayBtn.Visible = false;
        _toolAutopanClayBtn.Pressed += () => EmitSignal(SignalName.ToolSelected, (int)GameState.ActiveTool.AutopanClay);
        buildHbox.AddChild(_toolAutopanClayBtn);

        _toolFurnaceBtn = new Button();
        _toolFurnaceBtn.CustomMinimumSize = new Vector2(0, 32);
        _toolFurnaceBtn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _toolFurnaceBtn.ToggleMode = true;
        _toolFurnaceBtn.ButtonGroup = toolGroup;
        _toolFurnaceBtn.Visible = false;
        _toolFurnaceBtn.Pressed += () => EmitSignal(SignalName.ToolSelected, (int)GameState.ActiveTool.Furnace);
        buildHbox.AddChild(_toolFurnaceBtn);

        // Shovel Rental — available from the start; while supplied with gold it unlocks the
        // dig tool. On its own full-width row so the build tools fit the narrow panel.
        _toolShovelRentalBtn = new Button();
        _toolShovelRentalBtn.CustomMinimumSize = new Vector2(0, 32);
        _toolShovelRentalBtn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _toolShovelRentalBtn.ToggleMode = true;
        _toolShovelRentalBtn.ButtonGroup = toolGroup;
        _toolShovelRentalBtn.Pressed += () => EmitSignal(SignalName.ToolSelected, (int)GameState.ActiveTool.ShovelRental);
        buildBox.AddChild(_toolShovelRentalBtn);

        var buildHint = new Label();
        buildHint.Text = "Autopanners pan gold/clay on soil beside a connected river; yield scales with flow.\nA Shovel Rental, supplied with gold, unlocks the dig tool.\nFurnaces fire bricks (clay); each laid brick uses brick output. Brick-line to keep flow up.";
        buildHint.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
        buildHint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        buildBox.AddChild(buildHint);

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
        _mapContainer.CustomMinimumSize = new Vector2(212, Mhh * 2 + 4);
        _mapContainer.MouseFilter = Control.MouseFilterEnum.Stop;
        _mapContainer.GuiInput += OnMapInput;
        mapBox.AddChild(_mapContainer);

        AddRegionDiamond(0);

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
        _villageDialogBorder = style;

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

        // Placeholder portrait, tinted per-village (full per-village art is future work).
        _villageDialogPortrait = new ColorRect();
        _villageDialogPortrait.Color = new Color(0.55f, 0.45f, 0.30f);
        _villageDialogPortrait.CustomMinimumSize = new Vector2(48, 48);
        header.AddChild(_villageDialogPortrait);

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

    private void OnVillageDiscovered(int id)
    {
        var village = VillageDefs.All[id];
        _villageDialogName.Text = village.Name;
        _villageDialogName.AddThemeColorOverride("font_color", village.TileColor);
        _villageDialogText.Text = village.Dialogue;
        _villageDialogPortrait.Color = village.TileColor;
        _villageDialogBorder.BorderColor = village.TileColor;
        _villageDialog.Visible = true;

        // The first village unlocks the Highlands toggle (and the furnace + clay
        // autopanner, which VillageSystem unlocks via the FurnaceChanged signal).
        if (id == 0)
            _zoneToggle.Visible = true;
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

        _villageTitleLabel = new Label();
        _villageTitleLabel.Text = "Village";
        _villageTitleLabel.AddThemeFontSizeOverride("font_size", 14);
        _villageTitleLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.65f, 0.15f));
        vb.AddChild(_villageTitleLabel);

        _villageReqLabel = new Label();
        _villageReqLabel.Text = $"Needs ≥{(int)VillageDefs.All[0].FlowThreshold} flow";
        _villageReqLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        vb.AddChild(_villageReqLabel);

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

    // Rates are recomputed every tick; refresh the readouts and the build caps.
    private void OnRatesChanged()
    {
        var gs = GameState.Instance;
        _goldLabel.Text = $"Gold: +{gs.GoldGen:0.#}/s − {gs.GoldUse:0.#}/s";
        _clayLabel.Text = $"Clay: +{gs.ClayGen:0.#}/s − {gs.ClayUse:0.#}/s";
        _brickLabel.Text = $"Bricks: +{gs.BrickGen:0.#}/s − {gs.BrickUse:0.#}/s";

        var rd = gs.RegionData;
        if (rd.Count > gs.CurrentRegion)
        {
            var snap = rd[gs.CurrentRegion];
            _flowLabel.Text = $"Flow: in {(int)snap.InputFlow} / out {(int)snap.OutputFlow}";
        }
        // Dig is gated on a supplied Shovel Rental: hide the button until shovels are enabled.
        _toolShovelBtn.Visible = gs.ShovelsEnabled;
        UpdateBuildButtons();
    }

    private void UpdateBuildButtons()
    {
        var gs = GameState.Instance;
        int gold = 0, clay = 0, furnaces = 0, rentals = 0;
        for (int row = 0; row < GameState.Rows; row++)
            for (int col = 0; col < GameState.Cols; col++)
            {
                int k = GameState.MachineKind(gs.TileMachine[row, col]);
                if (k == 1) gold++;
                else if (k == 2) clay++;
                if (gs.Tiles[row, col] == GameState.TileType.Furnace) furnaces++;
                if (gs.Tiles[row, col] == GameState.TileType.ShovelRental) rentals++;
            }
        int cap = GameState.BuildCapPerType;
        _toolAutopanGoldBtn.Text = $"Gold {gold}/{cap}";
        _toolAutopanClayBtn.Text = $"Clay {clay}/{cap}";
        _toolFurnaceBtn.Text = $"Furn {furnaces}/{cap}";
        _toolShovelRentalBtn.Text = $"Shovel Rental {rentals}/{cap}";
    }

    private void OnFurnaceChanged(bool hasFurnace)
    {
        // "hasFurnace" means the village-tier build tools are unlocked. Reveal the
        // furnace, the clay autopanner, the brick tool, and the brick rate readout.
        _toolFurnaceBtn.Visible = hasFurnace;
        _toolAutopanClayBtn.Visible = hasFurnace;
        _toolBrickBtn.Visible = hasFurnace;
        _brickLabel.Visible = hasFurnace;
    }

    private void OnToolChanged(int tool)
    {
        _toolShovelBtn.ButtonPressed = tool == (int)GameState.ActiveTool.Shovel;
        _toolBrickBtn.ButtonPressed = tool == (int)GameState.ActiveTool.Brick;
        _toolFurnaceBtn.ButtonPressed = tool == (int)GameState.ActiveTool.Furnace;
        _toolAutopanGoldBtn.ButtonPressed = tool == (int)GameState.ActiveTool.AutopanGold;
        _toolAutopanClayBtn.ButtonPressed = tool == (int)GameState.ActiveTool.AutopanClay;
        _toolShovelRentalBtn.ButtonPressed = tool == (int)GameState.ActiveTool.ShovelRental;
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
        var showVillage = VillageDefs.ForRegion(GameState.Instance.CurrentZone, index) != null;
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
        // The Quests tab was removed; only the objective banner reflects progress.
        UpdateObjectiveBanner();
    }

    private void OnFlowChangedSign()
    {
        var gs = GameState.Instance;
        if (VillageDefs.ForRegion(gs.CurrentZone, gs.CurrentRegion) == null) return;
        UpdateVillageSign();
    }

    private void UpdateVillageSign()
    {
        var gs = GameState.Instance;
        var village = VillageDefs.ForRegion(gs.CurrentZone, gs.CurrentRegion);
        if (village == null || gs.RegionData.Count <= gs.CurrentRegion) return;

        _villageTitleLabel.Text = village.Name;
        _villageTitleLabel.AddThemeColorOverride("font_color", village.TileColor);

        if (village.GoldDemand > 0f)
        {
            // Gold-supplied village: show demand, drain toggle, and supply status.
            int id = VillageDefs.IndexOf(village);
            bool on = gs.VillageSupplyOn[id];
            bool supplied = gs.VillageSupplied[id];
            _villageReqLabel.Text = $"Needs {(int)village.GoldDemand}/s gold";
            _villageFlowLabel.Text = on ? "Drain: ON (click to stop)" : "Drain: OFF (click to start)";
            _villageGateLabel.Text = supplied ? "Supplied!" : "Not yet supplied";
            _villageGateLabel.AddThemeColorOverride("font_color",
                supplied ? new Color(0.3f, 0.85f, 0.3f) : new Color(0.8f, 0.3f, 0.3f));
            return;
        }

        _villageReqLabel.Text = $"Needs ≥{(int)village.FlowThreshold} flow";

        float flow = gs.TileFlowValues[village.Row, village.Col];
        bool open = flow >= village.FlowThreshold;
        _villageFlowLabel.Text = $"Flow: {(int)flow}";
        // Terminal villages have no gate; show whether the village is satisfied.
        _villageGateLabel.Text = village.HasEastGate
            ? (open ? "Gate: OPEN" : "Gate: CLOSED")
            : (open ? "Supplied!" : "Not yet supplied");
        _villageGateLabel.AddThemeColorOverride("font_color",
            open ? new Color(0.3f, 0.85f, 0.3f) : new Color(0.8f, 0.3f, 0.3f));
    }
}
