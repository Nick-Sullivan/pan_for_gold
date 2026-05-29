extends RefCounted

func pan(col: int, row: int) -> void:
	var amount: int = int(GameState.tile_gold[row][col])
	GameState.tile_gold[row][col] = 0.0
	GameState.tile_gold_changed.emit(col, row, 0)
	if amount > 0:
		earn(amount)

func earn(amount: int) -> void:
	GameState.gold += amount
	GameState.gold_changed.emit(GameState.gold)

func buy_shovel() -> void:
	if GameState.gold < GameState.SHOVEL_COST:
		return
	GameState.gold -= GameState.SHOVEL_COST
	GameState.gold_changed.emit(GameState.gold)
	GameState.shovels += 1
	GameState.shovels_changed.emit(GameState.shovels)

func tick_gold(delta: float) -> void:
	for region_idx in range(GameState._region_data.size()):
		var rt: Array = GameState._region_data[region_idx]["tiles"]
		var rg: Array = GameState._region_data[region_idx]["tile_gold"]
		var is_active: bool = region_idx == GameState.current_region
		for row in range(GameState.ROWS):
			for col in range(GameState.COLS):
				if rt[row][col] != GameState.TileType.BANK:
					continue
				if not _adj_river(col, row, rt):
					continue
				var old_int: int = int(rg[row][col])
				var rate: float = GameState.river_speed if is_active else 1.0
				rg[row][col] = minf(
					rg[row][col] + delta * rate / GameState.REFILL_TIME * GameState.MAX_TILE_GOLD,
					GameState.MAX_TILE_GOLD
				)
				var new_int: int = int(rg[row][col])
				if new_int != old_int and is_active:
					GameState.tile_gold_changed.emit(col, row, new_int)

func _adj_river(col: int, row: int, rt: Array) -> bool:
	for n in [Vector2i(col + 1, row), Vector2i(col - 1, row),
			  Vector2i(col, row + 1), Vector2i(col, row - 1)]:
		if n.x < 0 or n.x >= GameState.COLS or n.y < 0 or n.y >= GameState.ROWS:
			continue
		if rt[n.y][n.x] == GameState.TileType.RIVER:
			return true
	return false
