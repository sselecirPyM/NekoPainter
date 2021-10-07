


void task(int3 id)
{
	//tex0[id.xy] = color;
	float4 color1 = tex1[id.xy];
	color1.a = clamp(color1.a - threshold, 0, 1);
	if (color1.a < 0.01f)return;
	float4 color2 = tex0[id.xy];
	tex0[id.xy] = float4((color2 * (1 - color1.a) + color1 * color1.a).rgb, 1 - (1 - color2.a) * (1 - color1.a));
}