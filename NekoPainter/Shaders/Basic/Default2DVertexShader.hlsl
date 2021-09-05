// 存储用于构成几何图形的三个基本列优先矩阵的常量缓冲区。
cbuffer ModelViewProjectionConstantBuffer : register(b0)
{
	matrix model;
	//matrix view;
	//matrix projection;
};

// 用作顶点着色器输入的每个顶点的数据。
struct VertexShaderInput
{
	float3 pos : POSITION;
	float2 uv : TEXCOORD0;
};

// 通过像素着色器传递的每个像素的颜色数据。
struct PixelShaderInput
{
	float4 pos : SV_POSITION;
	float4 canvasPosition : POSITION1;
	float2 uv : TEXCOORD0;
};

// 用于在 GPU 上执行顶点处理的简单着色器。
PixelShaderInput main(VertexShaderInput input)
{
	PixelShaderInput output;
	float4 pos = float4(input.pos, 1.0f);
	output.canvasPosition = pos;
	pos = mul(pos, model);
	//pos = mul(pos, view);
	output.pos = pos;
	output.uv = input.uv;

	return output;
}
