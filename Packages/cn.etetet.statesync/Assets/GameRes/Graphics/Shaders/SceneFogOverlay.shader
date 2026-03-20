Shader "ET/SceneFogOverlay"
{
    Properties
    {
        [MainTexture] _FogTex("Fog Texture", 2D) = "black" {}
        _FogBounds("Fog Bounds", Vector) = (0, 0, 1, 1)
        _FogPlaneY("Fog Plane Y", Float) = 0
        _EdgeSoftness("Edge Softness", Float) = 1.2
        _OutsideFogColor("Outside Fog Color", Color) = (0.035, 0.075, 0.118, 0.91)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+500"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "SceneFogOverlay"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _FogBounds;
                float _FogPlaneY;
                float _EdgeSoftness;
                half4 _OutsideFogColor;
            CBUFFER_END

            TEXTURE2D(_FogTex);
            SAMPLER(sampler_FogTex);
            float4 _FogTex_TexelSize;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.screenPos = ComputeScreenPos(positionInputs.positionCS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 screenUv = input.screenPos.xy / input.screenPos.w;
                real deviceDepth = SampleSceneDepth(screenUv);

                #if UNITY_REVERSED_Z
                    if (deviceDepth <= 0.0001h)
                    {
                        return half4(0, 0, 0, 0);
                    }
                #else
                    deviceDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, deviceDepth);
                    if (deviceDepth >= 0.9999h)
                    {
                        return half4(0, 0, 0, 0);
                    }
                #endif

                float3 surfacePositionWS = ComputeWorldSpacePosition(screenUv, deviceDepth, UNITY_MATRIX_I_VP);
                float3 rayToSurfaceWS = surfacePositionWS - _WorldSpaceCameraPos.xyz;
                if (abs(rayToSurfaceWS.y) <= 0.0001)
                {
                    return half4(0, 0, 0, 0);
                }

                float rayDistanceScale = (_FogPlaneY - _WorldSpaceCameraPos.y) / rayToSurfaceWS.y;
                if (rayDistanceScale <= 0.0)
                {
                    return half4(0, 0, 0, 0);
                }

                float3 positionWS = _WorldSpaceCameraPos.xyz + rayToSurfaceWS * rayDistanceScale;
                float2 delta = positionWS.xz - _FogBounds.xy;
                float2 size = max(_FogBounds.zw, float2(0.0001, 0.0001));
                float2 fogUv = delta / size;

                if (fogUv.x < 0.0 || fogUv.x > 1.0 || fogUv.y < 0.0 || fogUv.y > 1.0)
                {
                    return _OutsideFogColor;
                }

                half4 center = SAMPLE_TEXTURE2D(_FogTex, sampler_FogTex, fogUv);
                if (_EdgeSoftness <= 0.001)
                {
                    return center;
                }

                float2 texelOffset = _FogTex_TexelSize.xy * _EdgeSoftness;
                half4 blur = center * 4.0h;
                blur += SAMPLE_TEXTURE2D(_FogTex, sampler_FogTex, fogUv + float2(texelOffset.x, 0.0));
                blur += SAMPLE_TEXTURE2D(_FogTex, sampler_FogTex, fogUv - float2(texelOffset.x, 0.0));
                blur += SAMPLE_TEXTURE2D(_FogTex, sampler_FogTex, fogUv + float2(0.0, texelOffset.y));
                blur += SAMPLE_TEXTURE2D(_FogTex, sampler_FogTex, fogUv - float2(0.0, texelOffset.y));
                blur *= 0.125h;

                half blurBlend = saturate(_EdgeSoftness * 0.5h);
                return lerp(center, blur, blurBlend);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
