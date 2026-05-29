extends RefCounted

# const _FlowSolver = preload("res://scripts/autoloads/flow_solver.gd")

func init_tiles() -> void:
	var tiles: Array = []
	var tile_gold: Array = []
	for row in range(GameState.ROWS):
		var tile_row: Array = []
		var gold_row: Array = []
		for col in range(GameState.COLS):
			var t: int = _starting_tile(row, col)
			tile_row.append(t)
			gold_row.append(GameState.MAX_TILE_GOLD if t == GameState.TileType.BANK else 0.0)
		tiles.append(tile_row)
		tile_gold.append(gold_row)
	GameState.tile_flow_values = _make_grid(0)
	GameState.tile_flow_dir = _make_grid(Vector2(1, 0))
	GameState.tile_bfs_depth = _make_grid(0.0)
	GameState.tile_flow_parent = _make_parent_grid()
	GameState._region_data.clear()
	GameState._region_data.append({"tiles": tiles, "tile_gold": tile_gold})
	GameState.tiles = tiles
	GameState.tile_gold = tile_gold

func _starting_tile(row: int, col: int) -> int:
	if row == 6:
		return GameState.TileType.RIVER if col < 4 else GameState.TileType.SOIL
	if row in [5, 7] and col > 0 and col < 4:
		return GameState.TileType.BANK
	if col == 0 or row == 0 or row == GameState.ROWS - 1:
		return GameState.TileType.STONE
	return GameState.TileType.SOIL

# # Three-pass BFS: connectivity → drain disconnected RIVER → fill adjacent CHANNEL.
# # changed_pos: tile that was just modified (Vector2i(-1,-1) = full reseed).
# # schedule_fills: false skips the fill pass (used when switching regions).
# func run_flood_fill(changed_pos: Vector2i, schedule_fills: bool) -> void:
# 	GameState._pending_fills.clear()

# 	# --- Pass 1: connectivity BFS from col-0 RIVER ---
# 	var connected: Array = _make_grid(false)
# 	var q: Array = []
# 	for row in range(GameState.ROWS):
# 		if GameState.tiles[row][0] == GameState.TileType.RIVER:
# 			connected[row][0] = true
# 			q.append(Vector2i(0, row))
# 	var h: int = 0
# 	while h < q.size():
# 		var pos: Vector2i = q[h]; h += 1
# 		for n in _neighbors(pos):
# 			if connected[n.y][n.x]: continue
# 			if GameState.tiles[n.y][n.x] != GameState.TileType.RIVER: continue
# 			connected[n.y][n.x] = true
# 			q.append(n)

# 	# --- Pass 2: drain — BFS outward from disconnected RIVER tiles near the break ---
# 	var drain_depth: Array = _make_grid(-1)
# 	q = []
# 	if changed_pos != Vector2i(-1, -1):
# 		for n in _neighbors(changed_pos):
# 			if GameState.tiles[n.y][n.x] == GameState.TileType.RIVER and not connected[n.y][n.x]:
# 				if drain_depth[n.y][n.x] == -1:
# 					drain_depth[n.y][n.x] = 0
# 					q.append(n)
# 	if q.is_empty():
# 		for row in range(GameState.ROWS):
# 			for col in range(GameState.COLS):
# 				if GameState.tiles[row][col] != GameState.TileType.RIVER or connected[row][col]:
# 					continue
# 				var is_edge: bool = false
# 				for n in _neighbors(Vector2i(col, row)):
# 					if GameState.tiles[n.y][n.x] != GameState.TileType.RIVER or connected[n.y][n.x]:
# 						is_edge = true
# 						break
# 				if col == 0 or col == GameState.COLS - 1 or row == 0 or row == GameState.ROWS - 1:
# 					is_edge = true
# 				if is_edge and drain_depth[row][col] == -1:
# 					drain_depth[row][col] = 0
# 					q.append(Vector2i(col, row))
# 	h = 0
# 	while h < q.size():
# 		var pos: Vector2i = q[h]; h += 1
# 		for n in _neighbors(pos):
# 			if drain_depth[n.y][n.x] != -1: continue
# 			if GameState.tiles[n.y][n.x] != GameState.TileType.RIVER or connected[n.y][n.x]: continue
# 			drain_depth[n.y][n.x] = drain_depth[pos.y][pos.x] + 1
# 			q.append(n)

# 	# Schedule drains and immediately mark as CHANNEL so pass 3 ignores them
# 	for row in range(GameState.ROWS):
# 		for col in range(GameState.COLS):
# 			if GameState.tiles[row][col] == GameState.TileType.RIVER and not connected[row][col]:
# 				var d: int = maxi(drain_depth[row][col], 0)
# 				GameState._pending_fills.append([col, row, d * GameState.FILL_DELAY_PER_STEP, false])
# 				GameState.tiles[row][col] = GameState.TileType.CHANNEL

# 	recompute_flow()

# 	if not schedule_fills:
# 		return

# 	# --- Pass 3: fill — BFS from live RIVER into CHANNEL ---
# 	var fill_depth: Array = _make_grid(-1)
# 	q = []
# 	for row in range(GameState.ROWS):
# 		for col in range(GameState.COLS):
# 			if GameState.tiles[row][col] == GameState.TileType.RIVER:
# 				fill_depth[row][col] = 0
# 				q.append(Vector2i(col, row))
# 	h = 0
# 	while h < q.size():
# 		var pos: Vector2i = q[h]; h += 1
# 		for n in _neighbors(pos):
# 			if fill_depth[n.y][n.x] != -1: continue
# 			if GameState.tiles[n.y][n.x] != GameState.TileType.CHANNEL: continue
# 			fill_depth[n.y][n.x] = fill_depth[pos.y][pos.x] + 1
# 			q.append(n)
# 	for row in range(GameState.ROWS):
# 		for col in range(GameState.COLS):
# 			if GameState.tiles[row][col] == GameState.TileType.CHANNEL and fill_depth[row][col] != -1:
# 				GameState._pending_fills.append([col, row, fill_depth[row][col] * GameState.FILL_DELAY_PER_STEP, true])

# # Advances fill/drain timers. Returns true if any CHANNEL tile became RIVER this tick.
# func tick_fills(delta: float) -> bool:
# 	var any_filled: bool = false
# 	var i: int = GameState._pending_fills.size() - 1
# 	while i >= 0:
# 		GameState._pending_fills[i][2] -= delta
# 		if GameState._pending_fills[i][2] <= 0.0:
# 			var c: int = GameState._pending_fills[i][0]
# 			var r: int = GameState._pending_fills[i][1]
# 			var is_fill: bool = GameState._pending_fills[i][3]
# 			GameState._pending_fills.remove_at(i)
# 			if is_fill and GameState.tiles[r][c] == GameState.TileType.CHANNEL:
# 				GameState.tiles[r][c] = GameState.TileType.RIVER
# 				GameState.tile_gold[r][c] = 0.0
# 				GameState.tile_changed.emit(c, r)
# 				any_filled = true
# 			elif not is_fill and GameState.tiles[r][c] == GameState.TileType.CHANNEL:
# 				GameState.tile_changed.emit(c, r)
# 		i -= 1
# 	return any_filled

# func recompute_flow() -> void:
# 	# Reset all parents — rebuild from scratch each time topology changes
# 	GameState.tile_flow_parent = _make_parent_grid()
# 	# Seed col-0 RIVER tiles as permanent sources
# 	for row in range(GameState.ROWS):
# 		if GameState.tiles[row][0] == GameState.TileType.RIVER:
# 			GameState.tile_flow_parent[row][0] = [_FlowSolver.PARENT_SOURCE]
# 	# Step until stable
# 	var solver = _FlowSolver.new()
# 	var result: Dictionary
# 	while true:
# 		result = solver.compute(GameState.tiles, GameState.tile_flow_parent)
# 		GameState.tile_flow_parent = result.parents
# 		if not result.is_changed:
# 			break
# 	GameState.tile_flow_values = result.flow_values
# 	GameState.tile_flow_dir = result.flow_dir
# 	GameState.tile_bfs_depth = result.bfs_depth
# 	_update_river_speed(result.connected_count)
# 	GameState.flow_changed.emit()

# func _update_river_speed(connected_count: int) -> void:
# 	var new_speed: float = clampf(
# 		float(GameState.BASE_SPEED_TILES) / maxf(connected_count, 1), 0.1, 2.0
# 	)
# 	if abs(new_speed - GameState.river_speed) > 0.005:
# 		GameState.river_speed = new_speed
# 		GameState.speed_changed.emit(GameState.river_speed)

# func _neighbors(pos: Vector2i) -> Array:
# 	var result: Array = []
# 	for n in [Vector2i(pos.x + 1, pos.y), Vector2i(pos.x - 1, pos.y),
# 			  Vector2i(pos.x, pos.y + 1), Vector2i(pos.x, pos.y - 1)]:
# 		if n.x >= 0 and n.x < GameState.COLS and n.y >= 0 and n.y < GameState.ROWS:
# 			result.append(n)
# 	return result

func _make_grid(default_value) -> Array:
	var grid: Array = []
	for _row in range(GameState.ROWS):
		var r: Array = []
		for _col in range(GameState.COLS):
			r.append(default_value)
		grid.append(r)
	return grid

func _make_parent_grid() -> Array:
	var grid: Array = []
	for _row in range(GameState.ROWS):
		var r: Array = []
		for _col in range(GameState.COLS):
			r.append([]) # each cell gets its own empty Array
		grid.append(r)
	return grid
