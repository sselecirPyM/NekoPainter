struct Tile
{
	float4 color[8][8];
};

Texture2D<float4> Source : register(t0);
uniform StructuredBuffer <uint2> TBuf1 : register(t1);
uniform RWStructuredBuffer <Tile> TBuf0 : register(u0);

[numthreads(8, 8, 16)]
void main( uint3 id : SV_DispatchThreadID )
{
	uint count;
	uint stride;
	TBuf1.GetDimensions(count, stride);
	if (id.z < count)
	{
		TBuf0[id.z].color[id.x][id.y] = Source[id.xy + TBuf1[id.z]];
	}
}