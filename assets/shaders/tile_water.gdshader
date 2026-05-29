shader_type canvas_item;

uniform float flow_speed : hint_range(0.0, 3.0) = 1.0;
uniform vec2  flow_dir = vec2(1.0, 0.0);
uniform float north : hint_range(0.0, 1.0) = 0.0;
uniform float south : hint_range(0.0, 1.0) = 0.0;
uniform float east  : hint_range(0.0, 1.0) = 0.0;
uniform float west  : hint_range(0.0, 1.0) = 0.0;
uniform vec4  soil_tint : source_color = vec4(0.32, 0.22, 0.12, 1.0);

bool in_arrow(vec2 uv, vec2 fd) {
	float rx = dot(uv, fd);
	float ry = dot(uv, vec2(-fd.y, fd.x));
	bool shaft = rx > 0.28 && rx < 0.37 && abs(ry) < 0.03;
	bool head  = rx > 0.35 && rx < 0.46 && abs(ry) < (0.46 - rx) * 0.9;
	return shaft || head;
}

void fragment() {
	vec2 uv = UV - vec2(0.5);

	// Empty channel look (0 flow)
	vec2 w = FRAGCOORD.xy / 60.0;
	float n1 = fract(sin(dot(floor(w * 1.4), vec2(127.1, 311.7))) * 43758.5453);
	float n2 = fract(sin(dot(floor(w * 3.8), vec2(127.1, 311.7))) * 43758.5453);
	vec4 channel = mix(vec4(0.12, 0.14, 0.18, 1.0), vec4(0.20, 0.23, 0.28, 1.0), (n1 * 0.7 + n2 * 0.3) * 0.6);

	// Flowing water look
	bool is_arrow = false;
	if (north > 0.5) is_arrow = is_arrow || in_arrow(uv, vec2( 0.0, -1.0));
	if (south > 0.5) is_arrow = is_arrow || in_arrow(uv, vec2( 0.0,  1.0));
	if (east  > 0.5) is_arrow = is_arrow || in_arrow(uv, vec2( 1.0,  0.0));
	if (west  > 0.5) is_arrow = is_arrow || in_arrow(uv, vec2(-1.0,  0.0));
	vec4 flowing = is_arrow ? vec4(1.0, 1.0, 1.0, 1.0) : vec4(0.12, 0.42, 0.80, 1.0);

	COLOR = mix(channel, flowing, step(0.001, flow_speed)) * COLOR;
}
