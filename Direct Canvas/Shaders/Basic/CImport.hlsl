Texture2D<float4> TexIn :register(t0);
RWTexture2D<float4> TexOut : register(u0);
[numthreads(32, 32, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
	uint width;
	uint height;
	TexIn.GetDimensions(width, height);
	uint width2;
	uint height2;
	TexOut.GetDimensions(width2, height2);
	if (id.x < width&&id.y < height&&id.x < width2&&id.y < height2)
	{
		TexOut[id.xy] = float4(TexIn[uint2(id.x, id.y)].rgb, TexIn[uint2(id.x,id.y)].a);
	}
}