//file7
Texture2D<float4> DC_Source :register(t0);
Texture2D<float4> RefTexture :register(t1);
Texture2D<float> DC_SelectionMask : register(t3);
RWTexture2D<float4> DC_Target : register(u0);
cbuffer DC_LayoutsData0 : register(b0)
{
	float4 DC_LayoutColor;
	int4 Parameters[8];
}
cbuffer DC_SelectionOffset : register(b1)
{
	float4 DC_SelectionColor;
	int2 DC_MaskOffset;
	int DC_UseSelection;
};

#define codehere

[numthreads(32, 32, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
	uint DC_width;
	uint DC_height;
	DC_Source.GetDimensions(DC_width, DC_height);
	if (id.x < DC_width&&id.y < DC_height)
	{
		uint2 position = id.xy;
		int2 position2 = position + DC_MaskOffset;
		if (DC_SelectionMask[position].r < 0.5&&DC_SelectionMask[position2].r < 0.5)
			DC_Target[position] = blend(DC_Target[position], DC_Source[position], position);
		else if (DC_SelectionMask[position2].r > 0.5)
			DC_Target[position] = blend(DC_Target[position], DC_Source[position2], position);
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