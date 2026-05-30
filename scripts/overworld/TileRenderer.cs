using Godot;
using System;
using System.Collections.Generic;

public partial class TileRenderer : Node2D
{
    private static readonly Shader WaterShader = GD.Load<Shader>("res://assets/shaders/tile_water.gdshader");
    private static readonly Shader ChannelShader = GD.Load<Shader>("res://assets/shaders/tile_channel.gdshader");
    private static readonly Shader BankShader = GD.Load<Shader>("res://assets/shaders/tile_bank.gdshader");
    private static readonly Shader SoilShader = GD.Load<Shader>("res://assets/shaders/tile_soil.gdshader");

    private static readonly Vector2[] DiamondUv = [
        new(0.5f, 0.0f),
        new(1.0f, 0.5f),
        new(0.5f, 1.0f),
        new(0.0f, 0.5f),
    ];

    private const int PreviewCols = 1;
    private const int WallH = 12;

    private Polygon2D[,] _tileNodes;
    private Label[,] _tileLabels;
    private readonly List<Polygon2D> _seWall = [];
    private readonly List<Polygon2D> _swWall = [];
    private readonly List<Node2D> _previewNodes = [];

    public void Build()
    {
        _tileNodes = new Polygon2D[GameState.Rows, GameState.Cols];
        _tileLabels = new Label[GameState.Rows, GameState.Cols];
        BuildWall();
        BuildTiles();
    }

    private void BuildWall()
    {
        var gs = GameState.Instance;
        for (int col = 0; col < GameState.Cols; col++)
        {
            int row = GameState.Rows - 1;
            var t = IsoMath.TileTop(col, row);
            var left = t + new Vector2(-IsoMath.HalfWidth, IsoMath.HalfHeight);
            var bottom = t + new Vector2(0, IsoMath.HalfHeight * 2);
            var poly = new Polygon2D();
            poly.Polygon = [left, bottom, bottom + new Vector2(0, WallH), left + new Vector2(0, WallH)];
            poly.Color = IsoMath.WallColor(gs.Tiles[row, col], col, row, gs.Tiles, gs.TileFlowValues);
            AddChild(poly);
            _seWall.Add(poly);
        }
        for (int row = 0; row < GameState.Rows; row++)
        {
            int col = GameState.Cols - 1;
            var t = IsoMath.TileTop(col, row);
            var right = t + new Vector2(IsoMath.HalfWidth, IsoMath.HalfHeight);
            var bottom = t + new Vector2(0, IsoMath.HalfHeight * 2);
            var poly = new Polygon2D();
            poly.Polygon = [right, bottom, bottom + new Vector2(0, WallH), right + new Vector2(0, WallH)];
            poly.Color = IsoMath.WallColor(gs.Tiles[row, col], col, row, gs.Tiles, gs.TileFlowValues);
            AddChild(poly);
            _swWall.Add(poly);
        }
    }

    private void BuildTiles()
    {
        var gs = GameState.Instance;
        for (int row = 0; row < GameState.Rows; row++)
            for (int col = 0; col < GameState.Cols; col++)
            {
                var tileType = gs.Tiles[row, col];
                int goldAmount = (int)gs.TileGold[row, col];

                var poly = new Polygon2D();
                poly.Polygon = IsoMath.DiamondVerts(col, row);
                poly.Set("uv", DiamondUv);
                var mat = new ShaderMaterial { Shader = ShaderFor(tileType) };
                ApplyParams(mat, tileType, goldAmount, col, row);
                poly.Material = mat;
                AddChild(poly);

                var center = IsoMath.TileCenter(col, row);
                var label = new Label();
                label.Position = center - new Vector2(30, 10);
                label.Size = new Vector2(60, 20);
                label.HorizontalAlignment = HorizontalAlignment.Center;
                label.VerticalAlignment = VerticalAlignment.Center;
                label.AddThemeFontSizeOverride("font_size", 10);
                label.Visible = tileType is GameState.TileType.Bank or GameState.TileType.River
                    or GameState.TileType.RiverSource or GameState.TileType.Village;
                label.Text = tileType == GameState.TileType.Bank
                    ? goldAmount.ToString()
                    : gs.TileFlowValues[row, col].ToString();
                AddChild(label);

                _tileNodes[row, col] = poly;
                _tileLabels[row, col] = label;
            }
    }

    public void RefreshTile(int col, int row)
    {
        var gs = GameState.Instance;
        var tileType = gs.Tiles[row, col];
        int goldAmount = (int)gs.TileGold[row, col];
        var poly = _tileNodes[row, col];
        var mat = (ShaderMaterial)poly.Material;
        mat.Shader = ShaderFor(tileType);
        ApplyParams(mat, tileType, goldAmount, col, row);
        var center = IsoMath.TileCenter(col, row);
        _tileLabels[row, col].Position = center - new Vector2(30, 10);
        _tileLabels[row, col].Visible = tileType is GameState.TileType.Bank
            or GameState.TileType.River or GameState.TileType.RiverSource;
        _tileLabels[row, col].Text = tileType == GameState.TileType.Bank
            ? goldAmount.ToString()
            : gs.TileFlowValues[row, col].ToString();
    }

    public void RefreshTileAndNeighbors(int col, int row)
    {
        var gs = GameState.Instance;
        RefreshTile(col, row);
        for (int dc = -2; dc <= 2; dc++)
            for (int dr = -2; dr <= 2; dr++)
            {
                if (Math.Abs(dc) + Math.Abs(dr) > 2 || (dc == 0 && dr == 0)) continue;
                int nx = col + dc, ny = row + dr;
                if (nx < 0 || nx >= GameState.Cols || ny < 0 || ny >= GameState.Rows) continue;
                switch (gs.Tiles[ny, nx])
                {
                    case GameState.TileType.Soil:
                        ((ShaderMaterial)_tileNodes[ny, nx].Material)
                            .SetShaderParameter("tint", IsoMath.SoilTint(nx, ny, gs.Tiles, gs.TileFlowValues));
                        break;
                    case GameState.TileType.River when Math.Abs(dc) + Math.Abs(dr) == 1:
                        RefreshTile(nx, ny);
                        break;
                }
            }
    }

    public void RefreshWall(int col, int row)
    {
        var gs = GameState.Instance;
        if (row == GameState.Rows - 1)
            _seWall[col].Color = IsoMath.WallColor(gs.Tiles[row, col], col, row, gs.Tiles, gs.TileFlowValues);
        if (col == GameState.Cols - 1)
            _swWall[row].Color = IsoMath.WallColor(gs.Tiles[row, col], col, row, gs.Tiles, gs.TileFlowValues);
    }

    public void RefreshGold(int col, int row, int amount)
    {
        ((ShaderMaterial)_tileNodes[row, col].Material)
            .SetShaderParameter("gold_ratio", (float)amount / GameState.MaxTileGold);
        _tileLabels[row, col].Text = amount.ToString();
    }

    public void RefreshClay(int col, int row, int amount) { }

    public void RefreshAllTiles()
    {
        for (int row = 0; row < GameState.Rows; row++)
            for (int col = 0; col < GameState.Cols; col++)
            {
                RefreshTile(col, row);
                RefreshWall(col, row);
            }
    }

    public void RefreshAllFlow()
    {
        var gs = GameState.Instance;
        for (int row = 0; row < GameState.Rows; row++)
            for (int col = 0; col < GameState.Cols; col++)
            {
                var tileType = gs.Tiles[row, col];
                var mat = (ShaderMaterial)_tileNodes[row, col].Material;
                if (IsWater(tileType))
                {
                    var conn = RiverConnectivity(col, row);
                    mat.SetShaderParameter("flow_speed", gs.TileFlowValues[row, col] / 1000.0f);
                    mat.SetShaderParameter("flow_dir", gs.TileFlowDir[row, col]);
                    mat.SetShaderParameter("bfs_depth", gs.TileBfsDepth[row, col]);
                    mat.SetShaderParameter("north", conn.north);
                    mat.SetShaderParameter("south", conn.south);
                    mat.SetShaderParameter("east", conn.east);
                    mat.SetShaderParameter("west", conn.west);
                    mat.SetShaderParameter("soil_tint", IsoMath.SoilTint(col, row, gs.Tiles, gs.TileFlowValues));
                    _tileLabels[row, col].Text = gs.TileFlowValues[row, col].ToString();
                }
                else if (tileType == GameState.TileType.Soil)
                {
                    mat.SetShaderParameter("tint", IsoMath.SoilTint(col, row, gs.Tiles, gs.TileFlowValues));
                }
            }
    }

    public void BuildPreview()
    {
        foreach (var node in _previewNodes) node.QueueFree();
        _previewNodes.Clear();

        var gs = GameState.Instance;
        var nextTiles = gs.GetRegionTiles(gs.CurrentRegion + 1);
        if (nextTiles != null)
        {
            for (int row = 0; row < GameState.Rows; row++)
                for (int dc = 0; dc < PreviewCols; dc++)
                {
                    var tileType = (GameState.TileType)nextTiles[row, dc];
                    var poly = MakePreviewPoly(GameState.Cols + dc, row, tileType);
                    if (dc == PreviewCols - 1)
                        AddPreviewWalls(GameState.Cols + dc, row, tileType, showSw: true);
                }
        }

        var prevTiles = gs.GetRegionTiles(gs.CurrentRegion - 1);
        if (prevTiles != null)
        {
            for (int row = 0; row < GameState.Rows; row++)
                for (int dc = 0; dc < PreviewCols; dc++)
                {
                    int srcCol = GameState.Cols - PreviewCols + dc;
                    var tileType = (GameState.TileType)prevTiles[row, srcCol];
                    MakePreviewPoly(-PreviewCols + dc, row, tileType);
                    if (dc == 0)
                        AddPreviewWalls(-PreviewCols + dc, row, tileType, showSw: true);
                }
        }
        else
        {
            for (int row = 0; row < GameState.Rows; row++)
            {
                var tileType = gs.Tiles[row, 0] == GameState.TileType.River
                    ? GameState.TileType.River
                    : GameState.TileType.Soil;
                MakePreviewPoly(-1, row, tileType);
                AddPreviewWalls(-1, row, tileType, showSw: true);
            }
        }
    }

    public Polygon2D GetTileNode(int col, int row) => _tileNodes[row, col];

    private Polygon2D MakePreviewPoly(int col, int row, GameState.TileType tileType)
    {
        var poly = new Polygon2D();
        poly.Polygon = IsoMath.DiamondVerts(col, row);
        poly.Color = IsoMath.PreviewColor(tileType);
        AddChild(poly);
        _previewNodes.Add(poly);
        return poly;
    }

    private void AddPreviewWalls(int col, int row, GameState.TileType tileType, bool showSw)
    {
        var t = IsoMath.TileTop(col, row);
        var wc = IsoMath.PreviewWallColor(tileType);
        if (row == GameState.Rows - 1)
        {
            var left = t + new Vector2(-IsoMath.HalfWidth, IsoMath.HalfHeight);
            var bottom = t + new Vector2(0, IsoMath.HalfHeight * 2);
            var se = new Polygon2D();
            se.Polygon = [left, bottom, bottom + new Vector2(0, WallH), left + new Vector2(0, WallH)];
            se.Color = wc;
            AddChild(se);
            _previewNodes.Add(se);
        }
        if (showSw)
        {
            var right = t + new Vector2(IsoMath.HalfWidth, IsoMath.HalfHeight);
            var bottom = t + new Vector2(0, IsoMath.HalfHeight * 2);
            var sw = new Polygon2D();
            sw.Polygon = [right, bottom, bottom + new Vector2(0, WallH), right + new Vector2(0, WallH)];
            sw.Color = wc;
            AddChild(sw);
            _previewNodes.Add(sw);
        }
    }

    private static Shader ShaderFor(GameState.TileType tileType) => tileType switch
    {
        GameState.TileType.River or GameState.TileType.RiverSource => WaterShader,
        GameState.TileType.Bank => BankShader,
        _ => SoilShader,
    };

    private (float north, float south, float east, float west) RiverConnectivity(int col, int row)
    {
        var tiles = GameState.Instance.Tiles;
        return (
            north: row > 0 && IsWater(tiles[row - 1, col]) ? 1f : 0f,
            south: row < GameState.Rows - 1 && IsWater(tiles[row + 1, col]) ? 1f : 0f,
            east: col < GameState.Cols - 1 && IsWater(tiles[row, col + 1]) ? 1f : 0f,
            west: col > 0 && IsWater(tiles[row, col - 1]) ? 1f : 0f
        );
    }

    private static bool IsWater(GameState.TileType t) =>
        t == GameState.TileType.River || t == GameState.TileType.RiverSource;

    private void ApplyParams(ShaderMaterial mat, GameState.TileType tileType, int goldAmount, int col, int row)
    {
        var gs = GameState.Instance;
        switch (tileType)
        {
            case GameState.TileType.Bank:
                mat.SetShaderParameter("gold_ratio", (float)goldAmount / GameState.MaxTileGold);
                break;
            case GameState.TileType.GoldSource:
                mat.SetShaderParameter("tint", new Color(0.90f, 0.72f, 0.10f));
                break;
            case GameState.TileType.ClaySource:
                mat.SetShaderParameter("tint", new Color(0.68f, 0.32f, 0.18f));
                break;
            case GameState.TileType.River:
            case GameState.TileType.RiverSource:
                var conn = RiverConnectivity(col, row);
                mat.SetShaderParameter("flow_speed", gs.TileFlowValues[row, col] / 1000.0f);
                mat.SetShaderParameter("flow_dir", gs.TileFlowDir[row, col]);
                mat.SetShaderParameter("bfs_depth", gs.TileBfsDepth[row, col]);
                mat.SetShaderParameter("north", conn.north);
                mat.SetShaderParameter("south", conn.south);
                mat.SetShaderParameter("east", conn.east);
                mat.SetShaderParameter("west", conn.west);
                mat.SetShaderParameter("soil_tint", IsoMath.SoilTint(col, row, gs.Tiles, gs.TileFlowValues));
                break;
            case GameState.TileType.Soil:
                mat.SetShaderParameter("tint", IsoMath.SoilTint(col, row, gs.Tiles, gs.TileFlowValues));
                break;
            case GameState.TileType.Stone:
                mat.SetShaderParameter("tint", new Color(0.55f, 0.52f, 0.50f));
                break;
            case GameState.TileType.Village:
                mat.SetShaderParameter("tint", new Color(0.85f, 0.65f, 0.15f));
                break;
            case GameState.TileType.Gate:
                mat.SetShaderParameter("tint", new Color(0.55f, 0.20f, 0.20f));
                break;
            case GameState.TileType.Brick:
                mat.SetShaderParameter("tint", new Color(0.70f, 0.35f, 0.20f));
                break;
        }
    }
}
