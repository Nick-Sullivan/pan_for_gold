shader_type canvas_item;

void fragment() {
	vec2 w = FRAGCOORD.xy / 60.0;

	// Large patches + fine grit, both spanning multiple tiles
	float n1 = fract(sin(dot(floor(w * 1.4), vec2(127.1, 311.7))) * 43758.5453);
	float n2 = fract(sin(dot(floor(w * 3.8), vec2(127.1, 311.7))) * 43758.5453);

	float noise = n1 * 0.7 + n2 * 0.3;

	vec4 dark  = vec4(0.12, 0.14, 0.18, 1.0);
	vec4 light = vec4(0.20, 0.23, 0.28, 1.0);

	COLOR = mix(dark, light, noise * 0.6) * COLOR;
}
