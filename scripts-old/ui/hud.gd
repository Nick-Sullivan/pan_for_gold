extends CanvasLayer

signal buy_shovel_requested
signal tool_selected(tool: int)
signal region_selected(index: int)

const MHW: int = 22  # minimap diamond half-width
const MHH: int = 11  # minimap diamond half-height

var _gold_label:    Label
var _buy_button:       Button
var _shop_empty_label: Label
var _tool_pan_btn:  Button
var _tool_shovel_btn: Button
var _map_container: Control
var _region_diamonds: Array = []  # Polygon2D per region

var _tab_contents: Array = []  # [equip, shop, map]
var _tab_buttons:  Array = []
var _active_tab:   int   = 0

func _ready() -> void:
	add_to_group("hud")
	_build_ui()
	GameState.gold_changed.connect(_on_gold_changed)
	GameState.shovels_changed.connect(_on_shovels_changed)
	GameState.tool_changed.connect(_on_tool_changed)
	GameState.region_unlocked.connect(_on_region_unlocked)
	GameState.region_switched.connect(_on_region_switched)

func _input(event: InputEvent) -> void:
	if event is InputEventKey and (event as InputEventKey).pressed and not (event as InputEventKey).echo:
		match (event as InputEventKey).keycode:
			KEY_1: _switch_tab(0)
			KEY_2: _switch_tab(1)
			KEY_3: _switch_tab(2)

func _switch_tab(index: int) -> void:
	_active_tab = index
	for i in range(_tab_contents.size()):
		_tab_contents[i].visible       = i == index
		_tab_buttons[i].button_pressed = i == index

func _build_ui() -> void:
	var panel: PanelContainer = PanelContainer.new()
	panel.position = Vector2(950, 10)
	var style: StyleBoxFlat = StyleBoxFlat.new()
	style.bg_color = Color(0.10, 0.10, 0.12, 1.0)
	style.set_corner_radius_all(6)
	panel.add_theme_stylebox_override("panel", style)
	add_child(panel)

	var margin: MarginContainer = MarginContainer.new()
	margin.add_theme_constant_override("margin_left",   12)
	margin.add_theme_constant_override("margin_right",  12)
	margin.add_theme_constant_override("margin_top",    12)
	margin.add_theme_constant_override("margin_bottom", 12)
	panel.add_child(margin)

	var vbox: VBoxContainer = VBoxContainer.new()
	vbox.custom_minimum_size = Vector2(290, 0)
	vbox.add_theme_constant_override("separation", 6)
	margin.add_child(vbox)

	# Stats — always visible
	_gold_label = Label.new()
	_gold_label.text = "Gold: 0"
	_gold_label.add_theme_font_size_override("font_size", 24)
	vbox.add_child(_gold_label)

	vbox.add_child(HSeparator.new())

	# Tab bar
	var tab_hbox: HBoxContainer = HBoxContainer.new()
	tab_hbox.add_theme_constant_override("separation", 4)
	vbox.add_child(tab_hbox)

	var tab_group: ButtonGroup = ButtonGroup.new()
	var tab_labels: Array      = ["Equip", "Shop", "Map"]
	for i in range(3):
		var btn: Button = Button.new()
		btn.text              = tab_labels[i]
		btn.toggle_mode       = true
		btn.button_group      = tab_group
		btn.button_pressed    = i == 0
		btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		var idx: int = i
		btn.pressed.connect(func(): _switch_tab(idx))
		tab_hbox.add_child(btn)
		_tab_buttons.append(btn)

	# Tab 1 — Equipment
	var equip_box: VBoxContainer = VBoxContainer.new()
	equip_box.add_theme_constant_override("separation", 6)
	vbox.add_child(equip_box)
	_tab_contents.append(equip_box)

	var tool_hbox: HBoxContainer = HBoxContainer.new()
	tool_hbox.add_theme_constant_override("separation", 6)
	equip_box.add_child(tool_hbox)

	var tool_group: ButtonGroup = ButtonGroup.new()

	_tool_pan_btn = Button.new()
	_tool_pan_btn.text = "Pan"
	_tool_pan_btn.custom_minimum_size = Vector2(80, 48)
	_tool_pan_btn.toggle_mode    = true
	_tool_pan_btn.button_pressed = true
	_tool_pan_btn.button_group   = tool_group
	_tool_pan_btn.pressed.connect(func(): tool_selected.emit(GameState.ActiveTool.PAN))
	tool_hbox.add_child(_tool_pan_btn)

	_tool_shovel_btn = Button.new()
	_tool_shovel_btn.text = "Shovel"
	_tool_shovel_btn.custom_minimum_size = Vector2(80, 48)
	_tool_shovel_btn.toggle_mode = true
	_tool_shovel_btn.button_group = tool_group
	_tool_shovel_btn.disabled    = true
	_tool_shovel_btn.visible     = false
	_tool_shovel_btn.pressed.connect(func(): tool_selected.emit(GameState.ActiveTool.SHOVEL))
	tool_hbox.add_child(_tool_shovel_btn)

	# Tab 2 — Shop
	var shop_box: VBoxContainer = VBoxContainer.new()
	shop_box.add_theme_constant_override("separation", 6)
	shop_box.visible = false
	vbox.add_child(shop_box)
	_tab_contents.append(shop_box)

	_buy_button = Button.new()
	_buy_button.text = "Buy Shovel (%dg)" % GameState.SHOVEL_COST
	_buy_button.custom_minimum_size = Vector2(0, 44)
	_buy_button.disabled = true
	_buy_button.pressed.connect(func(): buy_shovel_requested.emit())
	shop_box.add_child(_buy_button)

	_shop_empty_label = Label.new()
	_shop_empty_label.text = "No items available"
	_shop_empty_label.add_theme_color_override("font_color", Color(0.5, 0.5, 0.5))
	_shop_empty_label.visible = false
	shop_box.add_child(_shop_empty_label)

	# Tab 3 — Map
	var map_box: VBoxContainer = VBoxContainer.new()
	map_box.add_theme_constant_override("separation", 6)
	map_box.visible = false
	vbox.add_child(map_box)
	_tab_contents.append(map_box)

	_map_container = Control.new()
	_map_container.custom_minimum_size = Vector2(290, MHH * 2 + 4)
	_map_container.mouse_filter = Control.MOUSE_FILTER_STOP
	_map_container.gui_input.connect(_on_map_input)
	map_box.add_child(_map_container)

	_add_region_diamond(0)

func _diamond_at(index: int) -> PackedVector2Array:
	var cx: float = MHW + index * MHW
	var cy: float = MHH + index * MHH
	return PackedVector2Array([
		Vector2(cx,        cy - MHH),
		Vector2(cx + MHW,  cy),
		Vector2(cx,        cy + MHH),
		Vector2(cx - MHW,  cy),
	])

func _add_region_diamond(index: int) -> void:
	var poly: Polygon2D = Polygon2D.new()
	poly.polygon = _diamond_at(index)
	poly.color   = _diamond_color(index)
	_map_container.add_child(poly)
	_region_diamonds.append(poly)
	_map_container.custom_minimum_size = Vector2((index + 2) * MHW, (index + 2) * MHH)

func _diamond_color(index: int) -> Color:
	return Color(0.75, 0.60, 0.30) if index == GameState.current_region else Color(0.35, 0.28, 0.18)

func _update_region_diamonds() -> void:
	for i in range(_region_diamonds.size()):
		_region_diamonds[i].color = _diamond_color(i)

func _on_map_input(event: InputEvent) -> void:
	if not event is InputEventMouseButton:
		return
	if not (event as InputEventMouseButton).pressed:
		return
	if (event as InputEventMouseButton).button_index != MOUSE_BUTTON_LEFT:
		return
	var pos: Vector2 = (event as InputEventMouseButton).position
	for i in range(_region_diamonds.size()):
		if Geometry2D.is_point_in_polygon(pos, _region_diamonds[i].polygon):
			region_selected.emit(i)
			return

func _on_gold_changed(new_value: int) -> void:
	_gold_label.text = "Gold: %d" % new_value
	_buy_button.disabled = new_value < GameState.SHOVEL_COST

func _on_shovels_changed(new_value: int) -> void:
	_tool_shovel_btn.visible   = new_value > 0
	_tool_shovel_btn.disabled  = new_value == 0
	_buy_button.visible        = new_value == 0
	_shop_empty_label.visible  = new_value > 0

func _on_tool_changed(tool: int) -> void:
	_tool_pan_btn.button_pressed    = tool == GameState.ActiveTool.PAN
	_tool_shovel_btn.button_pressed = tool == GameState.ActiveTool.SHOVEL

func _on_region_unlocked(count: int) -> void:
	_add_region_diamond(count - 1)

func _on_region_switched(_index: int) -> void:
	_update_region_diamonds()
