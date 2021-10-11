


void task(int3 id)
{
	float4 color1 = tex1[id.xy];
	float4 color2 = tex0[id.xy];
	tex0[id.xy] = float4((color2 * (1 - color1.a) + color1 * color1.a).rgb, 1 - (1 - color2.a) * (1 - color1.a));
}