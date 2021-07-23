RWTexture2D<float4> DC_Target : register(u0);
uniform StructuredBuffer<uint2> DC_Tiles : register(t0);
Texture2D<float4> RefTexture1 : register(t1);
Texture2D<float4> RefTexture2 : register(t2);
Texture2D<float> DC_SelectionMask : register(t3);
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
cbuffer BrushData : register(b0)
{
	float4 BrushColor;
	float4 BrushColor2;
	float4 BrushColor3;
	float4 BrushColor4;
	int4 Parameters[8];
	float BrushSize;
	int UseSelection;
	float2 BrushDataPreserved;
	InputInfo InputDatas[16];
};

float4 StandardBrush(float4 bufferColor, uint2 position, float4 color,float hs)
{
	float2 mp = InputDatas[0].Position.xy - InputDatas[1].Position.xy;
	float mrad = atan2(mp.y, mp.x);
	float4x4 transform9 =
	{
		cos(mrad),-sin(mrad),0.0f,0.0f,
		sin(mrad),cos(mrad),0.0f,0.0f,
		0.0f,0.0f,1.0f,0.0f,
		0.0f,0.0f,0.0f,1.0f
	};
	float4 rPos = mul(float4(position + float2(0.5f, 0.5f) - InputDatas[1].Position.xy, 0, 1), transform9);
	float rSize = BrushSize * InputDatas[0].Pressure;
	float rl = sqrt(saturate(1 - (rPos.y / rSize)*(rPos.y / rSize)));
	float rl2 = rl * rSize;
	float rDistance = (1 - saturate((-rPos.x + rl2) / rl2 * 0.5f) - saturate((rPos.x - length(mp) + rl2) / rl2 * 0.5f));
	color.a = (1 - pow(2.718281828f, (rl*hs + 1.0f - hs)*log(1.0f - color.a*0.99999f)*rDistance));

	if (abs(rPos.y) <= rSize && rPos.x >= 0 && rPos.x <= length(mp) || distance(rPos, float2(0, 0))<= rSize || distance(rPos, float2(length(mp), 0)) <= rSize)
	{
		if (color.a > 0.00000001f)
		{
			float aalpha = 1 - (1 - bufferColor.a)*(1 - color.a);
			color = float4(bufferColor.rgb*(1 - color.a)*(bufferColor.a) / aalpha + color.rgb*color.a / aalpha, aalpha);
		}
		else
		{
			color = bufferColor;
		}
	}
	else
	{
		color = bufferColor;
	}
	return color;
}

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
		if (DC_SelectionMask[position].r > 0.5f||!UseSelection)
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