#define D2D_INPUT_COUNT 1
#define D2D_INPUT_SIMPLE

#include "d2d1effecthelpers.hlsli"

cbuffer constants : register(b0)
{
    float2 p1;
    float2 p2;
    float2 p3;
    float3 color;
    float aspectRatio;
    float glowStrength;
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
    float2 uv = D2DGetInputCoordinate(0);

    float dist1 = GetCorrectedDistance(uv, p1, aspectRatio);
    float dist2 = GetCorrectedDistance(uv, p2, aspectRatio);
    float dist3 = GetCorrectedDistance(uv, p3, aspectRatio);

    float minDistance = min(dist1, min(dist2, dist3));

    // float linearAlpha = saturate(1.0f - (minDistance / 100.0f));
    float smoothAlpha = smoothstep(0.3f, 0.0f, minDistance);
    
    float4 finalColor = float4(color, smoothAlpha);
    finalColor.rgb *= finalColor.a;

    return finalColor;
}