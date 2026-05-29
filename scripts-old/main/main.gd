extends Node2D

const GridScene: PackedScene = preload("res://scenes/overworld/grid.tscn")
const HUDScene: PackedScene = preload("res://scenes/ui/hud.tscn")

func _ready() -> void:
	var grid: Node = GridScene.instantiate()
	add_child(grid)
	var hud: Node = HUDScene.instantiate()
	add_child(hud)
	if "--screenshot" in OS.get_cmdline_user_args():
		_take_screenshot()

func _take_screenshot() -> void:
	# Wait a couple of frames so everything is rendered, then save and quit
	await get_tree().process_frame
	await get_tree().process_frame
	var img: Image = get_viewport().get_texture().get_image()
	img.save_png("screenshot.png")
	get_tree().quit()
