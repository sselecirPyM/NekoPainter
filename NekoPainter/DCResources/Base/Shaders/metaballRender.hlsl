


void task(int3 id)
{
	float4 color1 = tex1[id.xy];
	float a1= clamp(color1.a - threshold, 0, 1);
	color1 = color1 / color1.a;
	if (a1 < 0.01f)return;
	float4 color2 = tex0[id.xy];
	tex0[id.xy] = float4((color2 * (1 - a1) + color1 * a1).rgb, 1 - (1 - color2.a) * (1 - a1));
}