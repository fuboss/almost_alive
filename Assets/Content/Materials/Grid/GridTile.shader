Shader "Custom/GridTile"
{
    Properties
    {
        _Color ("Color", Color) = (0, 1, 0, 0.5)
        _BorderWidth ("Border Width", Range(0.01, 0.2)) = 0.05
        _BorderColor ("Border Color", Color) = (0, 1, 0, 1)
        _FillAlpha ("Fill Alpha", Range(0, 1)) = 0.2
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+50"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            Name "GridTile"
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 3.5
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _BorderWidth;
                float4 _BorderColor;
                float _FillAlpha;
            CBUFFER_END
            
            // Per-instance data
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _InstanceColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _InstanceBorderOnly)
            UNITY_INSTANCING_BUFFER_END(Props)
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                float4 instanceColor = UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceColor);
                float borderOnly = UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceBorderOnly);
                
                // Use instance color if set, otherwise material color
                float4 color = instanceColor.a > 0 ? instanceColor : _Color;
                
                // Calculate border mask
                float2 uv = input.uv;
                float2 distFromEdge = min(uv, 1.0 - uv);
                float minDist = min(distFromEdge.x, distFromEdge.y);
                
                // Border: 1 at edge, 0 inside
                float borderMask = 1.0 - smoothstep(0.0, _BorderWidth, minDist);
                
                // Fill mask: inverse of border
                float fillMask = 1.0 - borderMask;
                
                // Combine: border at full alpha, fill at reduced alpha
                float finalAlpha;
                if (borderOnly > 0.5)
                {
                    // Border only mode
                    finalAlpha = borderMask * color.a;
                }
                else
                {
                    // Border + fill mode
                    finalAlpha = borderMask * color.a + fillMask * color.a * _FillAlpha;
                }
                
                // Discard fully transparent pixels
                clip(finalAlpha - 0.001);
                
                return float4(color.rgb, finalAlpha);
            }
            ENDHLSL
        }
    }
    
    FallBack Off
}
