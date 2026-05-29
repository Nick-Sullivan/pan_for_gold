using Godot;

namespace pan_for_gold.Tests;

public class IsoMathTests
{
    [Fact]
    public void TileTop_OriginTile_IsAtOrigin()
    {
        var top = IsoMath.TileTop(0, 0);
        Assert.Equal(new Vector2(IsoMath.OriginX, IsoMath.OriginY), top);
    }

    [Fact]
    public void TileTop_MoveRight_ShiftsRightAndDown()
    {
        var a = IsoMath.TileTop(0, 0);
        var b = IsoMath.TileTop(1, 0);
        Assert.Equal(a.X + IsoMath.HalfWidth, b.X);
        Assert.Equal(a.Y + IsoMath.HalfHeight, b.Y);
    }

    [Fact]
    public void TileTop_MoveDown_ShiftsLeftAndDown()
    {
        var a = IsoMath.TileTop(0, 0);
        var b = IsoMath.TileTop(0, 1);
        Assert.Equal(a.X - IsoMath.HalfWidth, b.X);
        Assert.Equal(a.Y + IsoMath.HalfHeight, b.Y);
    }

    [Fact]
    public void TileCenter_IsHalfHeightBelowTop()
    {
        var top    = IsoMath.TileTop(3, 5);
        var center = IsoMath.TileCenter(3, 5);
        Assert.Equal(top.X, center.X);
        Assert.Equal(top.Y + IsoMath.HalfHeight, center.Y);
    }

    [Fact]
    public void DiamondVerts_ReturnsFourPoints()
    {
        Assert.Equal(4, IsoMath.DiamondVerts(0, 0).Length);
    }

    [Fact]
    public void DiamondVerts_TopVertIsAtTileTop()
    {
        var top   = IsoMath.TileTop(2, 3);
        var verts = IsoMath.DiamondVerts(2, 3);
        Assert.Equal(top, verts[0]);
    }

    [Fact]
    public void DiamondVerts_IsSymmetricAroundCenter()
    {
        var verts  = IsoMath.DiamondVerts(4, 4);
        var center = IsoMath.TileCenter(4, 4);
        // Top and bottom are symmetric vertically around center
        Assert.Equal(center.Y - IsoMath.HalfHeight, verts[0].Y); // top
        Assert.Equal(center.Y + IsoMath.HalfHeight, verts[2].Y); // bottom
    }

    [Fact]
    public void ScreenToTile_TileCenterRoundsToTile()
    {
        for (int col = 0; col < 5; col++)
        for (int row = 0; row < 5; row++)
        {
            var center = IsoMath.TileCenter(col, row);
            Assert.Equal(new Vector2I(col, row), IsoMath.ScreenToTile(center));
        }
    }

    [Fact]
    public void SoilTint_NoRiverNearby_ReturnsBrown()
    {
        var tiles = new GameState.TileType[5, 5];
        Assert.Equal(new Color(0.32f, 0.22f, 0.12f), IsoMath.SoilTint(2, 2, tiles));
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(3, 2)]
    [InlineData(2, 1)]
    [InlineData(2, 3)]
    public void SoilTint_RiverAdjacentInAnyDirection_ReturnsLightGreen(int rc, int rr)
    {
        var tiles = new GameState.TileType[5, 5];
        tiles[rr, rc] = GameState.TileType.River;
        Assert.Equal(new Color(0.35f, 0.50f, 0.20f), IsoMath.SoilTint(2, 2, tiles));
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(4, 2)]
    [InlineData(2, 0)]
    [InlineData(2, 4)]
    [InlineData(1, 1)]
    [InlineData(3, 1)]
    [InlineData(1, 3)]
    [InlineData(3, 3)]
    public void SoilTint_RiverAtManhattanDistance2_ReturnsDarkGreen(int rc, int rr)
    {
        var tiles = new GameState.TileType[5, 5];
        tiles[rr, rc] = GameState.TileType.River;
        Assert.Equal(new Color(0.22f, 0.32f, 0.15f), IsoMath.SoilTint(2, 2, tiles));
    }

    [Fact]
    public void SoilTint_RiverAtEdgeOfGrid_DoesNotThrow()
    {
        var tiles = new GameState.TileType[3, 3];
        tiles[0, 1] = GameState.TileType.River;
        var ex = Record.Exception(() => IsoMath.SoilTint(0, 0, tiles));
        Assert.Null(ex);
    }

    [Fact]
    public void SoilTint_AdjacentTakesPriorityOverTwoAway()
    {
        var tiles = new GameState.TileType[5, 5];
        tiles[2, 3] = GameState.TileType.River;
        tiles[2, 4] = GameState.TileType.River;
        Assert.Equal(new Color(0.35f, 0.50f, 0.20f), IsoMath.SoilTint(2, 2, tiles));
    }

    [Fact]
    public void WallColor_Stone_IsTransparent()
    {
        var tiles = new GameState.TileType[5, 5];
        var color = IsoMath.WallColor(GameState.TileType.Stone, 0, 0, tiles);
        Assert.Equal(new Color(0, 0, 0, 0), color);
    }

    [Fact]
    public void WallColor_IsDarkerThanFaceColor()
    {
        var tiles = new GameState.TileType[5, 5];
        var wall  = IsoMath.WallColor(GameState.TileType.River, 0, 0, tiles);
        // River face is (0.12, 0.42, 0.80); wall should be 80% of that
        Assert.Equal(0.12f * 0.80f, wall.R, 4);
        Assert.Equal(0.42f * 0.80f, wall.G, 4);
        Assert.Equal(0.80f * 0.80f, wall.B, 4);
    }

    [Fact]
    public void PreviewColor_HasReducedAlpha()
    {
        foreach (var t in Enum.GetValues<GameState.TileType>())
            Assert.Equal(0.35f, IsoMath.PreviewColor(t).A, 4);
    }

    [Fact]
    public void PreviewWallColor_IsDarkerThanPreviewFace()
    {
        var face = IsoMath.PreviewColor(GameState.TileType.Bank);
        var wall = IsoMath.PreviewWallColor(GameState.TileType.Bank);
        Assert.Equal(face.R * 0.80f, wall.R, 4);
        Assert.Equal(face.G * 0.80f, wall.G, 4);
        Assert.Equal(face.B * 0.80f, wall.B, 4);
        Assert.Equal(face.A, wall.A, 4);
    }
}
