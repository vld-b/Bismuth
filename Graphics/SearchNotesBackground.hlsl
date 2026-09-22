#define D2D_INPUT_COUNT 1
#define D2D_INPUT_COMPLEX

#include "d2d1effecthelpers.hlsli"

cbuffer constants : register(b0)
{
    float3 color;
    float aspectRatio;
};

// Helper function to calculate aspect-ratio-corrected Euclidean distance
float GetCorrectedDistance(float2 uv, float2 pointPos, float aspectRatio)
{
    // Scale X coordinate difference by AspectRatio to make screen space isotropic (square)
    float2 scale = float2(aspectRatio, 1.0);
    float2 delta = (uv - pointPos) * scale;
    
    // Return Euclidean distance (length of the vector)
    return length(delta);
}

D2D_PS_ENTRY(main)
{
    float2 uv = D2DGetInputCoordinate(0).xy;

    float a = GetCorrectedDistance(uv, float2(.5f, .5f), aspectRatio);

    //return finalColor;
    return float4(color * a, a);
}