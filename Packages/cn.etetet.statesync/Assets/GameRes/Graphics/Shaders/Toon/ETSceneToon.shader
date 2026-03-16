Shader "ET/Toon/Scene"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0, 2)) = 1
        _OcclusionMap("Occlusion Map", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0, 1)) = 0.35

        [Header(Scene Lighting)]
        _ShadowColor("Shadow Color", Color) = (0.72,0.67,0.62,1)
        _ToonThreshold("Toon Threshold", Range(0, 1)) = 0.52
        _ToonSmoothness("Toon Smoothness", Range(0.001, 0.2)) = 0.09
        _IndirectStrength("Indirect Strength", Range(0, 1)) = 0.18
        _LightBoost("Light Boost", Range(0.8, 1.5)) = 1.04
        _ShadowStrength("Shadow Strength", Range(0, 1)) = 0.72
        _HalfLambert("Half Lambert", Range(0, 1)) = 0.55

        [Header(Color Variation)]
        _TopColor("Top Color", Color) = (1.02,1.01,0.98,1)
        _BottomColor("Bottom Color", Color) = (0.88,0.9,0.94,1)
        _GradientCenter("Gradient Center", Range(-1, 1)) = 0
        _GradientRange("Gradient Range", Range(0.1, 4)) = 1.2

        [Header(Specular)]
        _SpecColor("Specular Color", Color) = (1,0.96,0.9,1)
        _SpecThreshold("Specular Threshold", Range(0, 1)) = 0.88
        _SpecSmoothness("Specular Smoothness", Range(0.001, 0.2)) = 0.05
        _SpecIntensity("Specular Intensity", Range(0, 1)) = 0.03
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex LitVertex
            #pragma fragment LitFragment
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _BumpScale;
                float _OcclusionStrength;
                float4 _ShadowColor;
                float _ToonThreshold;
                float _ToonSmoothness;
                float _IndirectStrength;
                float _LightBoost;
                float _ShadowStrength;
                float _HalfLambert;
                float4 _TopColor;
                float4 _BottomColor;
                float _GradientCenter;
                float _GradientRange;
                float4 _SpecColor;
                float _SpecThreshold;
                float _SpecSmoothness;
                float _SpecIntensity;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);
            TEXTURE2D(_OcclusionMap);
            SAMPLER(sampler_OcclusionMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half3 tangentWS : TEXCOORD3;
                half3 bitangentWS : TEXCOORD4;
                float fogCoord : TEXCOORD5;
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            half ToonBand(half value, half threshold, half smoothness)
            {
                return smoothstep(threshold - smoothness, threshold + smoothness, value);
            }

            Varyings LitVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = NormalizeNormalPerVertex(normalInputs.normalWS);
                output.tangentWS = normalInputs.tangentWS;
                output.bitangentWS = normalInputs.bitangentWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogCoord = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 LitFragment(Varyings input) : SV_Target
            {
                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half occlusion = lerp(1.0h, SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv).g, _OcclusionStrength);

                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                half3x3 tangentToWorld = half3x3(normalize(input.tangentWS), normalize(input.bitangentWS), normalize(input.normalWS));
                half3 normalWS = normalize(TransformTangentToWorld(normalTS, tangentToWorld));
                half3 viewDirWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));

                Light mainLight = GetMainLight();
                half3 lightDirWS = normalize(mainLight.direction);

                half rawNdotL = dot(normalWS, lightDirWS);
                half ndotl = saturate(lerp(saturate(rawNdotL), rawNdotL * 0.5h + 0.5h, _HalfLambert));
                half lightMask = ToonBand(ndotl, _ToonThreshold, _ToonSmoothness);

                half gradientMask = saturate((input.positionWS.y - _GradientCenter) / max(_GradientRange, 0.001));
                half3 gradientColor = lerp(_BottomColor.rgb, _TopColor.rgb, gradientMask);
                half3 baseColor = baseSample.rgb * gradientColor;

                half3 shadowTint = lerp(baseColor, baseColor * _ShadowColor.rgb, _ShadowStrength);
                half3 litTint = baseColor * _LightBoost;
                half3 litColor = lerp(shadowTint, litTint, lightMask);
                litColor *= mainLight.color * occlusion;

                half3 indirect = SampleSH(normalWS) * baseColor * _IndirectStrength;

                half3 halfDir = SafeNormalize(lightDirWS + viewDirWS);
                half ndoth = saturate(dot(normalWS, halfDir));
                half specBand = ToonBand(ndoth, _SpecThreshold, _SpecSmoothness) * lightMask;
                half3 specular = _SpecColor.rgb * _SpecIntensity * specBand;

                half3 finalColor = litColor + indirect + specular;
                finalColor = MixFog(finalColor, input.fogCoord);

                return half4(finalColor, baseSample.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
