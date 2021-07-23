const uint c_separationNum = 0x40000000;
struct Tile
{
	float4 color[8][8];
};

uniform StructuredBuffer <Tile> TBuf0 : register(t0);
uniform StructuredBuffer <uint> TBufIndicate : register(t1);
uniform RWStructuredBuffer <Tile> TBufOut : register(u0);

[numthreads(8, 8, 16)]
void main(uint3 id : SV_DispatchThreadID)
{
	uint count;
	uint stride;
	TBufOut.GetDimensions(count, stride);
	if (id.z < count)
	{
		if (TBufIndicate[id.z] < c_separationNum)
		{
			TBufOut[id.z].color[id.x][id.y] = TBuf0[TBufIndicate[id.z]].color[id.x][id.y];
		}
		else
		{
			//TBufOut[id.z].color[id.x][id.y] = float4(0.0f, 0.0f, 0.0f, 0.0f);
		}
	}
}