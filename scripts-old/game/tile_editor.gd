extends RefCounted

func can_dig(col: int, row: int) -> bool:
	return GameState.shovels > 0 and GameState.tiles[row][col] != GameState.TileType.STONE

func dig(col: int, row: int) -> void:
	match GameState.tiles[row][col]:
		GameState.TileType.SOIL:
			set_type(col, row, GameState.TileType.CHANNEL)
		GameState.TileType.RIVER, GameState.TileType.CHANNEL:
			set_type(col, row, GameState.TileType.BANK)
		GameState.TileType.BANK:
			set_type(col, row, GameState.TileType.SOIL)

func set_type(col: int, row: int, new_type: int) -> void:
	GameState.tiles[row][col] = new_type
	GameState.tile_gold[row][col] = 0.0
	GameState.tile_changed.emit(col, row)
