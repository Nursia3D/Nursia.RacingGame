#include "Macros.fxh"

string description = "Reflection shader for glass materials in RacingGame";

// Variables that are provided by the application.
// Support for UIWidget is also added for FXComposer and 3DS Max :)

float4x4 cViewProj              : ViewProjection;
float4x4 cModel                 : World;
float3 cCameraPos;

float3 cLightDir = {1.0f, -1.0f, 1.0f};

// The ambient, diffuse and specular colors are pre-multiplied with the light color!
float4 ambientColor = {0.15f, 0.15f, 0.15f, 1.0f};

float4 diffuseColor = {0.25f, 0.25f, 0.25f, 1.0f};

float4 specularColor = {1.0f, 1.0f, 1.0f, 1.0f};

float shininess = 24.0;

float alphaFactor = 0.66f;

float fresnelBias = 0.5f;
float fresnelPower = 1.5f;
float reflectionAmount = 1.0f;

float4 cEnvColor;

texture EnvCubeMap;
samplerCUBE EnvCubeMapSampler = sampler_state
{
    Texture = <EnvCubeMap>;
    AddressU  = Wrap;
    AddressV  = Wrap;
    AddressW  = Wrap;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
};

//----------------------------------------------------

// Vertex input structure (used for ALL techniques here!)
struct VertexInput
{
    float3 pos      : POSITION;
    float2 texCoord : TEXCOORD0;
    float3 normal   : NORMAL;
    float3 tangent  : TANGENT;
};

//----------------------------------------------------

// Common functions
float4 TransformPosition(float3 pos)
{
    return mul(mul(float4(pos.xyz, 1), cModel), cViewProj);
}

float3 GetWorldPos(float3 pos)
{
    return mul(float4(pos, 1), cModel).xyz;
}

float3 CalcNormalVector(float3 nor)
{
    return normalize(mul(nor, (float3x3)cModel));
}

//----------------------------------------------------

// For ps1.1 we can't do this advanced stuff,
// just render the material with the reflection and basic lighting
struct VertexOutput_Texture
{
    float4 pos          : POSITION;
    float3 cubeTexCoord : TEXCOORD1;
    float3 normal       : TEXCOORD2;
    float3 halfVec        : TEXCOORD3;
};

//----------------------------------------------------

struct VertexOutput20
{
    float4 pos      : POSITION;
    float3 normal   : TEXCOORD0;
    float3 viewVec  : TEXCOORD1;
    float3 halfVec  : TEXCOORD2;
};

// vertex shader
VertexOutput20 VS_ReflectionSpecular20(VertexInput In)
{
    VertexOutput20 Out;
    Out.pos = TransformPosition(In.pos);
    Out.normal = CalcNormalVector(In.normal);
    Out.viewVec = normalize(cCameraPos - GetWorldPos(In.pos));
    Out.halfVec = normalize(Out.viewVec + cLightDir);
    return Out;
}

float4 PS_ReflectionSpecular20(VertexOutput20 In) : COLOR
{
    half3 N = normalize(In.normal);
    float3 V = normalize(In.viewVec);

    // Reflection
    half3 R = reflect(-V, N);
    R = float3(R.x, R.z, R.y);
    half4 reflColor = cEnvColor * texCUBE(EnvCubeMapSampler, R);
    
    // Fresnel
    float3 E = -V;
    float facing = 1.0 - max(dot(E, -N), 0);
    float fresnel = fresnelBias + (1.0-fresnelBias)*pow(facing, fresnelPower);

    // Diffuse factor
    float diff = saturate(dot(N, cLightDir));

    // Specular factor
    float spec = pow(saturate(dot(N, In.halfVec)), shininess);
    
    // Output the colors
    float4 diffAmbColor = ambientColor + diff * diffuseColor;
    float4 ret;
    ret.rgb = reflColor * reflectionAmount * fresnel * 1.5f +
        diffAmbColor;
    ret.a = alphaFactor;
    ret += spec * specularColor;
    return ret;
}

technique ReflectionSpecular20
{
    pass P0
    {
        AlphaBlendEnable = true;
        SrcBlend = SrcAlpha;
        DestBlend = InvSrcAlpha;
        
        VertexShader = compile VS_PROFILE VS_ReflectionSpecular20();
        PixelShader  = compile PS_PROFILE PS_ReflectionSpecular20();
    }
}
