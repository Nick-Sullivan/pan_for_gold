using Godot;
using System.Collections.Generic;
using System.Text.Json;

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

    // ------------------------------------------------------------------
    // New game / reset
    // ------------------------------------------------------------------

    // Reset all mutable state to a fresh game. Tile/region arrays are
    // repopulated by WaterPropagation.InitTiles immediately afterwards
    // (see GameRunner.StartNewGame), so here we just clear the bookkeeping.
    public void ResetToNewGame()
    {
        Gold = 0;
        Clay = 0;
        Shovels = 0;
        Tool = ActiveTool.Pan;
        RiverSpeed = 1.0f;
        CurrentZone = 0;
        _currentRegion[0] = 0;
        _currentRegion[1] = 0;
        _unlockedRegions[0] = 1;
        _unlockedRegions[1] = 1;
        _zoneData[0].Clear();
        _zoneData[1].Clear();
        System.Array.Clear(QuestsComplete);
        TileFlowParent = InitFlowParent();
    }

    // ------------------------------------------------------------------
    // Serialization — quick state recreation and the save/load foundation
    // ------------------------------------------------------------------

    public sealed class RegionDto
    {
        public int[][] Tiles { get; set; }
        public float[][] Gold { get; set; }
        public float[][] Clay { get; set; }
        public float[][] Flow { get; set; }
    }

    public sealed class ZoneDto
    {
        public int CurrentRegion { get; set; }
        public int Unlocked { get; set; }
        public List<RegionDto> Regions { get; set; } = [];
    }

    public sealed class Snapshot
    {
        public int Gold { get; set; }
        public int Clay { get; set; }
        public int Shovels { get; set; }
        public int Tool { get; set; }
        public float RiverSpeed { get; set; }
        public int CurrentZone { get; set; }
        public bool[] Quests { get; set; }
        public ZoneDto[] Zones { get; set; }
    }

    public Snapshot ToSnapshot()
    {
        var zones = new ZoneDto[_zoneData.Length];
        for (int z = 0; z < _zoneData.Length; z++)
        {
            var dto = new ZoneDto
            {
                CurrentRegion = _currentRegion[z],
                Unlocked = _unlockedRegions[z],
            };
            foreach (var snap in _zoneData[z])
            {
                dto.Regions.Add(new RegionDto
                {
                    Tiles = TilesToJagged(snap.Tiles),
                    Gold = FloatsToJagged(snap.Gold),
                    Clay = FloatsToJagged(snap.Clay),
                    Flow = FloatsToJagged(snap.Flow),
                });
            }
            zones[z] = dto;
        }

        return new Snapshot
        {
            Gold = Gold,
            Clay = Clay,
            Shovels = Shovels,
            Tool = (int)Tool,
            RiverSpeed = RiverSpeed,
            CurrentZone = CurrentZone,
            Quests = (bool[])QuestsComplete.Clone(),
            Zones = zones,
        };
    }

    // Restore state from a snapshot, then emit a full set of refresh signals
    // so any attached views rebuild. Derived flow direction data is left for
    // the caller to recompute (GameRunner.StepPropagation).
    public void ApplySnapshot(Snapshot snap)
    {
        Gold = snap.Gold;
        Clay = snap.Clay;
        Shovels = snap.Shovels;
        Tool = (ActiveTool)snap.Tool;
        RiverSpeed = snap.RiverSpeed;
        System.Array.Copy(snap.Quests, QuestsComplete, System.Math.Min(snap.Quests.Length, QuestsComplete.Length));

        for (int z = 0; z < _zoneData.Length; z++)
        {
            _zoneData[z].Clear();
            var zdto = snap.Zones[z];
            _currentRegion[z] = zdto.CurrentRegion;
            _unlockedRegions[z] = zdto.Unlocked;
            foreach (var r in zdto.Regions)
            {
                _zoneData[z].Add(new RegionSnapshot(
                    JaggedToTiles(r.Tiles),
                    JaggedToFloats(r.Gold),
                    JaggedToFloats(r.Clay),
                    JaggedToFloats(r.Flow)));
            }
        }

        CurrentZone = snap.CurrentZone;
        TileFlowParent = InitFlowParent();
        SwapActiveTo(CurrentRegion);

        EmitSignal(SignalName.ZoneChanged, CurrentZone);
        EmitSignal(SignalName.RegionSwitched, CurrentRegion);
        EmitSignal(SignalName.GoldChanged, Gold);
        EmitSignal(SignalName.ClayChanged, Clay);
        EmitSignal(SignalName.ShovelsChanged, Shovels);
        EmitSignal(SignalName.ToolChanged, (int)Tool);
        EmitSignal(SignalName.FlowChanged);
    }

    public void Save(string path)
    {
        var json = JsonSerializer.Serialize(ToSnapshot(), new JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(path, json);
    }

    public void Load(string path)
    {
        var json = System.IO.File.ReadAllText(path);
        ApplySnapshot(JsonSerializer.Deserialize<Snapshot>(json));
    }

    private static int[][] TilesToJagged(TileType[,] src)
    {
        var jagged = new int[Rows][];
        for (int r = 0; r < Rows; r++)
        {
            jagged[r] = new int[Cols];
            for (int c = 0; c < Cols; c++)
                jagged[r][c] = (int)src[r, c];
        }
        return jagged;
    }

    private static float[][] FloatsToJagged(float[,] src)
    {
        var jagged = new float[Rows][];
        for (int r = 0; r < Rows; r++)
        {
            jagged[r] = new float[Cols];
            for (int c = 0; c < Cols; c++)
                jagged[r][c] = src[r, c];
        }
        return jagged;
    }

    private static TileType[,] JaggedToTiles(int[][] src)
    {
        var arr = new TileType[Rows, Cols];
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                arr[r, c] = (TileType)src[r][c];
        return arr;
    }

    private static float[,] JaggedToFloats(float[][] src)
    {
        var arr = new float[Rows, Cols];
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                arr[r, c] = src[r][c];
        return arr;
    }
}
