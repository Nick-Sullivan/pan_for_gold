extends RefCounted

func init() -> void:
	GameState.current_region  = 0
	GameState.unlocked_regions = 1

# Commit all pending fills instantly (called before leaving a region so the tile
# array is clean when we return to it).
func commit_pending_fills() -> void:
	for entry in GameState._pending_fills:
		var c: int     = entry[0]
		var r: int     = entry[1]
		var is_fill: bool = entry[3]
		if is_fill and GameState.tiles[r][c] == GameState.TileType.CHANNEL:
			GameState.tiles[r][c]     = GameState.TileType.RIVER
			GameState.tile_gold[r][c] = 0.0
		# Drain entries: tile is already CHANNEL — nothing more needed
	GameState._pending_fills.clear()

func switch_to(index: int) -> void:
	if index < 0 or index >= GameState.unlocked_regions or index == GameState.current_region:
		return
	commit_pending_fills()
	GameState.current_region = index
	GameState.tiles          = GameState._region_data[index]["tiles"]
	GameState.tile_gold      = GameState._region_data[index]["tile_gold"]
	GameState.region_switched.emit(index)

# Check if river reaches the right edge; if so, create and register the next region.
func try_unlock() -> void:
	if GameState.unlocked_regions > GameState.current_region + 1:
		return
	var exit_rows: Array = []
	for row in range(GameState.ROWS):
		if GameState.tiles[row][GameState.COLS - 1] == GameState.TileType.RIVER:
			exit_rows.append(row)
	if exit_rows.is_empty():
		return
	_create_new_region(exit_rows)
	GameState.unlocked_regions += 1
	GameState.region_unlocked.emit(GameState.unlocked_regions)

# Keep the next region's col-0 entry tiles in sync with the current region's right-edge exits.
func sync_next_entries() -> void:
	var next_idx: int = GameState.current_region + 1
	if next_idx >= GameState._region_data.size():
		return
	var next_tiles: Array = GameState._region_data[next_idx]["tiles"]
	var exit_rows: Array = []
	for row in range(GameState.ROWS):
		if GameState.tiles[row][GameState.COLS - 1] == GameState.TileType.RIVER:
			exit_rows.append(row)
	for row in range(GameState.ROWS):
		var should_be_river: bool = row in exit_rows
		var is_river: bool = next_tiles[row][0] == GameState.TileType.RIVER
		if should_be_river and not is_river:
			next_tiles[row][0] = GameState.TileType.RIVER
		elif not should_be_river and is_river:
			next_tiles[row][0] = GameState.TileType.CHANNEL

func get_tiles(index: int) -> Array:
	if index < 0 or index >= GameState._region_data.size():
		return []
	return GameState._region_data[index]["tiles"]

func _create_new_region(exit_rows: Array) -> void:
	var new_tiles: Array = []
	var new_gold: Array  = []
	for row in range(GameState.ROWS):
		var tile_row: Array = []
		var gold_row: Array = []
		for col in range(GameState.COLS):
			var t: int = GameState.TileType.RIVER if (col == 0 and row in exit_rows) else GameState.TileType.SOIL
			tile_row.append(t)
			gold_row.append(0.0)
		new_tiles.append(tile_row)
		new_gold.append(gold_row)
	GameState._region_data.append({"tiles": new_tiles, "tile_gold": new_gold})
