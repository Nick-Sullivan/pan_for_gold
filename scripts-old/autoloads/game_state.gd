extends Node

enum TileType {SOIL = 0, BANK = 1, RIVER = 2, CHANNEL = 3, STONE = 4, RIVER_SOURCE = 5}
enum ActiveTool {PAN = 0, SHOVEL = 1}

const COLS: int = 14
const ROWS: int = 14
const MAX_TILE_GOLD: float = 10.0
const REFILL_TIME: float = 15.0
const SHOVEL_COST: int = 10
const FILL_DELAY_PER_STEP: float = 0.25
const BASE_SPEED_TILES: int = 4

var gold: int = 0
var shovels: int = 0
var active_tool: int = ActiveTool.PAN
var river_speed: float = 1.0

var tiles: Array = []
var tile_gold: Array = []
var tile_flow_values: Array = []
var tile_flow_dir: Array = []
var tile_bfs_depth: Array = []
var tile_flow_parent: Array = []  # [row][col] = Array[Vector2i] of upstream parents; empty = no flow
var _pending_fills: Array = []

var _region_data: Array = []
var current_region: int = 0
var unlocked_regions: int = 1

signal tile_changed(col: int, row: int)
signal gold_changed(new_value: int)
signal tile_gold_changed(col: int, row: int, amount: int)
signal shovels_changed(new_value: int)
signal tool_changed(tool: int)
signal region_unlocked(count: int)
signal region_switched(index: int)
signal speed_changed(value: float)
signal flow_changed
