struct Tile
{
	float4 color[8][8];
};

Texture2D<float4> TTexIn : register(t0);
uniform RWStructuredBuffer<uint> TBufOut : register(u0);
[numthreads(32, 32, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
	uint width;
	uint height;
	TTexIn.GetDimensions(width, height);
	uint index = (id.x / 8) + (id.y / 8)*(width / 8 + (((width % 8) > 0) ? 1 : 0));
	if (id.x < width&&id.y < height)
	{
		if (TTexIn[id.xy].a > 0.00001f)
		{
			TBufOut[index] = 1;
		}
	}
}