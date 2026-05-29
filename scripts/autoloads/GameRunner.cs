using Godot;

public partial class GameRunner : Node
{
    public static GameRunner Instance { get; private set; }

    [Export] public float PropagationInterval = 0.2f;

    // When true, the automatic per-frame tick is suspended so tests can step
    // the simulation deterministically via Tick()/StepPropagation(). Production
    // boots with this off, leaving behaviour unchanged.
    public bool TestMode = false;

    // False until a game is started from the title screen (New Game / Continue).
    // Gates real-time ticking so the simulation doesn't run behind the menu.
    public bool Active = false;

    private WaterPropagation _water;
    private TileEditor _tiles;
    private Economy _economy;
    private RegionSystem _regions;
    private QuestSystem _quests;
    private GateSystem _gates;
    private VillageSystem _village;
    private double _propagationTimer;

    private readonly SaveSystem _save = new();
    public SaveSystem Save => _save;
    public int ActiveSlot { get; private set; } = -1;

    public override void _Ready()
    {
        Instance = this;
        _water = new WaterPropagation();
        _tiles = new TileEditor();
        _economy = new Economy();
        _regions = new RegionSystem();

        _quests = new QuestSystem();
        _quests.Connect();

        _gates = new GateSystem(_regions);
        _gates.Connect();

        _village = new VillageSystem();
        _village.Connect();

        _water.InitTiles();
        _regions.Init();

        GameState.Instance.RegionSwitched += _water.OnRegionSwitch;
        GameState.Instance.ZoneChanged += OnZoneChanged;

        // View signals are wired by the game scene once Grid/HUD exist (main.gd
        // calls ConnectViewSignals deferred). The title scene has no Grid/HUD.
    }

    public override void _Process(double delta)
    {
        if (TestMode || !Active) return;
        Tick(delta);
    }

    // The per-frame simulation step. Tests drive this directly instead of relying
    // on real-time _Process.
    public void Tick(double delta)
    {
        _economy.TickGold(delta);
        _economy.TickClay(delta);
        _propagationTimer += delta;
        if (_propagationTimer >= PropagationInterval)
        {
            _propagationTimer -= PropagationInterval;
            _water.Propagate();
        }
    }

    // Force a flow recompute now, independent of the propagation timer.
    public void StepPropagation() => _water.Propagate();

    // Reset to a fresh new game and re-run the same init sequence as boot.
    // Used by the test harness before each scenario; the future "New Game"
    // menu button will call this too.
    public void StartNewGame()
    {
        GameState.Instance.ResetToNewGame();
        _water.InitTiles();
        _regions.Init();
        _propagationTimer = 0;
    }

    // Start a fresh game in the given slot and write its initial save.
    public void NewGameInSlot(int slot)
    {
        StartNewGame();
        ActiveSlot = slot;
        _save.Save(slot);
        Active = true;
    }

    // Load a saved slot into GameState and make it the active game.
    public void LoadSlot(int slot)
    {
        ActiveSlot = slot;
        _save.Load(slot);
        StepPropagation();
        Active = true;
    }

    // Write the current game state to whichever slot is active (HUD Save button).
    public void SaveActiveSlot()
    {
        if (ActiveSlot >= 0)
            _save.Save(ActiveSlot);
    }

    public void ConnectViewSignals()
    {
        var grid = GetTree().GetFirstNodeInGroup("grid") as Grid;
        if (grid != null)
        {
            grid.DigRequested += OnDig;
            grid.PanRequested += OnPan;
        }

        var hud = GetTree().GetFirstNodeInGroup("hud") as HUD;
        if (hud != null)
        {
            hud.BuyShovelRequested += OnBuyShovel;
            hud.ToolSelected += OnSetTool;
            hud.RegionSelected += OnSwitchRegion;
            hud.ZoneSwitchRequested += z => _regions.SwitchZone(z);
            hud.SaveRequested += SaveActiveSlot;
        }
    }

    private void OnDig(int col, int row)
    {
        if (!_tiles.CanDig(col, row)) return;
        _tiles.Dig(col, row);
        _regions.TryUnlock();
        if (ShouldSyncEntries()) _regions.SyncNextEntries();
    }

    private bool ShouldSyncEntries()
        => GameState.Instance.CurrentRegion != 1 || _gates.IsGateOpen;

    private void OnPan(int col, int row)
    {
        _economy.Pan(col, row);
    }

    public void OnBuyShovel() => _economy.BuyShovel();

    public void OnSetTool(int tool)
    {
        var gs = GameState.Instance;
        gs.Tool = (GameState.ActiveTool)tool;
        gs.EmitSignal(GameState.SignalName.ToolChanged, tool);
    }

    private void OnZoneChanged(int zone)
    {
        var color = zone == 0
            ? new Color(0.72f, 0.78f, 0.82f)
            : new Color(0.62f, 0.52f, 0.38f);
        RenderingServer.SetDefaultClearColor(color);
        _water.Propagate();
    }

    public void OnSwitchRegion(int index)
    {
        _regions.SwitchTo(index);
        _regions.TryUnlock();
        if (ShouldSyncEntries()) _regions.SyncNextEntries();
    }
}
