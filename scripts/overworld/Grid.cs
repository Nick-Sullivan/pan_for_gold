using Godot;

[GlobalClass]
public partial class Grid : Node2D
{
    [Signal] public delegate void DigRequestedEventHandler(int col, int row);
    [Signal] public delegate void PanRequestedEventHandler(int col, int row);

    private TileRenderer _renderer;
    private Line2D _hoverLine;

    public override void _Ready()
    {
        AddToGroup("grid");

        _renderer = new TileRenderer();
        AddChild(_renderer);
        _renderer.Build();

        _hoverLine = new Line2D();
        _hoverLine.Width = 2.0f;
        _hoverLine.DefaultColor = new Color(1.0f, 1.0f, 0.8f, 0.9f);
        _hoverLine.Visible = false;
        AddChild(_hoverLine);

        _renderer.BuildPreview();

        var gs = GameState.Instance;
        gs.TileChanged += _OnTileChanged;
        gs.TileGoldChanged += _OnTileGoldChanged;
        gs.TileClayChanged += _OnTileClayChanged;
        gs.ToolChanged += _OnToolChanged;
        gs.RegionSwitched += _OnRegionSwitched;
        gs.RegionUnlocked += _OnRegionUnlocked;
        gs.ZoneChanged += _OnZoneChanged;
        gs.FlowChanged += _OnFlowChanged;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion)
        {
            _UpdateHover(motion.Position);
            return;
        }
        if (@event is not InputEventMouseButton btn || !btn.Pressed || btn.ButtonIndex != MouseButton.Left)
            return;

        var tile = IsoMath.ScreenToTile(btn.Position);
        if (tile.X < 0 || tile.X >= GameState.Cols || tile.Y < 0 || tile.Y >= GameState.Rows)
            return;

        var gs = GameState.Instance;
        if (gs.Tool == GameState.ActiveTool.Shovel)
        {
            EmitSignal(SignalName.DigRequested, tile.X, tile.Y);
        }
        else if (gs.Tool == GameState.ActiveTool.Pan)
        {
            var t = gs.Tiles[tile.Y, tile.X];
            if (t == GameState.TileType.Bank)
            {
                int amount = (int)gs.TileGold[tile.Y, tile.X];
                EmitSignal(SignalName.PanRequested, tile.X, tile.Y);
                _FlashTile(tile.X, tile.Y);
                if (amount > 0)
                    _ShowGoldPopup(tile.X, tile.Y, amount);
            }
        }
    }

    private void _UpdateHover(Vector2 mousePos)
    {
        var tile = IsoMath.ScreenToTile(mousePos);
        if (tile.X < 0 || tile.X >= GameState.Cols || tile.Y < 0 || tile.Y >= GameState.Rows)
        {
            _hoverLine.Visible = false;
            return;
        }

        var gs = GameState.Instance;
        bool clickable = false;
        if (gs.Tool == GameState.ActiveTool.Shovel && gs.Shovels > 0)
            clickable = true;
        else if (gs.Tool == GameState.ActiveTool.Pan)
        {
            var t = gs.Tiles[tile.Y, tile.X];
            clickable = t == GameState.TileType.Bank;
        }

        if (!clickable)
        {
            _hoverLine.Visible = false;
            return;
        }

        var verts = IsoMath.DiamondVerts(tile.X, tile.Y);
        _hoverLine.Points = [verts[0], verts[1], verts[2], verts[3], verts[0]];
        _hoverLine.Visible = true;
    }

    private void _ShowGoldPopup(int col, int row, int amount)
    {
        var label = new Label();
        label.Text = $"+{amount}";
        label.AddThemeFontSizeOverride("font_size", 16);
        label.AddThemeColorOverride("font_color", new Color(1.0f, 0.88f, 0.3f));
        label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.8f));
        label.AddThemeConstantOverride("shadow_offset_x", 1);
        label.AddThemeConstantOverride("shadow_offset_y", 1);
        var center = IsoMath.TileCenter(col, row);
        label.Position = center - new Vector2(20, 10);
        AddChild(label);

        var tween = CreateTween();
        tween.TweenProperty(label, "position", label.Position + new Vector2(0, -40), 0.7);
        tween.Parallel().TweenProperty(label, "modulate:a", 0.0f, 0.7);
        tween.TweenCallback(Callable.From(label.QueueFree));
    }

    private void _FlashTile(int col, int row)
    {
        var poly = _renderer.GetTileNode(col, row);
        var tween = CreateTween();
        tween.TweenProperty(poly, "modulate", new Color(1.5f, 1.3f, 0.9f), 0.05);
        tween.TweenProperty(poly, "modulate", Colors.White, 0.2);
    }

    private void _OnTileChanged(int col, int row)
    {
        _renderer.RefreshWall(col, row);
        _renderer.RefreshTileAndNeighbors(col, row);
    }

    private void _OnTileGoldChanged(int col, int row, int amount)
    {
        _renderer.RefreshGold(col, row, amount);
    }

    private void _OnTileClayChanged(int col, int row, int amount)
    {
        _renderer.RefreshClay(col, row, amount);
    }

    private void _OnFlowChanged()
    {
        _renderer.RefreshAllFlow();
    }

    private void _OnRegionSwitched(int _index)
    {
        _renderer.RefreshAllTiles();
        _renderer.BuildPreview();
    }

    private void _OnZoneChanged(int _zone)
    {
        _renderer.RefreshAllTiles();
        _renderer.BuildPreview();
    }

    private void _OnRegionUnlocked(int _count)
    {
        _renderer.BuildPreview();
    }

    private void _OnToolChanged(int _tool) { }
}
