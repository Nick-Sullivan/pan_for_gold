using Godot;
using System.Collections.Generic;

public partial class GameState : Node
{
    public enum TileType { Soil = 0, Bank = 1, River = 2, Channel = 3, Stone = 4, RiverSource = 5, Village = 6, Gate = 7, GoldSource = 8, ClaySource = 9 }
    public enum ActiveTool { Pan = 0, Shovel = 1 }

    public const int Cols = 14;
    public const int Rows = 14;
    public const float MaxTileGold = 10.0f;
    public const float RefillTime = 15.0f;
    public const int ShovelCost = 10;
    public const float FillDelayPerStep = 0.25f;
    public const int BaseSpeedTiles = 4;
    public const float MaxBankFlow = 10f;
    public const float VillageFlowThreshold = 100f;
    public const float MaxTileClay = 10.0f;

    public static GameState Instance { get; private set; }

    // Economy
    public int Gold { get; internal set; }
    public int Clay { get; internal set; }
    public int Shovels { get; internal set; }
    public ActiveTool Tool { get; internal set; } = ActiveTool.Pan;
    public float RiverSpeed { get; internal set; } = 1.0f;

    // Grid data [row, col]
    public TileType[,] Tiles { get; private set; } = new TileType[Rows, Cols];
    public float[,] TileGold { get; private set; } = new float[Rows, Cols];
    public float[,] TileClay { get; private set; } = new float[Rows, Cols];
    public float[,] TileFlowValues { get; private set; } = new float[Rows, Cols];
    public Vector2[,] TileFlowDir { get; private set; } = new Vector2[Rows, Cols];
    public int[,] TileBfsDepth { get; private set; } = new int[Rows, Cols];
    public List<Vector2I>[,] TileFlowParent { get; private set; }

    // Region / zone data
    public int CurrentZone { get; internal set; } = 0;

    private readonly List<RegionSnapshot>[] _zoneData = [new(), new()];
    private readonly int[] _currentRegion   = [0, 0];
    private readonly int[] _unlockedRegions = [1, 1];

    public int CurrentRegion
    {
        get => _currentRegion[CurrentZone];
        internal set => _currentRegion[CurrentZone] = value;
    }
    public int UnlockedRegions
    {
        get => _unlockedRegions[CurrentZone];
        internal set => _unlockedRegions[CurrentZone] = value;
    }
    internal List<RegionSnapshot> RegionData => _zoneData[CurrentZone];
    internal List<RegionSnapshot> GetZoneData(int zone) => _zoneData[zone];

    internal record RegionSnapshot(TileType[,] Tiles, float[,] Gold, float[,] Clay, float[,] Flow);

    [Signal] public delegate void TileChangedEventHandler(int col, int row);
    [Signal] public delegate void GoldChangedEventHandler(int newValue);
    [Signal] public delegate void ClayChangedEventHandler(int newValue);
    [Signal] public delegate void TileClayChangedEventHandler(int col, int row, int amount);
    [Signal] public delegate void TileGoldChangedEventHandler(int col, int row, int amount);
    [Signal] public delegate void ShovelsChangedEventHandler(int newValue);
    [Signal] public delegate void ToolChangedEventHandler(int tool);
    [Signal] public delegate void ZoneChangedEventHandler(int zone);
    [Signal] public delegate void RegionUnlockedEventHandler(int count);
    [Signal] public delegate void RegionSwitchedEventHandler(int index);
    [Signal] public delegate void SpeedChangedEventHandler(float value);
    [Signal] public delegate void FlowChangedEventHandler();
    [Signal] public delegate void QuestChangedEventHandler(int index);

    // Quests
    public bool[] QuestsComplete { get; private set; } = new bool[2];

    public override void _Ready()
    {
        Instance = this;
        TileFlowParent = InitFlowParent();
        GD.Print("GameState initialized");
    }

    public TileType[,] GetRegionTiles(int index)
        => index >= 0 && index < RegionData.Count ? RegionData[index].Tiles : null;

    public float[,] GetRegionGold(int index)
        => index >= 0 && index < RegionData.Count ? RegionData[index].Gold : null;

    // Called by RegionSystem to swap the active tile/gold arrays
    internal void SwapActiveTo(int index)
    {
        var snap = RegionData[index];
        Tiles          = snap.Tiles;
        TileGold       = snap.Gold;
        TileClay       = snap.Clay;
        TileFlowValues = snap.Flow;
    }

    private static List<Vector2I>[,] InitFlowParent()
    {
        var arr = new List<Vector2I>[Rows, Cols];
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                arr[r, c] = [];
        return arr;
    }
}
