extends Node2D

const GridScript = preload("res://scripts/overworld/Grid.cs")
const HudScript = preload("res://scripts/ui/HUD.cs")

func _ready() -> void:
	var grid: Node2D = GridScript.new()
	add_child(grid)

	var hud: CanvasLayer = HudScript.new()
	add_child(hud)

	if "--screenshot" in OS.get_cmdline_user_args():
		_take_screenshot()

func _take_screenshot() -> void:
	await get_tree().process_frame
	await get_tree().process_frame
	var img = get_viewport().get_texture().get_image()
	img.save_png("screenshot.png")
	get_tree().quit()
