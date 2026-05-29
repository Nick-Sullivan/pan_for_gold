using Godot;
using static GameState.TileType;

namespace pan_for_gold.Tests;

public static class TestTiles
{
    public static Vector2I V(int col, int row) => new(col, row);
    public static TileCell S() => new(Soil);
    public static TileCell R() => new(River);
    public static TileCell RS() => new(RiverSource);
    public static TileCell B() => new(Bank);

    // Write rows visually left-to-right; stores as [col, row] so X=col, Y=row
    public static TileCell[,] Grid(params TileCell[][] rows)
    {
        int numRows = rows.Length, numCols = rows[0].Length;
        var t = new TileCell[numCols, numRows];
        for (int row = 0; row < numRows; row++)
        {
            for (int col = 0; col < numCols; col++)
            {
                t[col, row] = rows[row][col];
            }
        }
        return t;
    }

    public static void AssertDAGEqual(RiverDAG expected, RiverDAG actual)
    {
        var lines = new List<string>();

        var expectedNodeIds = expected.NodeIds.ToHashSet();
        var actualNodeIds = actual.NodeIds.ToHashSet();

        foreach (var id in expectedNodeIds.Except(actualNodeIds).OrderBy(v => v.ToString()))
            lines.Add($"  - node {id}: missing (expected {expected.GetNode(id).Value})");

        foreach (var id in actualNodeIds.Except(expectedNodeIds).OrderBy(v => v.ToString()))
            lines.Add($"  + node {id}: unexpected ({actual.GetNode(id).Value})");

        foreach (var id in expectedNodeIds.Intersect(actualNodeIds).OrderBy(v => v.ToString()))
        {
            var exp = expected.GetNode(id).Value;
            var act = actual.GetNode(id).Value;
            if (!exp.Equals(act))
                lines.Add($"  ~ node {id}: {exp} → {act}");
        }

        var expectedEdges = expected.NodeIds
            .SelectMany(id => expected.GetChildEdges(id).Select(e => (e.Source, e.Destination, e.Value)))
            .ToDictionary(e => (e.Source, e.Destination), e => e.Value);

        var actualEdges = actual.NodeIds
            .SelectMany(id => actual.GetChildEdges(id).Select(e => (e.Source, e.Destination, e.Value)))
            .ToDictionary(e => (e.Source, e.Destination), e => e.Value);

        foreach (var key in expectedEdges.Keys.Except(actualEdges.Keys).OrderBy(k => k.ToString()))
            lines.Add($"  - edge {key.Source}→{key.Destination}: missing (expected {expectedEdges[key]})");

        foreach (var key in actualEdges.Keys.Except(expectedEdges.Keys).OrderBy(k => k.ToString()))
            lines.Add($"  + edge {key.Source}→{key.Destination}: unexpected ({actualEdges[key]})");

        foreach (var key in expectedEdges.Keys.Intersect(actualEdges.Keys).OrderBy(k => k.ToString()))
        {
            var exp = expectedEdges[key];
            var act = actualEdges[key];
            if (!exp.Equals(act))
                lines.Add($"  ~ edge {key.Source}→{key.Destination}: {exp} → {act}");
        }

        if (lines.Count > 0)
            Assert.Fail("RiverDAG diff:\n" + string.Join("\n", lines));
    }
}
