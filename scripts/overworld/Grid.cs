using Godot;

[GlobalClass]
public partial class Grid : Node2D
{
    [Signal] public delegate void DigRequestedEventHandler(int col, int row);
    [Signal] public delegate void PanRequestedEventHandler(int col, int row);
    [Signal] public delegate void BrickRequestedEventHandler(int col, int row);
    [Signal] public delegate void RegionSelectedEventHandler(int index);

    private TileRenderer _renderer;
    private Line2D _hoverLine;
    private Node2D _eastArrow;
    private Node2D _westArrow;
    private Vector2[] _eastHit;
    private Vector2[] _westHit;
    private bool _eastHover;
    private bool _westHover;

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
        _BuildArrows();

        var gs = GameState.Instance;
        gs.TileChanged += _OnTileChanged;
        gs.TileGoldChanged += _OnTileGoldChanged;
        gs.TileClayChanged += _OnTileClayChanged;
        gs.ToolChanged += _OnToolChanged;
        gs.RegionSwitched += _OnRegionSwitched;
        gs.RegionUnlocked += _OnRegionUnlocked;
        gs.ZoneChanged += _OnZoneChanged;
        gs.FlowChanged += _OnFlowChanged;

        _UpdateArrows();
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

        var gs = GameState.Instance;

        // Map-change arrows: click to move to the adjacent region.
        if (_eastArrow.Visible && Geometry2D.IsPointInPolygon(btn.Position, _eastHit))
        {
            EmitSignal(SignalName.RegionSelected, gs.CurrentRegion + 1);
            return;
        }
        if (_westArrow.Visible && Geometry2D.IsPointInPolygon(btn.Position, _westHit))
        {
            EmitSignal(SignalName.RegionSelected, gs.CurrentRegion - 1);
            return;
        }

        var tile = IsoMath.ScreenToTile(btn.Position);
        if (tile.X < 0 || tile.X >= GameState.Cols || tile.Y < 0 || tile.Y >= GameState.Rows)
            return;

        if (gs.Tool == GameState.ActiveTool.Shovel)
        {
            EmitSignal(SignalName.DigRequested, tile.X, tile.Y);
        }
        else if (gs.Tool == GameState.ActiveTool.Brick)
        {
            EmitSignal(SignalName.BrickRequested, tile.X, tile.Y);
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
        // Arrow hover (drives the grow/brighten animation in _Process).
        _eastHover = _eastArrow.Visible && Geometry2D.IsPointInPolygon(mousePos, _eastHit);
        _westHover = _westArrow.Visible && Geometry2D.IsPointInPolygon(mousePos, _westHit);

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
        else if (gs.Tool == GameState.ActiveTool.Brick && gs.Bricks > 0)
        {
            var t = gs.Tiles[tile.Y, tile.X];
            clickable = t == GameState.TileType.Soil || t == GameState.TileType.Bank;
        }
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
        _UpdateArrows();
    }

    private void _OnZoneChanged(int _zone)
    {
        _renderer.RefreshAllTiles();
        _renderer.BuildPreview();
        _UpdateArrows();
    }

    private void _OnRegionUnlocked(int _count)
    {
        _renderer.BuildPreview();
        _UpdateArrows();
    }

    // Clickable arrows built from the isometric basis so their edges run along the
    // tile diagonals (they look painted on the ground). The next map lies east
    // (+col = down-right on screen); the previous map is up-left. Anchored just
    // off the map's east/west edges by the river's entry/exit row.
    private void _BuildArrows()
    {
        var col = new Vector2(IsoMath.HalfWidth, IsoMath.HalfHeight);   // +col diagonal (down-right)
        _eastArrow = _BuildArrow(IsoMath.TileCenter(13, 6) + col * 1.4f, col, out _eastHit);
        _westArrow = _BuildArrow(IsoMath.TileCenter(0, 6) - col * 1.4f, -col, out _westHit);
    }

    // An arrowhead pointing along the iso `fwd` diagonal, with its base edge along
    // the other (row) diagonal so the whole shape sits flush with the grid.
    private Node2D _BuildArrow(Vector2 center, Vector2 fwd, out Vector2[] hit)
    {
        var row = new Vector2(-IsoMath.HalfWidth, IsoMath.HalfHeight); // cross diagonal
        var tip = fwd * 1.05f;
        var b1 = -fwd * 0.32f + row * 0.6f;
        var b2 = -fwd * 0.32f - row * 0.6f;
        hit = [center + tip, center + b1, center + b2];

        // Position the node AT the centre so Scale animates about it; children use
        // centre-relative points.
        var node = new Node2D { Position = center, Visible = false };
        AddChild(node);

        node.AddChild(new Polygon2D
        {
            Polygon = [tip + new Vector2(3, 5), b1 + new Vector2(3, 5), b2 + new Vector2(3, 5)],
            Color = new Color(0, 0, 0, 0.35f),
        });
        node.AddChild(new Polygon2D
        {
            Polygon = [tip, b1, b2],
            Color = new Color(0.93f, 0.78f, 0.28f),
        });
        node.AddChild(new Polygon2D // bevel highlight along the forward axis
        {
            Polygon = [tip, b1, Vector2.Zero],
            Color = new Color(1.0f, 0.92f, 0.55f),
        });
        node.AddChild(new Line2D
        {
            Points = [tip, b1, b2, tip],
            Width = 1.5f,
            DefaultColor = new Color(0.45f, 0.32f, 0.12f, 0.6f),
            JointMode = Line2D.LineJointMode.Round,
        });

        return node;
    }

    private void _UpdateArrows()
    {
        var gs = GameState.Instance;
        _eastArrow.Visible = gs.CurrentRegion + 1 < gs.UnlockedRegions;
        _westArrow.Visible = gs.CurrentRegion > 0;
    }

    public override void _Process(double delta)
    {
        float t = Mathf.Min((float)delta * 12f, 1f);
        _AnimateArrow(_eastArrow, _eastHover, t);
        _AnimateArrow(_westArrow, _westHover, t);
    }

    // Grow + brighten while hovered so it's obvious the arrow is clickable.
    private static void _AnimateArrow(Node2D arrow, bool hover, float t)
    {
        if (arrow == null || !arrow.Visible) return;
        arrow.Scale = arrow.Scale.Lerp(hover ? new Vector2(1.28f, 1.28f) : Vector2.One, t);
        arrow.Modulate = arrow.Modulate.Lerp(hover ? new Color(1.4f, 1.4f, 1.4f) : Colors.White, t);
    }

    private void _OnToolChanged(int _tool) { }
}
