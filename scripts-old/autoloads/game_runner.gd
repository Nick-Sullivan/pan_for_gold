extends Node

const _EconomyScript = preload("res://scripts/game/economy.gd")
const _TileEditorScript = preload("res://scripts/game/tile_editor.gd")
const _WaterScript = preload("res://scripts/game/water_propagation.gd")
const _RegionScript = preload("res://scripts/game/region_system.gd")

var _economy: RefCounted
var _tiles: RefCounted
var _water: RefCounted
var _regions: RefCounted

func _ready() -> void:
	_economy = _EconomyScript.new()
	_tiles = _TileEditorScript.new()
	_water = _WaterScript.new()
	_regions = _RegionScript.new()
	_water.init_tiles()
	_regions.init()
	# _water.run_flood_fill(Vector2i(-1, -1), true)
	get_tree().root.ready.connect(_connect_view_signals)

func _connect_view_signals() -> void:
	var grid: Node = get_tree().get_first_node_in_group("grid")
	var hud: Node = get_tree().get_first_node_in_group("hud")
	if grid:
		grid.dig_requested.connect(_on_dig)
		grid.pan_requested.connect(_on_pan)
	if hud:
		hud.buy_shovel_requested.connect(_on_buy_shovel)
		hud.tool_selected.connect(_on_set_tool)
		hud.region_selected.connect(_on_switch_region)

func _process(delta: float) -> void:
	# var any_filled: bool = _water.tick_fills(delta)
	# if any_filled:
	# 	_water.recompute_flow()
	# 	_regions.try_unlock()
	# 	_regions.sync_next_entries()
	_economy.tick_gold(delta)

func _on_dig(col: int, row: int) -> void:
	if not _tiles.can_dig(col, row):
		return
	_tiles.dig(col, row)
	# _water.run_flood_fill(Vector2i(col, row), true)
	_regions.try_unlock()
	_regions.sync_next_entries()

func _on_pan(col: int, row: int) -> void:
	_economy.pan(col, row)

func _on_buy_shovel() -> void:
	_economy.buy_shovel()

func _on_set_tool(tool: int) -> void:
	GameState.active_tool = tool
	GameState.tool_changed.emit(tool )

func _on_switch_region(index: int) -> void:
	_regions.switch_to(index)
	# _water.run_flood_fill(Vector2i(-1, -1), false)
	# _water.recompute_flow()
	_regions.try_unlock()
	_regions.sync_next_entries()
