cbuffer SceneConstants : register(b0)
{
    row_major float4x4 World;
    row_major float4x4 ViewProjection;
    float4 BaseColor;
};

struct VertexIn
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
};

struct VertexOut
{
    float4 Position : SV_POSITION;
    float3 Normal : NORMAL;
    float3 Color : COLOR;
};

VertexOut VSMain(VertexIn input)
{
    VertexOut output;
    float4 world = mul(World, float4(input.Position, 1.0f));
    output.Position = mul(ViewProjection, world);
    output.Normal = normalize(mul((float3x3)World, input.Normal));
    output.Color = BaseColor.rgb;
    return output;
}

float4 PSMain(VertexOut input) : SV_TARGET
{
    float3 n = normalize(input.Normal);
    float3 lightDirection = normalize(float3(0.35f, 0.85f, 0.4f));
    float ndl = saturate(dot(n, lightDirection));

    float3 sky = float3(0.46f, 0.49f, 0.56f);
    float3 ground = float3(0.14f, 0.135f, 0.13f);
    float3 ambient = lerp(ground, sky, n.y * 0.5f + 0.5f);

    float3 color = input.Color * (ambient * 0.85f + ndl * 0.65f);
    return float4(color, 1.0f);
}
