using Godot;

public partial class GameRunner : Node
{
    [Export] public float PropagationInterval = 0.2f;

    private WaterPropagation _water;
    private TileEditor _tiles;
    private Economy _economy;
    private RegionSystem _regions;
    private QuestSystem _quests;
    private GateSystem _gates;
    private double _propagationTimer;

    public override void _Ready()
    {
        _water = new WaterPropagation();
        _tiles = new TileEditor();
        _economy = new Economy();
        _regions = new RegionSystem();

        _quests = new QuestSystem();
        _quests.Connect();

        _gates = new GateSystem(_regions);
        _gates.Connect();

        _water.InitTiles();
        _regions.Init();

        GameState.Instance.RegionSwitched += _water.OnRegionSwitch;
        GameState.Instance.ZoneChanged += OnZoneChanged;

        GetTree().Root.Ready += ConnectViewSignals;
    }

    public override void _Process(double delta)
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

    private void ConnectViewSignals()
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
