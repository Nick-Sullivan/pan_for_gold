shader_type canvas_item;

uniform float gold_ratio : hint_range(0.0, 1.0) = 0.5;

void fragment() {
	vec2 w = FRAGCOORD.xy / 60.0;

	// Static soil texture — no TIME, no movement
	float n1 = fract(sin(dot(floor(w * 1.4), vec2(127.1, 311.7))) * 43758.5453);
	float n2 = fract(sin(dot(floor(w * 3.8), vec2(127.1, 311.7))) * 43758.5453);
	float noise = n1 * 0.6 + n2 * 0.4;

	vec4 base = vec4(0.62, 0.52, 0.36, 1.0);
	base.rgb += noise * 0.06 - 0.03;

	// Subtle gold specs — fixed world positions, more appear as gold_ratio grows
	vec2 cell = floor(w * 5.0);
	vec2 cell_uv = fract(w * 5.0);
	float cell_hash = fract(sin(dot(cell, vec2(127.1, 311.7))) * 43758.5453);

	float dist = length(cell_uv - 0.5);
	float dot_mask = step(dist, 0.18);
	float show = step(cell_hash, gold_ratio * 0.28) * dot_mask;

	base = mix(base, vec4(0.88, 0.78, 0.42, 1.0), show * 0.55);

	COLOR = base * COLOR;
}
