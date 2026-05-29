class_name RiverDAG
extends DAG

func _coord_to_id(xy: Vector2i) -> String:
    return str(xy.x) + "," + str(xy.y)

func add_node(xy: Vector2i, value = null) -> void:
    .add_node(_coord_to_id(xy), value)

func add_edge(xy1: Vector2i, xy2: Vector2i) -> void:
    .add_edge(_coord_to_id(xy1), _coord_to_id(xy2))

func bfs(xy1: Vector2i) -> Array:
    .bfs(_coord_to_id(xy1))
