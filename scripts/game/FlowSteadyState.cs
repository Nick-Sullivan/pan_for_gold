using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using static GameState.TileType;

public static class FlowSteadyState
{
    public static RiverDAG Calculate(TileCell[,] tiles, float[] entryFlowByRow = null)
    {
        var numCols = tiles.GetLength(0);
        var numRows = tiles.GetLength(1);
        var river = new RiverDAG();
        var riverSources = FindRiverSources(tiles);

        var nodeIdsToProcess = new List<(Vector2I id, float flow)>();
        foreach (var s in riverSources)
        {
            float flow = entryFlowByRow != null ? entryFlowByRow[s.Y] : 1000f;
            river.AddNode(s, new RiverNode(RiverSource, flow));
            nodeIdsToProcess.Add((s, flow));
        }

        while (nodeIdsToProcess.Count > 0)
        {
            var best = nodeIdsToProcess.MaxBy(e => e.flow);
            nodeIdsToProcess.Remove(best);
            var nodeId = best.id;

            var soilNeighbours = new List<Vector2I>();
            var villageNeighbours = new List<Vector2I>();
            var riverNeighbours = new List<Vector2I>();
            foreach (var nb in Neighbors(nodeId))
            {
                var isOutOfBounds = nb.X < 0 || nb.X >= numCols || nb.Y < 0 || nb.Y >= numRows;
                if (isOutOfBounds)
                {
                    continue;
                }
                else if (tiles[nb.X, nb.Y] == Soil || tiles[nb.X, nb.Y] == Bank)
                {
                    soilNeighbours.Add(nb);
                }
                else if (tiles[nb.X, nb.Y] == Village)
                {
                    villageNeighbours.Add(nb);
                }
                else if (tiles[nb.X, nb.Y] == River && !river.ContainsEdge(nb, nodeId))
                {
                    riverNeighbours.Add(nb);
                }
            }

            var node = river.GetNode(nodeId);
            var flowRemaining = 0f;
            var flowToGiveEachSoil = 0;
            if (node.Value.FlowRate <= GameState.MaxBankFlow * soilNeighbours.Count)
            {
                flowRemaining = 0;
                flowToGiveEachSoil = (int)(node.Value.FlowRate / soilNeighbours.Count);
            }
            else
            {
                flowRemaining = node.Value.FlowRate - GameState.MaxBankFlow * soilNeighbours.Count;
                flowToGiveEachSoil = (int)GameState.MaxBankFlow;
            }
            foreach (var soilNb in soilNeighbours)
            {
                if (!river.ContainsNode(soilNb))
                {
                    river.AddNode(soilNb, new RiverNode(tiles[soilNb.X, soilNb.Y].Type, 0));
                }
                var soilNode = river.GetNode(soilNb);
                soilNode.Value.FlowRate += flowToGiveEachSoil;
                river.AddEdge(nodeId, soilNb, new RiverEdge(flowToGiveEachSoil));
            }

            int flowToGiveEachVillage = villageNeighbours.Count > 0
                ? (int)Math.Min(GameState.VillageFlowThreshold, flowRemaining / villageNeighbours.Count)
                : 0;
            flowRemaining -= flowToGiveEachVillage * villageNeighbours.Count;
            foreach (var villageNb in villageNeighbours)
            {
                if (!river.ContainsNode(villageNb))
                    river.AddNode(villageNb, new RiverNode(Village, 0));
                var vNode = river.GetNode(villageNb);
                vNode.Value.FlowRate += flowToGiveEachVillage;
                river.AddEdge(nodeId, villageNb, new RiverEdge(flowToGiveEachVillage));
            }

            var flowToGiveEachRiver = (int)(flowRemaining / riverNeighbours.Count);
            foreach (var riverNb in riverNeighbours)
            {
                if (!river.ContainsNode(riverNb))
                {
                    river.AddNode(riverNb, new RiverNode(tiles[riverNb.X, riverNb.Y].Type, 0));
                }
                var riverNode = river.GetNode(riverNb);
                riverNode.Value.FlowRate += flowToGiveEachRiver;
                river.AddEdge(nodeId, riverNb, new RiverEdge(flowToGiveEachRiver));
                nodeIdsToProcess.RemoveAll(e => e.id == riverNb);
                nodeIdsToProcess.Add((riverNb, riverNode.Value.FlowRate));
            }
        }

        // Any remaining nodes that haven't been processed get 0
        for (int col = 0; col < numCols; col++)
        {
            for (int row = 0; row < numRows; row++)
            {
                var pos = new Vector2I(col, row);
                if (!river.ContainsNode(pos))
                {
                    river.AddNode(pos, new RiverNode(tiles[col, row].Type, 0));
                }
            }
        }

        return river;
    }

    private static List<Vector2I> FindRiverSources(TileCell[,] tiles)
    {
        var sources = new List<Vector2I>();
        var numCols = tiles.GetLength(0);
        var numRows = tiles.GetLength(1);
        for (int col = 0; col < numCols; col++)
        {
            for (int row = 0; row < numRows; row++)
            {
                if (tiles[col, row] == RiverSource)
                {
                    sources.Add(new Vector2I(col, row));
                }
            }
        }
        return sources;
    }

    private static IEnumerable<Vector2I> Neighbors(Vector2I pos)
    {
        yield return new Vector2I(pos.X + 1, pos.Y);
        yield return new Vector2I(pos.X - 1, pos.Y);
        yield return new Vector2I(pos.X, pos.Y + 1);
        yield return new Vector2I(pos.X, pos.Y - 1);
    }
}
