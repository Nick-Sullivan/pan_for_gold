class_name DAG

var DAGNode := load("res://scripts/utils/dag_node.gd")

var nodes: Dictionary = {} # id -> DAGNode

func add_node(id: String, value = null) -> void:
    if nodes.has(id):
        push_error("DAG.add_node: Node '%s' already exists" % id)
        return
    nodes[id] = DAGNode.new(id, value)

func add_edge(parent_id: String, child_id: String) -> void:
    if not nodes.has(parent_id):
        push_error("DAG.add_edge: Parent '%s' does not exist" % parent_id)
        return
    if not nodes.has(child_id):
        push_error("DAG.add_edge: Child '%s' does not exist" % child_id)
        return
    if child_id in nodes[parent_id].children:
        push_error("DAG.add_edge: Edge %s -> %s already exists" % [parent_id, child_id])
        return

    nodes[parent_id].children.append(child_id)
    nodes[child_id].parents.append(parent_id)

func set_node(id: String, value = null) -> void:
    if nodes.has(id):
        nodes[id].value = value
    else:
        nodes[id] = DAGNode.new(id, value)

func set_edge(parent_id: String, child_id: String) -> void:
    if not nodes.has(parent_id):
        nodes[parent_id] = DAGNode.new(parent_id)
    if not nodes.has(child_id):
        nodes[child_id] = DAGNode.new(child_id)

    nodes[parent_id].children.erase(child_id)
    nodes[child_id].parents.erase(parent_id)

    nodes[parent_id].children.append(child_id)
    nodes[child_id].parents.append(parent_id)

func get_node(id: String) -> DAGNode:
    return nodes.get(id, null)

func bfs(start_id: String) -> Array[String]:
    var result: Array[String] = []
    if not nodes.has(start_id):
        return result

    var queue: Array[String] = [start_id]
    var visited: Dictionary = {}

    while queue.size() > 0:
        var current_id = queue.pop_front()
        if visited.has(current_id):
            continue

        visited[current_id] = true
        result.append(current_id)

        for child_id in nodes[current_id].children:
            if not visited.has(child_id):
                queue.append(child_id)

    return result

# func copy() -> DAG:
#     var new_dag := DAG.new()
#     for id in nodes.keys():
#         var old_node: DAGNode = nodes[id]
#         var new_node := DAGNode.new(old_node.id, old_node.value)
#         new_node.parents = old_node.parents.duplicate()
#         new_node.children = old_node.children.duplicate()
#         new_dag.nodes[id] = new_node

#     return new_dag
