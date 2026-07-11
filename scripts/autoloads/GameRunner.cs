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

    private WaterPropagation _water; // kept only for InitTiles (DAG propagation unused)
    private FlowModel _flow;
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
        _flow = new FlowModel();
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
    // on real-time _Process. Flow + rates are recomputed on the propagation interval.
    public void Tick(double delta)
    {
        _propagationTimer += delta;
        if (_propagationTimer >= PropagationInterval)
        {
            _propagationTimer -= PropagationInterval;
            Simulate();
        }
    }

    // Recompute scalar flow, then the rate economy, then progress (unlock/sync). Called
    // on the tick interval, after every tile edit, and on region/zone switch and load.
    private void Simulate()
    {
        _flow.Recompute();
        _economy.Recompute();
        _regions.TryUnlock();
        if (ShouldSyncEntries()) _regions.SyncNextEntries();
    }

    // Force a full recompute now, independent of the propagation timer (tests, load).
    public void StepPropagation() => Simulate();

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
            grid.BrickRequested += OnPlaceBrick;
            grid.FurnaceRequested += OnFurnace;
            grid.AutopanRequested += OnAutopan;
            grid.ShovelRentalRequested += OnShovelRental;
            grid.VillageToggleRequested += OnVillageToggle;
            grid.RegionSelected += OnSwitchRegion;
        }

        var hud = GetTree().GetFirstNodeInGroup("hud") as HUD;
        if (hud != null)
        {
            hud.ToolSelected += OnSetTool;
            hud.RegionSelected += OnSwitchRegion;
            hud.ZoneSwitchRequested += z => _regions.SwitchZone(z);
            hud.SaveRequested += SaveActiveSlot;
        }
    }

    private void OnDig(int col, int row)
    {
        // Digging needs a shovel rental supplied with gold (req: dig is rented).
        if (!GameState.Instance.ShovelsEnabled) return;
        if (!_tiles.CanDig(col, row)) return;
        _tiles.Dig(col, row);
        Simulate();
    }

    private bool ShouldSyncEntries()
        => GameState.Instance.CurrentRegion != 1 || _gates.IsGateOpen;

    private void OnPlaceBrick(int col, int row)
    {
        if (!_tiles.CanPlaceBrick(col, row)) return;
        _tiles.PlaceBrick(col, row); // brick is exempt from flow consumption
        Simulate();
    }

    // Furnace tool on bare Soil places a furnace (free, capped per map); clicking an
    // existing furnace (with any tool) toggles it on/off.
    public void OnFurnace(int col, int row)
    {
        var gs = GameState.Instance;
        if (gs.Tiles[row, col] == GameState.TileType.Furnace)
        {
            _tiles.ToggleFurnace(col, row);
            Simulate();
            return;
        }
        if (!_tiles.CanPlaceFurnace(col, row)) return;
        _tiles.PlaceFurnace(col, row);
        Simulate();
    }

    // Autopan: kind 1 = gold, 2 = clay (placement by the matching tool); kind 0 = toggle
    // an existing machine (front-dispatched from any tool when one sits on the tile).
    public void OnAutopan(int col, int row, int kind)
    {
        var gs = GameState.Instance;
        if (gs.TileMachine[row, col] != 0f)
        {
            // kind -1 = remove (dig tool, only while shovels are rented); else toggle.
            if (kind < 0)
            {
                if (!gs.ShovelsEnabled) return;
                _tiles.RemoveMachine(col, row);
            }
            else
            {
                _tiles.ToggleMachine(col, row);
            }
            Simulate();
            return;
        }
        if (kind <= 0 || !_tiles.CanPlaceAutopan(col, row, kind)) return;
        _tiles.PlaceAutopan(col, row, kind);
        Simulate();
    }

    // Shovel Rental tool on bare Soil places a rental (free, capped per map). Clicking an
    // existing rental with the dig tool demolishes it (handled by OnDig -> Dig).
    public void OnShovelRental(int col, int row)
    {
        if (!_tiles.CanPlaceShovelRental(col, row)) return;
        _tiles.PlaceShovelRental(col, row);
        Simulate();
    }

    // Click a gold-trading village (with any tool) to toggle its gold drain on/off.
    public void OnVillageToggle(int col, int row)
    {
        var gs = GameState.Instance;
        var village = VillageDefs.ForRegion(gs.CurrentZone, gs.CurrentRegion);
        if (village == null || village.GoldDemand <= 0f) return;
        int id = VillageDefs.IndexOf(village);
        gs.VillageSupplyOn[id] = !gs.VillageSupplyOn[id];
        Simulate();
        // Refresh the village sign even when the supplied state didn't flip.
        gs.EmitSignal(GameState.SignalName.VillageSupplyChanged, id, gs.VillageSupplied[id]);
    }

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
        Simulate();
    }

    public void OnSwitchRegion(int index)
    {
        _regions.SwitchTo(index);
        Simulate();
    }
}
