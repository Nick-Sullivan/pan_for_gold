class_name DAGNode

var id: String
var value
var parents: Array[String] = []
var children: Array[String] = []

func _init(_id: String, _value = null):
    id = _id
    value = _value
