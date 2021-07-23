// 通过像素着色器传递的每个像素的颜色数据。
cbuffer ModelViewProjectionConstantBuffer : register(b0)
{
	matrix model;
	//matrix view;
	//matrix projection;
	float2 canvasSize;
}
Texture2D tex : register(t0);
SamplerState texSampler : register(s0);
struct PixelShaderInput
{
	float4 pos : SV_POSITION;
	float4 canvasPosition : POSITION1;
	float2 uv : TEXCOORD0;
};

// (内插)颜色数据的传递函数。
float4 main(PixelShaderInput input) : SV_TARGET
{
	float2 canvasPosition = input.canvasPosition.xy * canvasSize;
	float2 pos1= canvasPosition / 128;
	pos1 = pos1 % float4(1.0f, 1.0f, 1.0f, 1.0f);
	pos1 = round(pos1);
	float4 color;
	if (pos1.x == pos1.y)
	{
		color = float4(0.5f, 0.5f, 0.5f, 1.0f);
	}
	else
	{
		color = float4(0.75f, 0.75f, 0.75f, 1.0f);
	}
	float4 texColor = tex.Sample(texSampler,input.uv);
	color = float4(color.rgb * (1.0f - texColor.a) + texColor.rgb * texColor.a, 1.0f);
	//return float4(texColor.rgb,1.0f);
	return color;
}
