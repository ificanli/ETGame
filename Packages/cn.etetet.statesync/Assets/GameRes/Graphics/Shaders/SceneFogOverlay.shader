Shader "ET/SceneFogOverlay"
{
    Properties
    {
        [MainTexture] _FogTex("Fog Texture", 2D) = "black" {}
        _FogBounds("Fog Bounds", Vector) = (0, 0, 1, 1)
        _FogPlaneY("Fog Plane Y", Float) = 0
        _EdgeSoftness("Edge Softness", Float) = 1.2
        _OutsideFogColor("Outside Fog Color", Color) = (0.035, 0.075, 0.118, 0.91)
        _VisionCenter("Vision Center XZ", Vector) = (0, 0, 0, 0)
        _VisionRadius("Vision Radius", Float) = 0
        _VisibleColor("Visible Color", Color) = (0, 0, 0, 0)
        // 调试模式: 0=正常, 1=红色, 2=深度, 3=分支, 4=迷雾值, 5=UV坐标
        _DebugMode("Debug Mode", Float) = 0
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
                float4 _VisionCenter;
                half4 _OutsideFogColor;
                half4 _VisibleColor;
                float _FogPlaneY;
                float _EdgeSoftness;
                float _VisionRadius;
                float _DebugMode;
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

                // 调试模式:
                // 1: 红色 - 测试shader执行
                // 2: 显示深度值（越深越亮）
                // 3: 显示各分支（不同颜色代表不同路径）
                // 4: 显示迷雾纹理值（灰度）
                // 5: 显示UV坐标（R=U, G=V）
                if (_DebugMode > 4.5)
                {
                    // 模式5: 显示UV坐标
                    #if UNITY_REVERSED_Z
                        if (deviceDepth <= 0.0001h) return half4(0, 0, 1, 0.7); // 蓝=天空
                    #else
                        float adjustedDepth5 = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, deviceDepth);
                        if (adjustedDepth5 >= 0.9999h) return half4(0, 0, 1, 0.7);
                    #endif
                    
                    float3 surfacePos5 = ComputeWorldSpacePosition(screenUv, deviceDepth, UNITY_MATRIX_I_VP);
                    float3 rayToSurface5 = surfacePos5 - _WorldSpaceCameraPos.xyz;
                    if (abs(rayToSurface5.y) <= 0.0001) return half4(1, 1, 0, 0.7);
                    float scale5 = (_FogPlaneY - _WorldSpaceCameraPos.y) / rayToSurface5.y;
                    if (scale5 <= 0.0) return half4(1, 0, 1, 0.7);
                    
                    float3 pos5 = _WorldSpaceCameraPos.xyz + rayToSurface5 * scale5;
                    float2 delta5 = pos5.xz - _FogBounds.xy;
                    float2 size5 = max(_FogBounds.zw, float2(0.0001, 0.0001));
                    float2 uv5 = delta5 / size5;
                    
                    // 显示UV: 红=U, 绿=V, 超出范围显示蓝色
                    if (uv5.x < 0.0 || uv5.x > 1.0 || uv5.y < 0.0 || uv5.y > 1.0)
                    {
                        return half4(uv5.x, uv5.y, 1.0, 0.8);
                    }
                    return half4(uv5.x, uv5.y, 0, 0.8);
                }
                else if (_DebugMode > 3.5)
                {
                    // 模式4: 显示迷雾纹理采样值（灰度）
                    #if UNITY_REVERSED_Z
                        if (deviceDepth <= 0.0001h) return half4(0, 0, 0, 0);
                    #else
                        float adjustedDepth4 = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, deviceDepth);
                        if (adjustedDepth4 >= 0.9999h) return half4(0, 0, 0, 0);
                    #endif
                    
                    float3 surfacePos4 = ComputeWorldSpacePosition(screenUv, deviceDepth, UNITY_MATRIX_I_VP);
                    float3 rayToSurface4 = surfacePos4 - _WorldSpaceCameraPos.xyz;
                    if (abs(rayToSurface4.y) <= 0.0001) return half4(0, 0, 0, 0);
                    float scale4 = (_FogPlaneY - _WorldSpaceCameraPos.y) / rayToSurface4.y;
                    if (scale4 <= 0.0) return half4(0, 0, 0, 0);
                    
                    float3 pos4 = _WorldSpaceCameraPos.xyz + rayToSurface4 * scale4;
                    float2 delta4 = pos4.xz - _FogBounds.xy;
                    float2 size4 = max(_FogBounds.zw, float2(0.0001, 0.0001));
                    float2 uv4 = delta4 / size4;
                    
                    if (uv4.x < 0.0 || uv4.x > 1.0 || uv4.y < 0.0 || uv4.y > 1.0)
                    {
                        return half4(1, 0.5, 0, 0.7); // 橙色=超出边界
                    }
                    
                    half4 fogSample = SAMPLE_TEXTURE2D(_FogTex, sampler_FogTex, uv4);
                    // 直接显示纹理的RGB颜色（放大10倍以便看清深色）
                    // 迷雾颜色是#09131E (深蓝黑)，红色标记是#FF0000
                    half3 scaledRGB = saturate(fogSample.rgb * 10.0);
                    return half4(scaledRGB, 0.9);
                }
                else if (_DebugMode > 2.5)
                {
                    // 模式3: 分支调试
                    #if UNITY_REVERSED_Z
                        if (deviceDepth <= 0.0001h)
                        {
                            return half4(0, 0, 1, 0.7); // 蓝色 = 天空（深度为0）
                        }
                    #else
                        float adjustedDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, deviceDepth);
                        if (adjustedDepth >= 0.9999h)
                        {
                            return half4(0, 0, 1, 0.7); // 蓝色 = 天空
                        }
                    #endif
                    
                    float3 surfacePositionWS = ComputeWorldSpacePosition(screenUv, deviceDepth, UNITY_MATRIX_I_VP);
                    float3 rayToSurfaceWS = surfacePositionWS - _WorldSpaceCameraPos.xyz;
                    
                    if (abs(rayToSurfaceWS.y) <= 0.0001)
                    {
                        return half4(1, 1, 0, 0.7); // 黄色 = 水平射线
                    }

                    float rayDistanceScale = (_FogPlaneY - _WorldSpaceCameraPos.y) / rayToSurfaceWS.y;
                    
                    if (rayDistanceScale <= 0.0)
                    {
                        return half4(1, 0, 1, 0.7); // 紫色 = 交点在相机后面
                    }
                    
                    float3 positionWS = _WorldSpaceCameraPos.xyz + rayToSurfaceWS * rayDistanceScale;
                    float2 delta = positionWS.xz - _FogBounds.xy;
                    float2 size = max(_FogBounds.zw, float2(0.0001, 0.0001));
                    float2 fogUv = delta / size;

                    if (fogUv.x < 0.0 || fogUv.x > 1.0 || fogUv.y < 0.0 || fogUv.y > 1.0)
                    {
                        return half4(1, 0.5, 0, 0.7); // 橙色 = 超出边界
                    }
                    
                    return half4(0, 1, 0, 0.7); // 绿色 = 正常采样区域
                }
                else if (_DebugMode > 1.5)
                {
                    // 模式2: 显示深度
                    #if UNITY_REVERSED_Z
                        // reversed Z: 1=近, 0=远
                        return half4(deviceDepth, deviceDepth, deviceDepth, 0.7);
                    #else
                        return half4(1.0 - deviceDepth, 1.0 - deviceDepth, 1.0 - deviceDepth, 0.7);
                    #endif
                }
                else if (_DebugMode > 0.5)
                {
                    // 模式1: 纯红色
                    return half4(1, 0, 0, 0.5);
                }

                // 正常迷雾渲染逻辑
                #if UNITY_REVERSED_Z
                    // 深度为0表示最远处（天空），跳过
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
                
                // 射线几乎水平，无法计算交点
                if (abs(rayToSurfaceWS.y) <= 0.0001)
                {
                    return half4(0, 0, 0, 0);
                }

                float rayDistanceScale = (_FogPlaneY - _WorldSpaceCameraPos.y) / rayToSurfaceWS.y;
                
                // 交点在相机后面，跳过
                if (rayDistanceScale <= 0.0)
                {
                    return half4(0, 0, 0, 0);
                }

                float3 positionWS = _WorldSpaceCameraPos.xyz + rayToSurfaceWS * rayDistanceScale;
                float2 delta = positionWS.xz - _FogBounds.xy;
                float2 size = max(_FogBounds.zw, float2(0.0001, 0.0001));
                float2 fogUv = delta / size;

                // 超出迷雾边界，返回外部迷雾颜色
                if (fogUv.x < 0.0 || fogUv.x > 1.0 || fogUv.y < 0.0 || fogUv.y > 1.0)
                {
                    return _OutsideFogColor;
                }

                // 实时可见圆：VisionRadius > 0 时在 Shader 端逐像素计算，零延迟跟随玩家
                if (_VisionRadius > 0.0)
                {
                    float dist = distance(positionWS.xz, _VisionCenter.xy);
                    float softEdge = max(_VisionRadius * 0.15, _FogBounds.z / max(_FogTex_TexelSize.z, 1.0));
                    half visionAlpha = (half)smoothstep(_VisionRadius - softEdge, _VisionRadius, dist);
                    if (visionAlpha < 0.001h)
                    {
                        return _VisibleColor;
                    }
                }

                half4 center = SAMPLE_TEXTURE2D(_FogTex, sampler_FogTex, fogUv);
                if (_EdgeSoftness <= 0.001)
                {
                    // 实时可见圆过渡区混合
                    if (_VisionRadius > 0.0)
                    {
                        float dist2 = distance(positionWS.xz, _VisionCenter.xy);
                        float softEdge2 = max(_VisionRadius * 0.15, _FogBounds.z / max(_FogTex_TexelSize.z, 1.0));
                        half visionAlpha2 = (half)smoothstep(_VisionRadius - softEdge2, _VisionRadius, dist2);
                        return lerp(_VisibleColor, center, visionAlpha2);
                    }
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
                half4 fogColor = lerp(center, blur, blurBlend);

                // 实时可见圆过渡区混合
                if (_VisionRadius > 0.0)
                {
                    float dist3 = distance(positionWS.xz, _VisionCenter.xy);
                    float softEdge3 = max(_VisionRadius * 0.15, _FogBounds.z / max(_FogTex_TexelSize.z, 1.0));
                    half visionAlpha3 = (half)smoothstep(_VisionRadius - softEdge3, _VisionRadius, dist3);
                    return lerp(_VisibleColor, fogColor, visionAlpha3);
                }

                return fogColor;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
