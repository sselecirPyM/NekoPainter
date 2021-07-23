Texture2D<float4> TexIn :register(t0);
RWTexture2D<float4> TexOut : register(u0);
[numthreads(32, 32, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
	uint width;
	uint height;
	TexIn.GetDimensions(width, height);
	if (id.x < width && id.y < height)
	{
		float aalpha = TexIn[uint2(id.x, id.y)].a;
		if (abs(aalpha - 1.0f) < 0.5 / 255.0)
		{
			aalpha = 1;
		}
		if (abs(aalpha) < 0.5 / 255.0)
		{
			aalpha = 0;
		}
		TexOut[id.xy] = float4(TexIn[uint2(id.x, id.y)].rgb, aalpha);
	}
}