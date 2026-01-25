Shader "Game/StructureCutout"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _Smoothness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0
        
        [Header(Maps)]
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0,2)) = 1
        _OcclusionMap("Occlusion Map", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0,1)) = 1
        
        [Header(Cutout)]
        [Toggle(_CUTOUT_ENABLED)] _CutoutEnabled("Cutout Enabled", Float) = 0
        _CutoutCenter("Cutout Center", Vector) = (0,0,0,0)
        _CutoutRadius("Cutout Radius", Float) = 5
        _CutoutFalloff("Cutout Falloff", Range(0,1)) = 0.5
        _CutoutIntensity("Cutout Intensity", Range(0,1)) = 1
        _EdgeSoftness("Edge Softness", Range(0,2)) = 0.5
        
        [Header(Shape)]
        [Toggle(_CUTOUT_SHAPE_MASK)] _UseShapeMask("Use Shape Mask", Float) = 0
        _ShapeMask("Shape Mask", 2D) = "white" {}
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            
            #pragma shader_feature_local _CUTOUT_ENABLED
            #pragma shader_feature_local _CUTOUT_SHAPE_MASK
            #pragma shader_feature_local _NORMALMAP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);
            
            TEXTURE2D(_OcclusionMap);
            SAMPLER(sampler_OcclusionMap);
            
            #ifdef _CUTOUT_SHAPE_MASK
            TEXTURE2D(_ShapeMask);
            SAMPLER(sampler_ShapeMask);
            #endif

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Smoothness;
                float _Metallic;
                
                float _BumpScale;
                float _OcclusionStrength;
                
                float _CutoutEnabled;
                float3 _CutoutCenter;
                float _CutoutRadius;
                float _CutoutFalloff;
                float _CutoutIntensity;
                float _EdgeSoftness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS = normalInput.normalWS;
                output.tangentWS = float4(normalInput.tangentWS, input.tangentOS.w);
                
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                
                return output;
            }

            float CalculateCutoutAlpha(float3 worldPos)
            {
                #ifdef _CUTOUT_ENABLED
                    if (_CutoutEnabled < 0.5) return 1.0;
                    
                    float dist = distance(worldPos, _CutoutCenter);
                    float normalizedDist = saturate(dist / _CutoutRadius);
                    
                    float cutoutFactor = smoothstep(_CutoutFalloff - _EdgeSoftness, _CutoutFalloff + _EdgeSoftness, normalizedDist);
                    
                    #ifdef _CUTOUT_SHAPE_MASK
                        float2 dir = (worldPos.xz - _CutoutCenter.xz) / _CutoutRadius;
                        float2 maskUV = dir * 0.5 + 0.5;
                        float shapeMask = SAMPLE_TEXTURE2D(_ShapeMask, sampler_ShapeMask, maskUV).r;
                        cutoutFactor *= shapeMask;
                    #endif
                    
                    return lerp(cutoutFactor, 1.0, 1.0 - _CutoutIntensity);
                #else
                    return 1.0;
                #endif
            }

            half4 frag(Varyings input) : SV_Target
            {
                float alpha = CalculateCutoutAlpha(input.positionWS);
                clip(alpha - 0.01);
                
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                
                // Normal mapping
                half3 normalWS = input.normalWS;
                #ifdef _NORMALMAP
                    half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                    half3 bitangentWS = input.tangentWS.w * cross(input.normalWS, input.tangentWS.xyz);
                    half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangentWS, input.normalWS);
                    normalWS = normalize(mul(normalTS, tangentToWorld));
                #endif
                
                // Occlusion
                half occlusion = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv).g;
                occlusion = lerp(1.0, occlusion, _OcclusionStrength);
                
                // Lighting
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                
                half3 ambient = half3(0.2, 0.2, 0.2) * occlusion;
                half3 lighting = mainLight.color * NdotL + ambient;
                half3 finalColor = baseColor.rgb * lighting;
                
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
            #pragma shader_feature_local _CUTOUT_ENABLED

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
}
