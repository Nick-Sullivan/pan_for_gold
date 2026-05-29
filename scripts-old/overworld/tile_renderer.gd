extends Node2D

const WaterShader:   Shader = preload("res://assets/shaders/tile_water.gdshader")
const ChannelShader: Shader = preload("res://assets/shaders/tile_channel.gdshader")
const BankShader:    Shader = preload("res://assets/shaders/tile_bank.gdshader")
const SoilShader:    Shader = preload("res://assets/shaders/tile_soil.gdshader")

var DIAMOND_UV: PackedVector2Array = PackedVector2Array([
	Vector2(0.5, 0.0),
	Vector2(1.0, 0.5),
	Vector2(0.5, 1.0),
	Vector2(0.0, 0.5),
])
const PREVIEW_COLS: int = 1
const WALL_H: int = 12

var _tile_nodes:    Array = []
var _tile_labels:   Array = []
var _se_wall:       Array = []
var _sw_wall:       Array = []
var _preview_nodes: Array = []

var _iso: RefCounted  # IsoMath instance passed from grid

func build(iso: RefCounted) -> void:
	_iso = iso
	_build_wall()
	_build_tiles()

func _build_wall() -> void:
	for col in range(GameState.COLS):
		var row: int = GameState.ROWS - 1
		var t:      Vector2 = _iso.tile_top(col, row)
		var left:   Vector2 = t + Vector2(-_iso.HW, _iso.HH)
		var bottom: Vector2 = t + Vector2(0, _iso.HH * 2)
		var poly: Polygon2D = Polygon2D.new()
		poly.polygon = PackedVector2Array([left, bottom, bottom + Vector2(0, WALL_H), left + Vector2(0, WALL_H)])
		poly.color = _iso.wall_color(GameState.tiles[row][col], col, row)
		add_child(poly)
		_se_wall.append(poly)
	for row in range(GameState.ROWS):
		var col: int = GameState.COLS - 1
		var t:      Vector2 = _iso.tile_top(col, row)
		var right:  Vector2 = t + Vector2(_iso.HW, _iso.HH)
		var bottom: Vector2 = t + Vector2(0, _iso.HH * 2)
		var poly: Polygon2D = Polygon2D.new()
		poly.polygon = PackedVector2Array([right, bottom, bottom + Vector2(0, WALL_H), right + Vector2(0, WALL_H)])
		poly.color = _iso.wall_color(GameState.tiles[row][col], col, row)
		add_child(poly)
		_sw_wall.append(poly)

func _build_tiles() -> void:
	for row in range(GameState.ROWS):
		var node_row:  Array = []
		var label_row: Array = []
		for col in range(GameState.COLS):
			var tile_type:   int = GameState.tiles[row][col]
			var gold_amount: int = int(GameState.tile_gold[row][col])

			var poly: Polygon2D = Polygon2D.new()
			poly.polygon = _iso.diamond_verts(col, row)
			poly.uv      = DIAMOND_UV

			var mat: ShaderMaterial = ShaderMaterial.new()
			mat.shader = _shader_for(tile_type)
			_apply_params(mat, tile_type, gold_amount, col, row)
			poly.material = mat
			add_child(poly)

			var label: Label = Label.new()
			var c: Vector2 = _iso.tile_center(col, row)
			label.position = c - Vector2(30, 10)
			label.size     = Vector2(60, 20)
			label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
			label.vertical_alignment   = VERTICAL_ALIGNMENT_CENTER
			label.add_theme_font_size_override("font_size", 10)
			label.visible = tile_type == GameState.TileType.BANK or tile_type == GameState.TileType.RIVER
			label.text    = str(gold_amount) if tile_type == GameState.TileType.BANK \
				else str(GameState.tile_flow_values[row][col])
			add_child(label)

			node_row.append(poly)
			label_row.append(label)
		_tile_nodes.append(node_row)
		_tile_labels.append(label_row)

func refresh_tile(col: int, row: int) -> void:
	var tile_type:   int = GameState.tiles[row][col]
	var gold_amount: int = int(GameState.tile_gold[row][col])
	var poly: Polygon2D = _tile_nodes[row][col]
	var mat: ShaderMaterial = poly.material as ShaderMaterial
	mat.shader = _shader_for(tile_type)
	_apply_params(mat, tile_type, gold_amount, col, row)
	var c: Vector2 = _iso.tile_center(col, row)
	_tile_labels[row][col].position = c - Vector2(30, 10)
	_tile_labels[row][col].visible  = tile_type == GameState.TileType.BANK or tile_type == GameState.TileType.RIVER
	_tile_labels[row][col].text     = str(gold_amount) if tile_type == GameState.TileType.BANK \
		else str(GameState.tile_flow_values[row][col])

# Refresh tile plus update neighbour soil tints (called on tile_changed).
func refresh_tile_and_neighbors(col: int, row: int) -> void:
	refresh_tile(col, row)
	for dc in range(-2, 3):
		for dr in range(-2, 3):
			if abs(dc) + abs(dr) > 2 or (dc == 0 and dr == 0):
				continue
			var nx: int = col + dc
			var ny: int = row + dr
			if nx < 0 or nx >= GameState.COLS or ny < 0 or ny >= GameState.ROWS:
				continue
			match GameState.tiles[ny][nx]:
				GameState.TileType.SOIL:
					var mat: ShaderMaterial = _tile_nodes[ny][nx].material as ShaderMaterial
					mat.set_shader_parameter("tint", _iso.soil_tint(nx, ny))
				GameState.TileType.RIVER:
					if abs(dc) + abs(dr) == 1:
						refresh_tile(nx, ny)

func refresh_wall(col: int, row: int) -> void:
	if row == GameState.ROWS - 1:
		_se_wall[col].color = _iso.wall_color(GameState.tiles[row][col], col, row)
	if col == GameState.COLS - 1:
		_sw_wall[row].color = _iso.wall_color(GameState.tiles[row][col], col, row)

func refresh_gold(col: int, row: int, amount: int) -> void:
	var mat: ShaderMaterial = _tile_nodes[row][col].material as ShaderMaterial
	mat.set_shader_parameter("gold_ratio", float(amount) / GameState.MAX_TILE_GOLD)
	_tile_labels[row][col].text = str(amount)

func refresh_all_tiles() -> void:
	for row in range(GameState.ROWS):
		for col in range(GameState.COLS):
			refresh_tile(col, row)
			refresh_wall(col, row)

func refresh_all_flow() -> void:
	for row in range(GameState.ROWS):
		for col in range(GameState.COLS):
			if GameState.tiles[row][col] != GameState.TileType.RIVER:
				continue
			var mat: ShaderMaterial = _tile_nodes[row][col].material as ShaderMaterial
			mat.set_shader_parameter("flow_speed", GameState.tile_flow_values[row][col] / 1000.0)
			mat.set_shader_parameter("flow_dir",   GameState.tile_flow_dir[row][col])
			mat.set_shader_parameter("bfs_depth",  GameState.tile_bfs_depth[row][col])
			var conn: Dictionary = _river_connectivity(col, row)
			mat.set_shader_parameter("north",      conn.north)
			mat.set_shader_parameter("south",      conn.south)
			mat.set_shader_parameter("east",       conn.east)
			mat.set_shader_parameter("west",       conn.west)
			mat.set_shader_parameter("soil_tint",  _iso.soil_tint(col, row))
			_tile_labels[row][col].text = str(GameState.tile_flow_values[row][col])

func build_preview() -> void:
	for node in _preview_nodes:
		node.queue_free()
	_preview_nodes.clear()
	var next_tiles: Array = _get_region_tiles(GameState.current_region + 1)
	for row in range(GameState.ROWS):
		for dc in range(PREVIEW_COLS):
			if next_tiles.is_empty():
				break
			var tile_type: int = next_tiles[row][dc]
			var poly: Polygon2D = Polygon2D.new()
			poly.polygon = _iso.diamond_verts(GameState.COLS + dc, row)
			poly.color = _iso.preview_color(tile_type)
			add_child(poly)
			_preview_nodes.append(poly)
			if dc == PREVIEW_COLS - 1:
				_add_preview_walls(GameState.COLS + dc, row, tile_type, true)
	var prev_tiles: Array = _get_region_tiles(GameState.current_region - 1)
	if not prev_tiles.is_empty():
		for row in range(GameState.ROWS):
			for dc in range(PREVIEW_COLS):
				var src_col: int = GameState.COLS - PREVIEW_COLS + dc
				var tile_type: int = prev_tiles[row][src_col]
				var poly: Polygon2D = Polygon2D.new()
				poly.polygon = _iso.diamond_verts(-PREVIEW_COLS + dc, row)
				poly.color = _iso.preview_color(tile_type)
				add_child(poly)
				_preview_nodes.append(poly)
				if dc == 0:
					_add_preview_walls(-PREVIEW_COLS + dc, row, tile_type, true)
	else:
		for row in range(GameState.ROWS):
			var is_river: bool = GameState.tiles[row][0] == GameState.TileType.RIVER
			var tile_type: int = GameState.TileType.RIVER if is_river else GameState.TileType.SOIL
			var poly: Polygon2D = Polygon2D.new()
			poly.polygon = _iso.diamond_verts(-1, row)
			poly.color = _iso.preview_color(tile_type)
			add_child(poly)
			_preview_nodes.append(poly)
			_add_preview_walls(-1, row, tile_type, true)

func get_tile_node(col: int, row: int) -> Polygon2D:
	return _tile_nodes[row][col]

func _add_preview_walls(col: int, row: int, tile_type: int, show_sw: bool) -> void:
	var t: Vector2 = _iso.tile_top(col, row)
	var wc: Color = _iso.preview_wall_color(tile_type)
	if row == GameState.ROWS - 1:
		var left:   Vector2 = t + Vector2(-_iso.HW, _iso.HH)
		var bottom: Vector2 = t + Vector2(0, _iso.HH * 2)
		var se: Polygon2D = Polygon2D.new()
		se.polygon = PackedVector2Array([left, bottom, bottom + Vector2(0, WALL_H), left + Vector2(0, WALL_H)])
		se.color = wc
		add_child(se)
		_preview_nodes.append(se)
	if show_sw:
		var right:  Vector2 = t + Vector2(_iso.HW, _iso.HH)
		var bottom: Vector2 = t + Vector2(0, _iso.HH * 2)
		var sw: Polygon2D = Polygon2D.new()
		sw.polygon = PackedVector2Array([right, bottom, bottom + Vector2(0, WALL_H), right + Vector2(0, WALL_H)])
		sw.color = wc
		add_child(sw)
		_preview_nodes.append(sw)

func _shader_for(tile_type: int) -> Shader:
	match tile_type:
		GameState.TileType.RIVER:   return WaterShader
		GameState.TileType.CHANNEL: return ChannelShader
		GameState.TileType.BANK:    return BankShader
		_:                          return SoilShader

func _river_connectivity(col: int, row: int) -> Dictionary:
	return {
		"north": 1.0 if row > 0                  and GameState.tiles[row - 1][col] == GameState.TileType.RIVER else 0.0,
		"south": 1.0 if row < GameState.ROWS - 1 and GameState.tiles[row + 1][col] == GameState.TileType.RIVER else 0.0,
		"east":  1.0 if col < GameState.COLS - 1 and GameState.tiles[row][col + 1] == GameState.TileType.RIVER else 0.0,
		"west":  1.0 if col > 0                  and GameState.tiles[row][col - 1] == GameState.TileType.RIVER else 0.0,
	}

func _apply_params(mat: ShaderMaterial, tile_type: int, gold_amount: int, col: int, row: int) -> void:
	if tile_type == GameState.TileType.BANK:
		mat.set_shader_parameter("gold_ratio", float(gold_amount) / GameState.MAX_TILE_GOLD)
	elif tile_type == GameState.TileType.RIVER:
		mat.set_shader_parameter("flow_speed", GameState.tile_flow_values[row][col] / 1000.0)
		mat.set_shader_parameter("flow_dir",   GameState.tile_flow_dir[row][col])
		mat.set_shader_parameter("bfs_depth",  GameState.tile_bfs_depth[row][col])
		var conn: Dictionary = _river_connectivity(col, row)
		mat.set_shader_parameter("north",      conn.north)
		mat.set_shader_parameter("south",      conn.south)
		mat.set_shader_parameter("east",       conn.east)
		mat.set_shader_parameter("west",       conn.west)
		mat.set_shader_parameter("soil_tint",  _iso.soil_tint(col, row))
	elif tile_type == GameState.TileType.SOIL:
		mat.set_shader_parameter("tint", _iso.soil_tint(col, row))
	elif tile_type == GameState.TileType.STONE:
		mat.set_shader_parameter("tint", Color(0.55, 0.52, 0.50))

func _get_region_tiles(index: int) -> Array:
	if index < 0 or index >= GameState._region_data.size():
		return []
	return GameState._region_data[index]["tiles"]
