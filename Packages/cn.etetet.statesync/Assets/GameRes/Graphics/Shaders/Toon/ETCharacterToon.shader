Shader "ET/Toon/Character"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0, 2)) = 1

        [Header(Toon Lighting)]
        _ShadowColor("Shadow Color", Color) = (0.55,0.62,0.82,1)
        _ToonThreshold("Toon Threshold", Range(0, 1)) = 0.5
        _ToonSmoothness("Toon Smoothness", Range(0.001, 0.2)) = 0.04
        _IndirectStrength("Indirect Strength", Range(0, 1)) = 0.35
        _LightBoost("Light Boost", Range(0.8, 1.5)) = 1.08
        _ShadowStrength("Shadow Strength", Range(0, 1)) = 0.85
        _HalfLambert("Half Lambert", Range(0, 1)) = 0.35

        [Header(Friendly Lift)]
        _FriendlyTint("Friendly Tint", Color) = (1,0.97,0.94,1)
        _FriendlyThreshold("Friendly Threshold", Range(0, 1)) = 0.58
        _FriendlySmoothness("Friendly Smoothness", Range(0.01, 0.4)) = 0.12
        _FriendlyFrontPower("Friendly Front Power", Range(0.5, 6)) = 2.2
        _FriendlyIntensity("Friendly Intensity", Range(0, 1)) = 0.18

        [Header(Specular)]
        _SpecColor("Specular Color", Color) = (1,0.97,0.92,1)
        _SpecThreshold("Specular Threshold", Range(0, 1)) = 0.75
        _SpecSmoothness("Specular Smoothness", Range(0.001, 0.2)) = 0.04
        _SpecIntensity("Specular Intensity", Range(0, 2)) = 0.25

        [Header(Rim)]
        _RimColor("Rim Color", Color) = (1,0.95,0.84,1)
        _RimPower("Rim Power", Range(0.5, 8)) = 3
        _RimIntensity("Rim Intensity", Range(0, 2)) = 0.4

        [Header(Outline)]
        _OutlineColor("Outline Color", Color) = (0.16,0.19,0.24,1)
        _OutlineWidth("Outline Width", Range(0, 0.03)) = 0.006

        [Header(Alpha Clip)]
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clip", Float) = 0
        _Cutoff("Cutoff", Range(0, 1)) = 0.5
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
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _BumpScale;
                float4 _OutlineColor;
                float _OutlineWidth;
                float _Cutoff;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings OutlineVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS += normalWS * _OutlineWidth;

                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 OutlineFragment(Varyings input) : SV_Target
            {
                #if defined(_ALPHATEST_ON)
                    half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                    clip(alpha - _Cutoff);
                #endif

                return _OutlineColor;
            }
            ENDHLSL
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
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _BumpScale;
                float4 _ShadowColor;
                float4 _SpecColor;
                float4 _RimColor;
                float _ToonThreshold;
                float _ToonSmoothness;
                float _IndirectStrength;
                float _LightBoost;
                float _ShadowStrength;
                float _HalfLambert;
                float4 _FriendlyTint;
                float _FriendlyThreshold;
                float _FriendlySmoothness;
                float _FriendlyFrontPower;
                float _FriendlyIntensity;
                float _SpecThreshold;
                float _SpecSmoothness;
                float _SpecIntensity;
                float _RimPower;
                float _RimIntensity;
                float _Cutoff;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

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
                #if defined(_ALPHATEST_ON)
                    clip(baseSample.a - _Cutoff);
                #endif

                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                half3x3 tangentToWorld = half3x3(normalize(input.tangentWS), normalize(input.bitangentWS), normalize(input.normalWS));
                half3 normalWS = normalize(TransformTangentToWorld(normalTS, tangentToWorld));
                half3 viewDirWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));

                Light mainLight = GetMainLight();
                half3 lightDirWS = normalize(mainLight.direction);

                half rawNdotL = dot(normalWS, lightDirWS);
                half ndotl = saturate(lerp(saturate(rawNdotL), rawNdotL * 0.5h + 0.5h, _HalfLambert));
                half lightMask = ToonBand(ndotl, _ToonThreshold, _ToonSmoothness);

                half3 shadowTint = lerp(baseSample.rgb, baseSample.rgb * _ShadowColor.rgb, _ShadowStrength);
                half3 litTint = baseSample.rgb * _LightBoost;
                half3 litColor = lerp(shadowTint, litTint, lightMask);
                litColor *= mainLight.color;

                half3 indirect = SampleSH(normalWS) * baseSample.rgb * _IndirectStrength;

                half3 halfDir = SafeNormalize(lightDirWS + viewDirWS);
                half ndoth = saturate(dot(normalWS, halfDir));
                half specBand = ToonBand(ndoth, _SpecThreshold, _SpecSmoothness) * lightMask;
                half3 specular = _SpecColor.rgb * _SpecIntensity * specBand;

                half baseLuma = dot(baseSample.rgb, half3(0.299h, 0.587h, 0.114h));
                half brightMask = smoothstep(_FriendlyThreshold - _FriendlySmoothness, _FriendlyThreshold + _FriendlySmoothness, baseLuma);
                half frontMask = pow(saturate(dot(normalWS, viewDirWS)), _FriendlyFrontPower);
                half friendlyMask = brightMask * frontMask * (0.35h + 0.65h * lightMask) * _FriendlyIntensity;
                half3 friendlyLift = baseSample.rgb * _FriendlyTint.rgb * friendlyMask;

                half rim = pow(1.0h - saturate(dot(viewDirWS, normalWS)), _RimPower);
                rim *= lightMask;
                half3 rimLight = _RimColor.rgb * rim * _RimIntensity;

                half3 finalColor = litColor + indirect + specular + rimLight + friendlyLift;
                finalColor = MixFog(finalColor, input.fogCoord);

                return half4(finalColor, baseSample.a);
            }
            ENDHLSL
        }

    }

    FallBack Off
}
