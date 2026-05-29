extends RefCounted

const HW: int = 44   # half-width  (diamond is HW*2 wide)
const HH: int = 25   # half-height (diamond is HH*2 tall)
const ORIGIN_X: int = 640
const ORIGIN_Y: int = 16

func tile_top(col: int, row: int) -> Vector2:
	return Vector2(ORIGIN_X + (col - row) * HW, ORIGIN_Y + (col + row) * HH)

func tile_center(col: int, row: int) -> Vector2:
	return tile_top(col, row) + Vector2(0, HH)

func diamond_verts(col: int, row: int) -> PackedVector2Array:
	var t: Vector2 = tile_top(col, row)
	return PackedVector2Array([
		t + Vector2(  0,    0),  # top
		t + Vector2( HW,   HH),  # right
		t + Vector2(  0, HH*2),  # bottom
		t + Vector2(-HW,   HH),  # left
	])

func screen_to_tile(mouse_pos: Vector2) -> Vector2i:
	var sx: float = mouse_pos.x - ORIGIN_X
	var sy: float = mouse_pos.y - ORIGIN_Y
	return Vector2i(
		int(floor((sx / HW + sy / HH) / 2.0)),
		int(floor((sy / HH - sx / HW) / 2.0))
	)

func soil_tint(col: int, row: int) -> Color:
	match _river_distance(col, row):
		1: return Color(0.35, 0.50, 0.20)
		2: return Color(0.22, 0.32, 0.15)
		_: return Color(0.32, 0.22, 0.12)

func wall_color(tile_type: int, col: int, row: int) -> Color:
	var base: Color
	match tile_type:
		GameState.TileType.BANK:    base = Color(0.62, 0.52, 0.36)
		GameState.TileType.RIVER:   base = Color(0.12, 0.42, 0.80)
		GameState.TileType.CHANNEL: base = Color(0.16, 0.18, 0.23)
		GameState.TileType.STONE:   return Color(0, 0, 0, 0)
		_:                          base = soil_tint(col, row)
	return Color(base.r * 0.80, base.g * 0.80, base.b * 0.80)

func preview_color(tile_type: int) -> Color:
	match tile_type:
		GameState.TileType.BANK:    return Color(0.62, 0.52, 0.36, 0.35)
		GameState.TileType.RIVER:   return Color(0.12, 0.42, 0.80, 0.35)
		GameState.TileType.CHANNEL: return Color(0.16, 0.18, 0.23, 0.35)
		GameState.TileType.STONE:   return Color(0.38, 0.36, 0.34, 0.35)
		_:                          return Color(0.32, 0.22, 0.12, 0.35)

func preview_wall_color(tile_type: int) -> Color:
	var c: Color = preview_color(tile_type)
	return Color(c.r * 0.80, c.g * 0.80, c.b * 0.80, c.a)

func _river_distance(col: int, row: int) -> int:
	for n in [Vector2i(col + 1, row), Vector2i(col - 1, row),
			  Vector2i(col, row + 1), Vector2i(col, row - 1)]:
		if n.x < 0 or n.x >= GameState.COLS or n.y < 0 or n.y >= GameState.ROWS:
			continue
		if GameState.tiles[n.y][n.x] == GameState.TileType.RIVER:
			return 1
	for dc in range(-2, 3):
		for dr in range(-2, 3):
			if abs(dc) + abs(dr) != 2:
				continue
			var nc: int = col + dc
			var nr: int = row + dr
			if nc < 0 or nc >= GameState.COLS or nr < 0 or nr >= GameState.ROWS:
				continue
			if GameState.tiles[nr][nc] == GameState.TileType.RIVER:
				return 2
	return 99
