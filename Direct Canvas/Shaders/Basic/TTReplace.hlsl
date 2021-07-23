const uint c_separationNum = 0x40000000;
struct Tile
{
	float4 color[8][8];
};

uniform StructuredBuffer <Tile> TBuf0 : register(t0);
uniform StructuredBuffer <Tile> TBuf1 : register(t1);
uniform StructuredBuffer <uint> TBufData : register(t2);
uniform RWStructuredBuffer <Tile> TBufOut : register(u0);

[numthreads(8, 8, 16)]
void main(uint3 id : SV_DispatchThreadID )
{
	uint count;
	uint stride;
	TBufOut.GetDimensions(count, stride);
	if (id.z < count)
	{
		if (TBufData[id.z] < c_separationNum)
		{
			TBufOut[id.z].color[id.x][id.y] = TBuf0[TBufData[id.z]].color[id.x][id.y];
		}
		else
		{
			TBufOut[id.z].color[id.x][id.y] = TBuf1[TBufData[id.z] - c_separationNum].color[id.x][id.y];
		}
	}
}