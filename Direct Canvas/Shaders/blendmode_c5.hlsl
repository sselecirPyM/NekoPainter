//file5
cbuffer DC_LayoutsData0 : register(b0)
{
	float4 DC_LayoutColor;
	int4 Parameters[8];
}
Texture2D<float4> RefTexture :register(t0);
RWTexture2D<float4> DC_Target : register(u0);

#define codehere

[numthreads(32, 32, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
	uint DC_width;
	uint DC_height;
	DC_Target.GetDimensions(DC_width, DC_height);
	if (id.x < DC_width&&id.y < DC_height)
	{
		uint2 position = id.xy;
		DC_Target[position] = blend(DC_Target[position], DC_LayoutColor, position);
	}
}

//example
/*
float4 blend(float4 bufferColor, float4 layoutColor, uint2 position)
{
	float4 color = bufferColor;
	return float4(color.rgb*(1 - layoutColor.a) + layoutColor.rgb*layoutColor.a,
		saturate(1 - (1 - layoutColor.a)*(1 - color.a)));
}
*/