shader_type canvas_item;

uniform vec4 tint : source_color = vec4(0.32, 0.22, 0.12, 1.0);

void fragment() {
	vec2 w = FRAGCOORD.xy / 60.0;

	float n1 = fract(sin(dot(floor(w * 1.4), vec2(127.1, 311.7))) * 43758.5453);
	float n2 = fract(sin(dot(floor(w * 3.8), vec2(127.1, 311.7))) * 43758.5453);
	float noise = n1 * 0.65 + n2 * 0.35;

	vec4 dark  = vec4(tint.rgb * 0.72, 1.0);
	vec4 light = vec4(min(tint.rgb * 1.28, vec3(1.0)), 1.0);

	COLOR = mix(dark, light, noise * 0.45) * COLOR;
}
