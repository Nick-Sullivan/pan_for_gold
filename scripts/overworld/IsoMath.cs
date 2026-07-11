using Godot;
using System;

public static class IsoMath
{
    public const int HalfWidth = 44;
    public const int HalfHeight = 25;
    public const int OriginX = 640;
    public const int OriginY = 16;

    public static Vector2 TileTop(int col, int row)
        => new(OriginX + (col - row) * HalfWidth, OriginY + (col + row) * HalfHeight);

    public static Vector2 TileCenter(int col, int row)
        => TileTop(col, row) + new Vector2(0, HalfHeight);

    public static Vector2[] DiamondVerts(int col, int row)
    {
        var t = TileTop(col, row);
        return [
            t,
            t + new Vector2( HalfWidth,  HalfHeight),
            t + new Vector2( 0,          HalfHeight * 2),
            t + new Vector2(-HalfWidth,  HalfHeight),
        ];
    }

    public static Vector2I ScreenToTile(Vector2 mousePos)
    {
        float sx = mousePos.X - OriginX;
        float sy = mousePos.Y - OriginY;
        return new Vector2I(
            (int)Math.Floor((sx / HalfWidth + sy / HalfHeight) / 2.0),
            (int)Math.Floor((sy / HalfHeight - sx / HalfWidth) / 2.0)
        );
    }

    public static Color SoilTint(int col, int row, GameState.TileType[,] tiles, float[,]? flowValues = null)
        => RiverDistance(col, row, tiles, flowValues) switch
        {
            1 => new Color(0.35f, 0.50f, 0.20f),
            2 => new Color(0.22f, 0.32f, 0.15f),
            _ => new Color(0.32f, 0.22f, 0.12f),
        };

    public static Color WallColor(GameState.TileType tileType, int col, int row, GameState.TileType[,] tiles, float[,]? flowValues = null)
    {
        if (tileType == GameState.TileType.Stone)
            return new Color(0, 0, 0, 0);
        var base_ = tileType switch
        {
            GameState.TileType.Bank => new Color(0.62f, 0.52f, 0.36f),
            GameState.TileType.River or GameState.TileType.RiverSource => new Color(0.12f, 0.42f, 0.80f),
            GameState.TileType.Village => VillageDefs.ActiveColor(),
            GameState.TileType.Gate => new Color(0.55f, 0.20f, 0.20f),
            GameState.TileType.GoldSource => new Color(0.90f, 0.72f, 0.10f),
            GameState.TileType.ClaySource => new Color(0.68f, 0.32f, 0.18f),
            GameState.TileType.Brick => new Color(0.70f, 0.35f, 0.20f),
            GameState.TileType.Furnace => new Color(0.38f, 0.30f, 0.28f),
            GameState.TileType.ShovelRental => new Color(0.30f, 0.45f, 0.55f),
            _ => SoilTint(col, row, tiles, flowValues),
        };
        return new Color(base_.R * 0.80f, base_.G * 0.80f, base_.B * 0.80f);
    }

    public static Color PreviewColor(GameState.TileType tileType)
        => tileType switch
        {
            GameState.TileType.Bank => new Color(0.62f, 0.52f, 0.36f, 0.35f),
            GameState.TileType.River or GameState.TileType.RiverSource => new Color(0.12f, 0.42f, 0.80f, 0.35f),
            GameState.TileType.Stone => new Color(0.38f, 0.36f, 0.34f, 0.35f),
            GameState.TileType.Village => new Color(VillageDefs.ActiveColor(), 0.35f),
            GameState.TileType.Gate => new Color(0.55f, 0.20f, 0.20f, 0.35f),
            GameState.TileType.GoldSource => new Color(0.90f, 0.72f, 0.10f, 0.35f),
            GameState.TileType.ClaySource => new Color(0.68f, 0.32f, 0.18f, 0.35f),
            GameState.TileType.Brick => new Color(0.70f, 0.35f, 0.20f, 0.35f),
            GameState.TileType.Furnace => new Color(0.85f, 0.45f, 0.18f, 0.35f),
            GameState.TileType.ShovelRental => new Color(0.30f, 0.45f, 0.55f, 0.35f),
            _ => new Color(0.32f, 0.22f, 0.12f, 0.35f),
        };

    public static Color PreviewWallColor(GameState.TileType tileType)
    {
        var c = PreviewColor(tileType);
        return new Color(c.R * 0.80f, c.G * 0.80f, c.B * 0.80f, c.A);
    }

    private static int RiverDistance(int col, int row, GameState.TileType[,] tiles, float[,]? flowValues)
    {
        int rows = tiles.GetLength(0);
        int cols = tiles.GetLength(1);

        foreach (var (nc, nr) in new[] { (col + 1, row), (col - 1, row), (col, row + 1), (col, row - 1) })
        {
            if (nc < 0 || nc >= cols || nr < 0 || nr >= rows) continue;
            if (IsFlowingRiver(tiles[nr, nc], flowValues?[nr, nc] ?? 1f)) return 1;
        }

        for (int dc = -2; dc <= 2; dc++)
            for (int dr = -2; dr <= 2; dr++)
            {
                if (Math.Abs(dc) + Math.Abs(dr) != 2) continue;
                int nc = col + dc, nr = row + dr;
                if (nc < 0 || nc >= cols || nr < 0 || nr >= rows) continue;
                if (IsFlowingRiver(tiles[nr, nc], flowValues?[nr, nc] ?? 1f)) return 2;
            }

        return 99;
    }

    private static bool IsFlowingRiver(GameState.TileType t, float flow) =>
        (t == GameState.TileType.River || t == GameState.TileType.RiverSource) && flow > 0;
}
