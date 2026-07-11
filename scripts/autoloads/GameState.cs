using Godot;
using System.Collections.Generic;
using System.Text.Json;

public partial class GameState : Node
{
    public enum TileType { Soil = 0, Bank = 1, River = 2, Channel = 3, Stone = 4, RiverSource = 5, Village = 6, Gate = 7, GoldSource = 8, ClaySource = 9, Brick = 10, Furnace = 11, ShovelRental = 12 }
    // Manual panning is gone; gold/clay come from autopanner buildings. Default tool digs.
    public enum ActiveTool { Shovel = 0, Brick = 1, Furnace = 2, AutopanGold = 3, AutopanClay = 4, ShovelRental = 5 }

    public const int Cols = 14;
    public const int Rows = 14;

    // --- Rate-economy + scalar-flow constants ---
    public const int BuildCapPerType = 3;            // max of each building type (gold/clay autopanner, furnace) per map
    public const float BaseInflow = 1000f;           // input flow at a zone's first region (its source)
    public const float FlowCostPerTile = 10f;        // flow consumed by each Soil/Bank tile adjacent to a river
    public const float AutopanYieldPerFlow = 0.02f;  // gold or clay /sec per running autopanner, per unit of input flow
    public const float FurnaceClayPerSec = 2f;       // clay /sec a running furnace draws (to fire bricks)
    public const float BrickPerFurnacePerSec = 2f;   // bricks /sec a running, clay-fed furnace produces
    public const float BrickUpkeepPerSec = 0.25f;    // brick /sec each laid Brick tile consumes
    public const float ShovelRentalGoldPerSec = 5f;  // gold /sec a Shovel Rental draws; while a map covers it the dig tool unlocks

    public const float MaxBankFlow = 10f;            // kept: shader flow normalisation reference
    // Back-compat constants still referenced by the (now unused) DAG flow files and a
    // few tests. New code resolves villages via VillageDefs.ForRegion.
    public const float VillageFlowThreshold = 100f;
    public const int VillageRow = 0;
    public const int VillageCol = 7;

    public static GameState Instance { get; private set; }

    // Economy — RATES only (no accumulation). Recomputed each tick by Economy.Recompute
    // for the current map. Autopanners generate; villages/furnaces consume.
    public float GoldGen { get; internal set; }
    public float GoldUse { get; internal set; }
    public float ClayGen { get; internal set; }
    public float ClayUse { get; internal set; }
    public float BrickGen { get; internal set; } // bricks/sec produced by running, clay-fed furnaces
    public float BrickUse { get; internal set; } // bricks/sec consumed by laid Brick tiles
    public bool HasFurnace { get; internal set; } // means: the furnace/build tools are unlocked
    // True while some map has a Shovel Rental whose gold demand is covered — gates the dig tool.
    // Runtime only (recomputed each tick by Economy.Recompute); not serialized.
    public bool ShovelsEnabled { get; internal set; }
    public ActiveTool Tool { get; internal set; } = ActiveTool.Shovel;

    // Grid data [row, col]
    public TileType[,] Tiles { get; private set; } = new TileType[Rows, Cols];
    // Per-tile Gold/Clay arrays are retained (zeroed, unused) so the region snapshot
    // shape and save format stay stable; gold/clay are now rates, not per-tile pools.
    public float[,] TileGold { get; private set; } = new float[Rows, Cols];
    public float[,] TileClay { get; private set; } = new float[Rows, Cols];
    // Furnace per-tile state: >= 0 enabled (value = accrual progress in [0,1)),
    // < 0 disabled (value = -(progress + 1)). Only meaningful on Furnace tiles.
    public float[,] TileFurnace { get; private set; } = new float[Rows, Cols];
    // Autopanner overlay on a land tile beside a river. Encoding:
    //   0 = none, +1 = gold (running), -1 = gold (paused), +2 = clay (running), -2 = clay (paused).
    public float[,] TileMachine { get; private set; } = new float[Rows, Cols];
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

    // Flow is now a single scalar per region (InputFlow from the previous map, OutputFlow
    // passed to the next). Added as mutable members so existing constructor calls are
    // unchanged; recomputed each tick by FlowModel, so not serialized.
    internal record RegionSnapshot(TileType[,] Tiles, float[,] Gold, float[,] Clay, float[,] Flow, float[,] Furnace, float[,] Machine)
    {
        public float InputFlow;
        public float OutputFlow;
    }

    // Autopanner overlay decoders (TileMachine encoding).
    public static int MachineKind(float v) => (int)System.Math.Abs(v); // 0 none, 1 gold, 2 clay
    public static bool MachineRunning(float v) => v > 0f;

    [Signal] public delegate void TileChangedEventHandler(int col, int row);
    // Emitted after each Economy.Recompute with the current map's rates refreshed.
    [Signal] public delegate void RatesChangedEventHandler();
    [Signal] public delegate void FurnaceChangedEventHandler(bool hasFurnace);
    [Signal] public delegate void ToolChangedEventHandler(int tool);
    [Signal] public delegate void ZoneChangedEventHandler(int zone);
    [Signal] public delegate void RegionUnlockedEventHandler(int count);
    [Signal] public delegate void RegionSwitchedEventHandler(int index);
    [Signal] public delegate void SpeedChangedEventHandler(float value);
    [Signal] public delegate void FlowChangedEventHandler();
    [Signal] public delegate void QuestChangedEventHandler(int index);
    [Signal] public delegate void VillageFoundEventHandler(int villageId);
    [Signal] public delegate void VillageSupplyChangedEventHandler(int villageId, bool supplied);

    // Quests — length matches QuestSystem.Defs.
    public bool[] QuestsComplete { get; private set; } = new bool[10];

    // One bit per VillageDefs entry; set the first time the player enters that
    // village's region. Mutated element-wise by VillageSystem (like QuestsComplete).
    public bool[] VillagesDiscovered { get; private set; } = new bool[VillageDefs.Count];

    // Back-compat: "the first village is discovered" (reveals Highlands + furnace).
    public bool VillageDiscovered => VillagesDiscovered.Length > 0 && VillagesDiscovered[0];

    // Per-village gold-supply drain toggle (only meaningful for villages with
    // GoldDemand > 0). Default on, so a thirsty village drains until the player
    // turns it off. Serialized. VillageSupplied is runtime-only render feedback:
    // true when the village is currently being supplied (income >= demand).
    public bool[] VillageSupplyOn { get; private set; } = NewSupplyOn();
    public bool[] VillageSupplied { get; private set; } = new bool[VillageDefs.Count];

    private static bool[] NewSupplyOn()
    {
        var arr = new bool[VillageDefs.Count];
        for (int i = 0; i < arr.Length; i++) arr[i] = true;
        return arr;
    }

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
        TileFurnace    = snap.Furnace;
        TileMachine    = snap.Machine;
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
        GoldGen = GoldUse = ClayGen = ClayUse = BrickGen = BrickUse = 0f;
        HasFurnace = false;
        ShovelsEnabled = false;
        Tool = ActiveTool.Shovel;
        CurrentZone = 0;
        _currentRegion[0] = 0;
        _currentRegion[1] = 0;
        _unlockedRegions[0] = 1;
        _unlockedRegions[1] = 1;
        _zoneData[0].Clear();
        _zoneData[1].Clear();
        System.Array.Clear(QuestsComplete);
        System.Array.Clear(VillagesDiscovered);
        System.Array.Clear(VillageSupplied);
        for (int i = 0; i < VillageSupplyOn.Length; i++) VillageSupplyOn[i] = true;
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
        // Per-tile furnace state (null in pre-furnace saves).
        public float[][] Furnace { get; set; }
        // Per-tile autopan machine state (null in pre-machine saves).
        public float[][] Machine { get; set; }
    }

    public sealed class ZoneDto
    {
        public int CurrentRegion { get; set; }
        public int Unlocked { get; set; }
        public List<RegionDto> Regions { get; set; } = [];
    }

    public sealed class Snapshot
    {
        public bool HasFurnace { get; set; }
        public int Tool { get; set; }
        public int CurrentZone { get; set; }
        public bool[] Quests { get; set; }
        // Legacy single-village flag, still written/read so older saves load.
        public bool VillageDiscovered { get; set; }
        // Per-village discovery bits (preferred; null in pre-multi-village saves).
        public bool[] VillagesDiscovered { get; set; }
        // Per-village gold-drain toggle (null in pre-supply saves -> default on).
        public bool[] VillageSupplyOn { get; set; }
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
                    Furnace = FloatsToJagged(snap.Furnace),
                    Machine = FloatsToJagged(snap.Machine),
                });
            }
            zones[z] = dto;
        }

        return new Snapshot
        {
            HasFurnace = HasFurnace,
            Tool = (int)Tool,
            CurrentZone = CurrentZone,
            Quests = (bool[])QuestsComplete.Clone(),
            VillageDiscovered = VillageDiscovered,
            VillagesDiscovered = (bool[])VillagesDiscovered.Clone(),
            VillageSupplyOn = (bool[])VillageSupplyOn.Clone(),
            Zones = zones,
        };
    }

    // Restore state from a snapshot, then emit a full set of refresh signals
    // so any attached views rebuild. Derived flow direction data is left for
    // the caller to recompute (GameRunner.StepPropagation).
    public void ApplySnapshot(Snapshot snap)
    {
        GoldGen = GoldUse = ClayGen = ClayUse = BrickGen = BrickUse = 0f;
        ShovelsEnabled = false; // recomputed by the StepPropagation the caller runs after load
        HasFurnace = snap.HasFurnace;
        Tool = (ActiveTool)snap.Tool;
        System.Array.Copy(snap.Quests, QuestsComplete, System.Math.Min(snap.Quests.Length, QuestsComplete.Length));
        System.Array.Clear(VillagesDiscovered);
        if (snap.VillagesDiscovered != null)
            System.Array.Copy(snap.VillagesDiscovered, VillagesDiscovered,
                System.Math.Min(snap.VillagesDiscovered.Length, VillagesDiscovered.Length));
        else if (VillagesDiscovered.Length > 0)
            VillagesDiscovered[0] = snap.VillageDiscovered; // seed from legacy flag

        for (int i = 0; i < VillageSupplyOn.Length; i++) VillageSupplyOn[i] = true;
        if (snap.VillageSupplyOn != null)
            System.Array.Copy(snap.VillageSupplyOn, VillageSupplyOn,
                System.Math.Min(snap.VillageSupplyOn.Length, VillageSupplyOn.Length));
        System.Array.Clear(VillageSupplied);

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
                    JaggedToFloats(r.Flow),
                    r.Furnace != null ? JaggedToFloats(r.Furnace) : new float[Rows, Cols],
                    r.Machine != null ? JaggedToFloats(r.Machine) : new float[Rows, Cols]));
            }
        }

        CurrentZone = snap.CurrentZone;
        TileFlowParent = InitFlowParent();
        SwapActiveTo(CurrentRegion);

        EmitSignal(SignalName.ZoneChanged, CurrentZone);
        EmitSignal(SignalName.RegionSwitched, CurrentRegion);
        EmitSignal(SignalName.FurnaceChanged, HasFurnace);
        EmitSignal(SignalName.ToolChanged, (int)Tool);
        EmitSignal(SignalName.RatesChanged);
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
