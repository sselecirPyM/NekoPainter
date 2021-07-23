struct Tile
{
	float4 color[8][8];
};

uniform StructuredBuffer <Tile> TBuf0 : register(t0);
uniform StructuredBuffer <uint2> TBuf1 : register(t1);
uniform RWTexture2D<float4> Target : register(u0);

[numthreads(8, 8, 16)]
void main(uint3 id : SV_DispatchThreadID)
{
	uint count;
	uint stride;
	TBuf0.GetDimensions(count, stride);
	if (id.z < count)
	{
		Target[id.xy + TBuf1[id.z]] = TBuf0[id.z].color[id.x][id.y];
	}
}