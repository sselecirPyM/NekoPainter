// 通过像素着色器传递的每个像素的颜色数据。
struct PixelShaderInput
{
	float4 pos : SV_POSITION;
	float4 canvasPosition : POSITION1;
};

// (内插)颜色数据的传递函数。
float4 main(PixelShaderInput input) : SV_TARGET
{
	float4 canvasPosition = input.canvasPosition;
	float4 color = canvasPosition / 128;
	color = color % float4(1.0f, 1.0f, 1.0f, 1.0f);
	color = round(color);
	if (color.x==color.y)
	{
		color = float4(0.5f, 0.5f, 0.5f, 1.0f);
	}
	else
	{
		color = float4(0.75f, 0.75f, 0.75f, 1.0f);
	}
	color = float4(color.xyz, 1.0f);
	return color;
}
