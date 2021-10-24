


void task(int3 id)
{
	float2 uv = (-targetRect.xy + (float2)id.xy+float2(0.5,0.5)) / targetRect.zw;
	float2 uv1 = (uv * sourceRect.zw + sourceRect.xy) / NPGetDimensions(tex1).xy;

	tex0[id.xy] = tex1.SampleLevel(sampler1, uv1, 0);
	//tex0[id.xy] = tex1[id.xy];
}