uniform StructuredBuffer <uint2> TBuf0 : register(t0);
RWTexture2D<float4> Target : register(u0);
[numthreads(8, 8, 16)]
void main(uint3 id : SV_DispatchThreadID)
{
	uint count;
	uint stride;
	TBuf0.GetDimensions(count, stride);
	if (id.z < count)
	{
		Target[id.xy + TBuf0[id.z]] = float4(0.0f, 0.0f, 0.0f, 0.0f);
	}
}