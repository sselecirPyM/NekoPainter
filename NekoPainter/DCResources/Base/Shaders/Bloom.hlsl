


void horizontal(int3 id)
{
	float4 color2 = tex0[id.xy];
	float4 color3 = float4(0, 0, 0, 0);
	for (int i = 0; i < radius * 2 + 1; i++)
	{
		int2 id1 = (id.xy - int2(radius - i, 0));
		id1 = clamp(id1, int2(0, 0), NPGetDimensions(tex0.xy) - int2(1, 1));
		float4 color1 = tex1[id1];
		if (color1.r + color1.g + color1.b < threshold * 3)
			color3 += float4(0, 0, 0, color1.a) * weights[i];
		else
			color3 += float4(color1.rgb * color1.a, color1.a) * weights[i];
	}
	color3.rgb /= max(color3.a, 0.0001);
	color3.rgb *= intensity;
	tex0[id.xy] = color3;
}

void vertical(int3 id)
{
	float4 color2 = tex0[id.xy];
	float4 color3 = float4(0, 0, 0, 0);
	for (int i = 0; i < radius * 2 + 1; i++)
	{
		int2 id1 = (id.xy - int2(0, radius - i));
		id1 = clamp(id1, int2(0, 0), NPGetDimensions(tex0.xy) - int2(1, 1));
		float4 color1 = tex1[id1];
		color3 += float4(color1.rgb * color1.a, color1.a) * weights[i];
	}
	color3.rgb /= max(color3.a, 0.0001);
	tex0[id.xy] = float4(color2.rgb + color3.rgb * color3.a, 1 - (1 - color2.a) * (1 - color3.a));
}