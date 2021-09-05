RWTexture2D<float4> DC_Target : register(u0);
uniform StructuredBuffer<uint2> DC_Tiles : register(t0);
//Texture2D<float4> RefTexture1 : register(t1);
//Texture2D<float4> RefTexture2 : register(t2);
//Texture2D<float> DC_SelectionMask : register(t3);
struct InputInfo
{
	uint FrameId;
	uint PointerId;
	uint2 Timestamp;
	float4 Position;
	float2 XYTilt;
	float Twist;
	float Pressure;
	float Orientation;
	float ZDistance;
	float2 InputInfoPreserverd;
};
//cbuffer BrushData : register(b0)
//{
//	float4 BrushColor;
//	float4 BrushColor2;
//	float4 BrushColor3;
//	float4 BrushColor4;
//	float BrushSize;
//	int UseSelection;
//	float2 BrushDataPreserved;
//	InputInfo InputDatas[8];
//	//int4 Parameters[8];
//};

#define codehere

[numthreads(8, 8, 1)]
void main(uint3 id : SV_DispatchThreadID)
{
	uint DC_count;
	uint DC_stride;
	DC_Tiles.GetDimensions(DC_count, DC_stride);
	if (id.z < DC_count)
	{
		uint2 position = id.xy + DC_Tiles[id.z];
		//if (DC_SelectionMask[position].r > 0.5f||!UseSelection)
			DC_Target[position] = brush(DC_Target[position], position);
	}
}

//example
/*
float4 brush(float4 bufferColor, uint2 position)
{
	float4 color;
	InputInfo p0 = InputDatas[0];

	if (distance(position + float2(0.5f, 0.5f), p0.Position.xy) < BrushSize)
	{
		color = BrushColor;
	}
	else
	{
		color = bufferColor;
	}

	return color;
}
*/