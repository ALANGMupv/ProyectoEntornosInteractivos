#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

#if UNITY_VERSION >= 60010000
#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
#define _USE_MULTI_LIGHT USE_CLUSTER_LIGHT_LOOP
#else
#pragma multi_compile _ _FORWARD_PLUS
#define _USE_MULTI_LIGHT USE_FORWARD_PLUS
#endif


void MainLight_half(float3 WorldPos, out half3 Direction, out half3 Color, out half DistanceAtten, out half ShadowAtten)
{
#if SHADERGRAPH_PREVIEW
    Direction = half3(0.5, 0.5, 0);
    Color = 1;
    DistanceAtten = 1;
    ShadowAtten = 1;
#else
#if SHADOWS_SCREEN
    half4 clipPos = TransformWorldToHClip(WorldPos);
    half4 shadowCoord = ComputeScreenPos(clipPos);
#else
    half4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
#endif
    Light mainLight = GetMainLight(shadowCoord);
    Direction = mainLight.direction;
    Color = mainLight.color;
    DistanceAtten = mainLight.distanceAttenuation;
    ShadowAtten = mainLight.shadowAttenuation;
#endif
}

void DirectSpecular_half(half3 Specular, half Smoothness, half3 Direction, half3 Color, half3 WorldNormal, half3 WorldView, out half3 Out)
{
#if SHADERGRAPH_PREVIEW
    Out = 0;
#else
    Smoothness = exp2(10 * Smoothness + 1);
    WorldNormal = normalize(WorldNormal);
    WorldView = SafeNormalize(WorldView);
    Out = LightingSpecular(Color, Direction, WorldNormal, WorldView,half4(Specular, 0), Smoothness);
#endif
}

void AdditionalLights_half(half3 SpecColor, half Smoothness, half3 WorldPosition, half3 WorldNormal, half3 WorldView, half3 ScreenPos, out half3 Diffuse, out half3 Specular)
{
    half3 diffuseColor = 0;
    half3 specularColor = 0;

#ifndef SHADERGRAPH_PREVIEW
    Smoothness = exp2(10 * Smoothness + 1);
    WorldNormal = normalize(WorldNormal);
    WorldView = SafeNormalize(WorldView);

    InputData inputData = (InputData)0;
    inputData.positionWS = WorldPosition;
    inputData.normalWS = WorldNormal;
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(WorldPosition);

    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(ScreenPos);

//Forward plus GetAdditionalLightsCount give 0 all the time, we need special handling
#if USE_CLUSTER_LIGHT_LOOP
    UNITY_LOOP for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        Light additionalLight = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1,1,1,1));
        half3 attenuatedLightColor = additionalLight.color * (additionalLight.distanceAttenuation * additionalLight.shadowAttenuation);
        diffuseColor += LightingLambert(attenuatedLightColor, additionalLight.direction, WorldNormal);
        specularColor += LightingSpecular(attenuatedLightColor, additionalLight.direction, WorldNormal, WorldView, half4(SpecColor, 0), Smoothness);
    }
#endif

    int pixelLightCount = GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData.positionWS);
        half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
        diffuseColor += LightingLambert(attenuatedLightColor, light.direction, WorldNormal);
        specularColor += LightingSpecular(attenuatedLightColor, light.direction, WorldNormal, WorldView, half4(SpecColor, 0), Smoothness);
    LIGHT_LOOP_END

#endif

    Diffuse = diffuseColor;
    Specular = specularColor;
}

#endif