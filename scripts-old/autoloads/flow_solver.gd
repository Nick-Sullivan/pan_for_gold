extends RefCounted

const TILE_RIVER: int = 2

# Sentinel stored as a parent entry for col-0 source tiles.
# Means "water enters from the left edge of the map."
const PARENT_SOURCE: Vector2i = Vector2i(-1, -1)

# Each tile's parent list is an Array[Vector2i]:
#   []               → no flow
#   [PARENT_SOURCE]  → col-0 source tile (seeded externally)
#   [p, ...]         → one or more upstream tile positions (joins allowed)

# Advances flow ONE step — both fill and drain.
#
# Fill rule  (tile currently has no parents):
#   Gain ALL flowing RIVER neighbours as parents in one tick.
#   This correctly handles confluences: both upstream branches become parents.
#
# Drain rule (tile currently has parents):
#   Drop any parent whose own parent list was empty in source_parents (it lost flow last tick).
#   If the list empties, the tile loses flow this tick.
#   Processing from OLD state means drain propagates one tile per tick — natural left-to-right fade.
#
# No-cycle guarantee:
#   A tile that already has parents never gains new ones.
#   So a downstream tile never becomes a parent of its upstream neighbour.
#
# Returns:
#   parents        : updated 2D Array of Array[Vector2i]
#   flow_values    : derived — 1000 where parents non-empty, else 0
#   flow_dir       : derived — mean upstream direction vector per tile
#   connected_count: tiles with flow > 0
#   is_changed     : false when nothing moved (steady state)
func compute(tiles: Array, current_flow: Array, source_parents: Array) -> Dictionary:
	var rows: int = tiles.size()
	var cols: int = tiles[0].size() if rows > 0 else 0
	var new_parents: Array = _make_parent_grid(rows, cols)
	var flow_values: Array = _make_grid(0, rows, cols)
	var flow_dir: Array = _make_grid(Vector2(1, 0), rows, cols)
	var is_changed: bool = false
	var connected_count: int = 0

	for row in range(rows):
		for col in range(cols):
			if tiles[row][col] != TILE_RIVER:
				continue

			var old_ps: Array = source_parents[row][col]
			var result_ps: Array = []

			if old_ps.size() > 0:
				# DRAIN — keep only parents that still had flow in the old state
				for p in old_ps:
					if p == PARENT_SOURCE:
						result_ps.append(p) # removed externally; keep unless gone
					elif source_parents[p.y][p.x].size() > 0:
						result_ps.append(p) # parent still had flow
			else:
				# FILL — gain ALL flowing RIVER neighbours as parents (handles confluences)
				for n in _neighbors(Vector2i(col, row), rows, cols):
					if tiles[n.y][n.x] == TILE_RIVER and source_parents[n.y][n.x].size() > 0:
						result_ps.append(Vector2i(n.x, n.y))

			new_parents[row][col] = result_ps

			if result_ps != old_ps:
				is_changed = true

			if result_ps.size() > 0:
				flow_values[row][col] = 1000
				connected_count += 1
				flow_dir[row][col] = _mean_upstream_dir(col, row, result_ps)

	return {
		"parents": new_parents,
		"flow_values": flow_values,
		"flow_dir": flow_dir,
		"bfs_depth": _make_grid(0.0, rows, cols),
		"connected_count": connected_count,
		"is_changed": is_changed,
	}

func _mean_upstream_dir(col: int, row: int, parents: Array) -> Vector2:
	if parents.is_empty():
		return Vector2(1, 0)
	var sum: Vector2 = Vector2.ZERO
	for p in parents:
		if p == PARENT_SOURCE:
			sum += Vector2(1, 0) # water enters from the left
		else:
			sum += Vector2(col - p.x, row - p.y)
	return sum.normalized() if sum.length_squared() > 0.001 else Vector2(1, 0)

func _neighbors(pos: Vector2i, rows: int, cols: int) -> Array:
	var result: Array = []
	for n in [Vector2i(pos.x + 1, pos.y), Vector2i(pos.x - 1, pos.y),
			  Vector2i(pos.x, pos.y + 1), Vector2i(pos.x, pos.y - 1)]:
		if n.x >= 0 and n.x < cols and n.y >= 0 and n.y < rows:
			result.append(n)
	return result

func _make_parent_grid(rows: int, cols: int) -> Array:
	var grid: Array = []
	for _row in range(rows):
		var r: Array = []
		for _col in range(cols):
			r.append([]) # each cell gets its own empty Array
		grid.append(r)
	return grid

func _make_grid(default_value, rows: int, cols: int) -> Array:
	var grid: Array = []
	for _row in range(rows):
		var r: Array = []
		for _col in range(cols):
			r.append(default_value)
		grid.append(r)
	return grid
