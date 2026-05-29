extends Node2D

const _IsoMath      = preload("res://scripts/overworld/iso_math.gd")
const _TileRenderer = preload("res://scripts/overworld/tile_renderer.gd")

signal dig_requested(col: int, row: int)
signal pan_requested(col: int, row: int)

var _iso:      RefCounted  # IsoMath instance
var _renderer: Node2D      # TileRenderer instance

var _hover_line: Line2D

func _ready() -> void:
	add_to_group("grid")
	_iso = _IsoMath.new()
	_renderer = _TileRenderer.new()
	add_child(_renderer)
	_renderer.build(_iso)

	_hover_line = Line2D.new()
	_hover_line.width = 2.0
	_hover_line.default_color = Color(1.0, 1.0, 0.8, 0.9)
	_hover_line.visible = false
	add_child(_hover_line)

	_renderer.build_preview()

	GameState.tile_changed.connect(_on_tile_changed)
	GameState.tile_gold_changed.connect(_on_tile_gold_changed)
	GameState.tool_changed.connect(_on_tool_changed)
	GameState.region_switched.connect(_on_region_switched)
	GameState.region_unlocked.connect(_on_region_unlocked)
	GameState.flow_changed.connect(_on_flow_changed)

func _input(event: InputEvent) -> void:
	if event is InputEventMouseMotion:
		_update_hover((event as InputEventMouseMotion).position)
		return
	if not event is InputEventMouseButton:
		return
	if not (event as InputEventMouseButton).pressed:
		return
	if (event as InputEventMouseButton).button_index != MOUSE_BUTTON_LEFT:
		return
	var tile: Vector2i = _iso.screen_to_tile(event.position)
	if tile.x < 0 or tile.x >= GameState.COLS or tile.y < 0 or tile.y >= GameState.ROWS:
		return
	if GameState.active_tool == GameState.ActiveTool.SHOVEL:
		dig_requested.emit(tile.x, tile.y)
	elif GameState.active_tool == GameState.ActiveTool.PAN:
		if GameState.tiles[tile.y][tile.x] == GameState.TileType.BANK:
			var amount: int = int(GameState.tile_gold[tile.y][tile.x])
			pan_requested.emit(tile.x, tile.y)
			_flash_tile(tile.x, tile.y)
			if amount > 0:
				_show_gold_popup(tile.x, tile.y, amount)

func _update_hover(mouse_pos: Vector2) -> void:
	var tile: Vector2i = _iso.screen_to_tile(mouse_pos)
	if tile.x < 0 or tile.x >= GameState.COLS or tile.y < 0 or tile.y >= GameState.ROWS:
		_hover_line.visible = false
		return
	if GameState.tiles.is_empty() or GameState.tiles[tile.y].is_empty():
		_hover_line.visible = false
		return
	var clickable: bool = false
	if GameState.active_tool == GameState.ActiveTool.SHOVEL and GameState.shovels > 0:
		clickable = true
	elif GameState.active_tool == GameState.ActiveTool.PAN:
		clickable = GameState.tiles[tile.y][tile.x] == GameState.TileType.BANK
	if not clickable:
		_hover_line.visible = false
		return
	var verts: PackedVector2Array = _iso.diamond_verts(tile.x, tile.y)
	verts.append(verts[0])
	_hover_line.points = verts
	_hover_line.visible = true

func _show_gold_popup(col: int, row: int, amount: int) -> void:
	var label: Label = Label.new()
	label.text = "+%d" % amount
	label.add_theme_font_size_override("font_size", 16)
	label.add_theme_color_override("font_color", Color(1.0, 0.88, 0.3))
	label.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.8))
	label.add_theme_constant_override("shadow_offset_x", 1)
	label.add_theme_constant_override("shadow_offset_y", 1)
	var c: Vector2 = _iso.tile_center(col, row)
	label.position = c - Vector2(20, 10)
	add_child(label)
	var tween: Tween = create_tween()
	tween.tween_property(label, "position", label.position + Vector2(0, -40), 0.7)
	tween.parallel().tween_property(label, "modulate:a", 0.0, 0.7)
	tween.tween_callback(label.queue_free)

func _flash_tile(col: int, row: int) -> void:
	var poly: Polygon2D = _renderer.get_tile_node(col, row)
	var tween: Tween = create_tween()
	tween.tween_property(poly, "modulate", Color(1.5, 1.3, 0.9), 0.05)
	tween.tween_property(poly, "modulate", Color.WHITE, 0.2)

func _on_tile_changed(col: int, row: int) -> void:
	_renderer.refresh_wall(col, row)
	_renderer.refresh_tile_and_neighbors(col, row)

func _on_tile_gold_changed(col: int, row: int, amount: int) -> void:
	_renderer.refresh_gold(col, row, amount)

func _on_flow_changed() -> void:
	_renderer.refresh_all_flow()

func _on_region_switched(_index: int) -> void:
	_renderer.refresh_all_tiles()
	_renderer.build_preview()

func _on_region_unlocked(_count: int) -> void:
	_renderer.build_preview()

func _on_tool_changed(_tool: int) -> void:
	pass
