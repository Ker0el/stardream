cbuffer SceneConstants : register(b0)
{
    row_major float4x4 ViewProjection;
};

struct VertexIn
{
    float3 Position : POSITION;
    float4 Color : COLOR;
};

struct VertexOut
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
};

VertexOut VSMain(VertexIn input)
{
    VertexOut output;
    output.Position = mul(ViewProjection, float4(input.Position, 1.0f));
    output.Color = input.Color;
    return output;
}

float4 PSMain(VertexOut input) : SV_TARGET
{
    return input.Color;
}
