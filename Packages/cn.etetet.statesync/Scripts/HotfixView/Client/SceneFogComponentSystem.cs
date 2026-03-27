using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;

namespace ET.Client
{
    /// <summary>
    /// 管理场景迷雾覆盖层的创建、贴图刷新与主相机投射。
    /// </summary>
    [EntitySystemOf(typeof(SceneFogComponent))]
    public static partial class SceneFogComponentSystem
    {
        private const string OverlayObjectName = "SceneFogOverlay";
        private const string OverlayShaderName = "ET/SceneFogOverlay";
        private const float DefaultRefreshInterval = 0.1f;
        private const float DefaultEdgeSoftness = 1.2f;
        private const float DefaultExploredAlphaScale = 0.75f;
        private const float MinRefreshInterval = 0.02f;
        private const float OverlayDistanceOffset = 0.05f;
        private const float DebugRadiusStep = 1f;
        private const long DiagnosticLogIntervalMs = 1000;

        [EntitySystem]
        private static void Awake(this SceneFogComponent self)
        {
            self.RuntimeMapName = string.Empty;
            self.OverlayObject = null;
            self.ProjectorView = null;
            self.OverlayMaterial = null;
            self.FogTexture = null;
            self.OverlayMesh = null;
            self.FogPixels = null;
            self.RefreshInterval = DefaultRefreshInterval;
            self.EdgeSoftness = DefaultEdgeSoftness;
            self.ExploredAlphaScale = DefaultExploredAlphaScale;
            self.LastRefreshTime = float.MinValue;
            self.ShaderMissingLogged = false;
            self.LastDiagnosticLogTime = 0;
            self.LastDiagnosticSignature = string.Empty;
            self.LoggedDebugKeys = new System.Collections.Generic.HashSet<string>();
            Log.Info("[SceneFogDebug] SceneFogComponent created");
        }

        [EntitySystem]
        private static void Destroy(this SceneFogComponent self)
        {
            self.DestroyOverlayResources();
            self.RuntimeMapName = string.Empty;
            self.RefreshInterval = DefaultRefreshInterval;
            self.EdgeSoftness = DefaultEdgeSoftness;
            self.ExploredAlphaScale = DefaultExploredAlphaScale;
            self.LastRefreshTime = float.MinValue;
            self.ShaderMissingLogged = false;
            self.LastDiagnosticLogTime = 0;
            self.LastDiagnosticSignature = string.Empty;
            self.LoggedDebugKeys?.Clear();
            self.LoggedDebugKeys = null;
        }

        [EntitySystem]
        private static void Update(this SceneFogComponent self)
        {
            Scene scene = self.GetParent<Scene>();
            if (scene == null || scene.IsDisposed)
            {
                return;
            }

            MinimapRuntimeComponent runtime = scene.GetComponent<MinimapRuntimeComponent>();
            if (runtime == null)
            {
                self.LogDebugOnce("no_runtime", "[SceneFogDebug] MinimapRuntimeComponent is null");
                self.SetOverlayVisible(false);
                return;
            }

            if (runtime.ShouldRetryResolveWorldBounds())
            {
                runtime.RefreshWorldBoundsFromTerrain();
            }

            self.HandleDebugRadiusInput(runtime);
            self.RefreshRuntimeSettings(runtime);
            self.RefreshFogTexture(runtime);
            if (self.FogTexture == null)
            {
                self.LogDebugOnce("no_texture", $"[SceneFogDebug] FogTexture is null, mapName={runtime.MapName}, worldBounds=({runtime.WorldMinX},{runtime.WorldMinZ})-({runtime.WorldMaxX},{runtime.WorldMaxZ}), fogCellSize={runtime.FogCellSize}");
                self.SetOverlayVisible(false);
                return;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                self.LogDebugOnce("no_camera", "[SceneFogDebug] Camera.main is null");
                return;
            }

            camera.depthTextureMode |= DepthTextureMode.Depth;
            if (!self.EnsureOverlay(camera))
            {
                self.LogDebugOnce("no_overlay", "[SceneFogDebug] EnsureOverlay failed");
                self.SetOverlayVisible(false);
                return;
            }

            self.UpdateOverlayTransform(camera);
            self.UpdateOverlayMaterial(runtime);
            self.SetOverlayVisible(true);
            
            // 详细的调试信息
            if (self.ProjectorView?.MeshRenderer != null)
            {
                var mr = self.ProjectorView.MeshRenderer;
                var mat = mr.sharedMaterial;
                var bounds = runtime != null ? new Vector4(runtime.WorldMinX, runtime.WorldMinZ, runtime.WorldMaxX - runtime.WorldMinX, runtime.WorldMaxZ - runtime.WorldMinZ) : Vector4.zero;
                self.LogDebugOnce("success", $"[SceneFogDebug] Fog overlay active, mapName={runtime.MapName}, texSize={self.FogTexture.width}x{self.FogTexture.height}, " +
                    $"overlayLayer={self.OverlayObject?.layer}, camCullingMask={camera.cullingMask}, " +
                    $"rendererEnabled={mr.enabled}, materialValid={mat != null}, " +
                    $"fogBounds=({bounds.x},{bounds.y},{bounds.z},{bounds.w}), " +
                    $"camPos={camera.transform.position}, camNear={camera.nearClipPlane}, camFar={camera.farClipPlane}");
            }
            else
            {
                self.LogDebugOnce("success", $"[SceneFogDebug] Fog overlay active, mapName={runtime.MapName}, texSize={self.FogTexture.width}x{self.FogTexture.height}");
            }
        }

        private static void RefreshRuntimeSettings(this SceneFogComponent self, MinimapRuntimeComponent runtime)
        {
            string mapName = runtime.MapName ?? string.Empty;
            if (self.RuntimeMapName == mapName)
            {
                return;
            }

            self.RuntimeMapName = mapName;
            string refreshKey = global::ET.MinimapConstKey.GetMapKey(mapName, global::ET.MinimapConstKey.SceneFogRefreshInterval);
            float globalRefreshInterval = global::ET.MinimapConstConfigHelper.GetFloat(global::ET.MinimapConstKey.SceneFogRefreshInterval, DefaultRefreshInterval);
            self.RefreshInterval = Mathf.Max(
                MinRefreshInterval,
                global::ET.MinimapConstConfigHelper.GetFloat(refreshKey, globalRefreshInterval));
            string edgeSoftnessKey = global::ET.MinimapConstKey.GetMapKey(mapName, global::ET.MinimapConstKey.SceneFogEdgeSoftness);
            float globalEdgeSoftness = global::ET.MinimapConstConfigHelper.GetFloat(global::ET.MinimapConstKey.SceneFogEdgeSoftness, DefaultEdgeSoftness);
            self.EdgeSoftness = Mathf.Max(0f, global::ET.MinimapConstConfigHelper.GetFloat(edgeSoftnessKey, globalEdgeSoftness));
            string exploredAlphaScaleKey = global::ET.MinimapConstKey.GetMapKey(mapName, global::ET.MinimapConstKey.SceneFogExploredAlphaScale);
            float globalExploredAlphaScale = global::ET.MinimapConstConfigHelper.GetFloat(
                global::ET.MinimapConstKey.SceneFogExploredAlphaScale,
                DefaultExploredAlphaScale);
            self.ExploredAlphaScale = Mathf.Clamp01(
                global::ET.MinimapConstConfigHelper.GetFloat(exploredAlphaScaleKey, globalExploredAlphaScale));
            self.LastRefreshTime = float.MinValue;
        }

        private static void RefreshFogTexture(this SceneFogComponent self, MinimapRuntimeComponent runtime)
        {
            if (!runtime.TryGetFogGridSize(out int gridWidth, out int gridHeight))
            {
                self.DestroyFogTexture();
                return;
            }

            self.EnsureFogTexture(gridWidth, gridHeight);

            float now = Time.unscaledTime;
            if (self.LastRefreshTime > float.MinValue && now - self.LastRefreshTime < self.RefreshInterval)
            {
                return;
            }

            runtime.RefreshLocalFog();

            // 贴图只写已探索/未探索两种状态，可见区域由 Shader 实时计算
            Color32 exploredColor = self.ScaleColorAlpha(
                self.ResolveSceneFogColor(runtime, global::ET.MinimapConstKey.FogColorExplored, "#000020C0"),
                self.ExploredAlphaScale);
            Color32 unexploredColor = self.ResolveSceneFogColor(runtime, global::ET.MinimapConstKey.FogColorUnexplored, "#000010F0");

            int totalCount = gridWidth * gridHeight;
            if (self.FogPixels == null || self.FogPixels.Length != totalCount)
            {
                self.FogPixels = new Color32[totalCount];
            }

            for (int i = 0; i < totalCount; ++i)
            {
                self.FogPixels[i] = runtime.ExploredCells.Contains(i) ? exploredColor : unexploredColor;
            }

            self.FogTexture.SetPixels32(self.FogPixels);
            self.FogTexture.Apply(false, false);
            self.LastRefreshTime = now;
            self.LogFogDiagnostics(runtime, gridWidth, gridHeight);
        }

        private static void EnsureFogTexture(this SceneFogComponent self, int width, int height)
        {
            if (self.FogTexture != null && self.FogTexture.width == width && self.FogTexture.height == height)
            {
                return;
            }

            self.DestroyFogTexture();
            self.FogTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "SceneFogTexture",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            self.LastRefreshTime = float.MinValue;
        }

        private static bool EnsureOverlay(this SceneFogComponent self, Camera camera)
        {
            if (self.OverlayObject == null || self.ProjectorView == null || self.OverlayMaterial == null)
            {
                return self.CreateOverlay(camera);
            }

            if (self.OverlayObject.transform.parent != camera.transform)
            {
                self.OverlayObject.transform.SetParent(camera.transform, false);
            }

            if (self.ProjectorView.MeshFilter != null && self.ProjectorView.MeshFilter.sharedMesh != self.OverlayMesh)
            {
                self.ProjectorView.MeshFilter.sharedMesh = self.OverlayMesh;
            }

            if (self.ProjectorView.MeshRenderer != null && self.ProjectorView.MeshRenderer.sharedMaterial != self.OverlayMaterial)
            {
                self.ProjectorView.MeshRenderer.sharedMaterial = self.OverlayMaterial;
            }

            return self.ProjectorView.MeshFilter != null && self.ProjectorView.MeshRenderer != null;
        }

        private static bool CreateOverlay(this SceneFogComponent self, Camera camera)
        {
            Shader shader = Shader.Find(OverlayShaderName);
            if (shader == null)
            {
                if (!self.ShaderMissingLogged)
                {
                    Log.Warning($"[SceneFog] missing shader: {OverlayShaderName}");
                    self.ShaderMissingLogged = true;
                }
                return false;
            }

            self.ShaderMissingLogged = false;
            self.DestroyOverlayPresentation();
            self.EnsureOverlayMesh();

            GameObject overlayObject = new GameObject(OverlayObjectName, typeof(MeshFilter), typeof(MeshRenderer), typeof(SceneFogProjectorView));
            // 使用Default layer (0) 确保被大多数相机渲染
            overlayObject.layer = 0;
            overlayObject.transform.SetParent(camera.transform, false);

            SceneFogProjectorView view = overlayObject.GetComponent<SceneFogProjectorView>();
            view.MeshFilter = overlayObject.GetComponent<MeshFilter>();
            view.MeshRenderer = overlayObject.GetComponent<MeshRenderer>();
            view.MeshFilter.sharedMesh = self.OverlayMesh;

            Material material = new Material(shader)
            {
                name = "SceneFogOverlayMaterial",
            };

            MeshRenderer meshRenderer = view.MeshRenderer;
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            meshRenderer.allowOcclusionWhenDynamic = false;
            meshRenderer.sortingOrder = short.MaxValue;
            meshRenderer.enabled = false;

            self.OverlayObject = overlayObject;
            self.ProjectorView = view;
            self.OverlayMaterial = material;
            return true;
        }

        private static void EnsureOverlayMesh(this SceneFogComponent self)
        {
            if (self.OverlayMesh != null)
            {
                return;
            }

            self.OverlayMesh = new Mesh
            {
                name = "SceneFogOverlayMesh",
            };
            self.OverlayMesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
            };
            self.OverlayMesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f),
            };
            self.OverlayMesh.normals = new[]
            {
                Vector3.forward,
                Vector3.forward,
                Vector3.forward,
                Vector3.forward,
            };
            self.OverlayMesh.triangles = new[]
            {
                0, 1, 2,
                0, 2, 3,
            };
            self.OverlayMesh.RecalculateBounds();
        }

        private static void UpdateOverlayTransform(this SceneFogComponent self, Camera camera)
        {
            if (self.OverlayObject == null)
            {
                return;
            }

            Transform overlayTransform = self.OverlayObject.transform;
            float distance = Mathf.Max(camera.nearClipPlane + OverlayDistanceOffset, camera.nearClipPlane + 0.001f);
            float aspect = Mathf.Max(camera.aspect, 0.0001f);
            float height = camera.orthographic
                ? camera.orthographicSize * 2f
                : 2f * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * distance;
            float width = height * aspect;

            overlayTransform.localPosition = new Vector3(0f, 0f, distance);
            overlayTransform.localRotation = Quaternion.identity;
            overlayTransform.localScale = new Vector3(width, height, 1f);
        }

        private static void UpdateOverlayMaterial(this SceneFogComponent self, MinimapRuntimeComponent runtime)
        {
            if (self.OverlayMaterial == null || self.FogTexture == null)
            {
                return;
            }

            float width = runtime.WorldMaxX - runtime.WorldMinX;
            float height = runtime.WorldMaxZ - runtime.WorldMinZ;
            if (width <= 0f || height <= 0f)
            {
                return;
            }

            Color outsideFogColor = self.ResolveSceneFogColor(runtime, global::ET.MinimapConstKey.FogColorUnexplored, "#09131EE8");
            float fogPlaneY = 0f;
            float visionCenterX = 0f;
            float visionCenterZ = 0f;
            Unit myUnit = runtime.GetMyUnit();
            if (myUnit != null && !myUnit.IsDisposed)
            {
                // 优先读 GameObject Transform，实时跟随渲染位置（无网络延迟）
                Transform unitTransform = myUnit.GetComponent<GameObjectComponent>()?.Transform;
                Vector3 pos = unitTransform != null ? unitTransform.position : (Vector3)myUnit.Position;
                fogPlaneY = pos.y;
                visionCenterX = pos.x;
                visionCenterZ = pos.z;
            }
            else if (runtime.TryGetMyPosition(out float3 myPosition))
            {
                fogPlaneY = myPosition.y;
                visionCenterX = myPosition.x;
                visionCenterZ = myPosition.z;
            }

            // 实时可见圆：FogVisionRadius > 0 时传玩家位置和半径给 Shader
            float visionRadius = runtime.FogVisionRadius > 0f ? runtime.FogVisionRadius : 0f;
            Color visibleColor = self.ResolveSceneFogColor(runtime, global::ET.MinimapConstKey.FogColorVisible, "#00000000");

            self.OverlayMaterial.SetTexture("_FogTex", self.FogTexture);
            self.OverlayMaterial.SetVector("_FogBounds", new Vector4(runtime.WorldMinX, runtime.WorldMinZ, width, height));
            self.OverlayMaterial.SetColor("_OutsideFogColor", outsideFogColor);
            self.OverlayMaterial.SetFloat("_EdgeSoftness", self.EdgeSoftness);
            self.OverlayMaterial.SetFloat("_FogPlaneY", fogPlaneY);
            self.OverlayMaterial.SetVector("_VisionCenter", new Vector4(visionCenterX, visionCenterZ, 0f, 0f));
            self.OverlayMaterial.SetFloat("_VisionRadius", visionRadius);
            self.OverlayMaterial.SetColor("_VisibleColor", visibleColor);

            // 调试模式：
            // 0 = 正常迷雾渲染
            // 1 = 纯红色（测试shader执行）
            // 2 = 显示深度值（灰度）
            // 3 = 分支调试（蓝=天空, 黄=水平射线, 紫=交点在后, 橙=超出边界, 绿=正常区域）
            // 4 = 显示迷雾纹理采样值（alpha通道，黑=可见，白=未探索）
            // 5 = 显示UV坐标（R=U, G=V）
            self.OverlayMaterial.SetFloat("_DebugMode", 0f);
        }

        private static void HandleDebugRadiusInput(this SceneFogComponent self, MinimapRuntimeComponent runtime)
        {
#if UNITY_EDITOR
            if (runtime == null)
            {
                return;
            }

            float delta = 0f;
            if (Input.GetKeyDown(KeyCode.LeftBracket) || Input.GetKeyDown(KeyCode.KeypadMinus) || Input.GetKeyDown(KeyCode.Minus))
            {
                delta -= DebugRadiusStep;
            }

            if (Input.GetKeyDown(KeyCode.RightBracket) || Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.Equals))
            {
                delta += DebugRadiusStep;
            }

            if (Mathf.Abs(delta) <= 0.0001f)
            {
                return;
            }

            runtime.FogVisionRadius = Mathf.Max(0f, runtime.FogVisionRadius + delta);
            runtime.RefreshLocalFog();
            self.LastRefreshTime = float.MinValue;
            self.LastDiagnosticLogTime = 0;
            self.LastDiagnosticSignature = string.Empty;

            Log.Info(
                $"[SceneFogDebug] map={runtime.MapName}, fogVisionRadius={runtime.FogVisionRadius:F1}, visibleCells={runtime.CurrentVisibleCells.Count}, exploredCells={runtime.ExploredCells.Count}");
#endif
        }

        private static void LogFogDiagnostics(this SceneFogComponent self, MinimapRuntimeComponent runtime, int gridWidth, int gridHeight)
        {
            if (runtime == null || !string.Equals(runtime.MapName, "SDCMap"))
            {
                return;
            }

            long nowMs = TimeInfo.Instance.ClientNow();
            bool shouldLogByTime = nowMs - self.LastDiagnosticLogTime >= DiagnosticLogIntervalMs;

            Scene scene = self.GetParent<Scene>();
            UnitComponent unitComponent = scene?.GetComponent<UnitComponent>();
            Unit myUnit = runtime.GetMyUnit();
            bool hasMyPosition = runtime.TryGetMyPosition(out float3 myPosition);
            bool inBounds = hasMyPosition &&
                myPosition.x >= runtime.WorldMinX &&
                myPosition.x <= runtime.WorldMaxX &&
                myPosition.z >= runtime.WorldMinZ &&
                myPosition.z <= runtime.WorldMaxZ;

            int unitCount = unitComponent?.Children?.Count ?? 0;
            int friendlyCount = 0;
            int friendlyWithAoiCount = 0;
            int maxFriendlyRawAoi = 0;
            int myRawAoi = myUnit?.NumericComponent?.GetAsInt(NumericType.AOI) ?? 0;
            int myAoiCellRadius = MinimapRuntimeComponentSystem.ResolveAoiCellRadius(myRawAoi);
            float fogVisionRadius = runtime.FogVisionRadius;
            string firstFriendlySummary = "none";

            if (unitComponent != null && myUnit != null && !myUnit.IsDisposed)
            {
                foreach (Unit unit in unitComponent.Children.Values)
                {
                    if (unit == null || unit.IsDisposed || !CampHelper.IsFriendly(myUnit, unit))
                    {
                        continue;
                    }

                    ++friendlyCount;
                    int rawAoi = unit.NumericComponent?.GetAsInt(NumericType.AOI) ?? 0;
                    if (rawAoi > 0)
                    {
                        ++friendlyWithAoiCount;
                    }

                    if (rawAoi > maxFriendlyRawAoi)
                    {
                        maxFriendlyRawAoi = rawAoi;
                    }

                    if (firstFriendlySummary == "none")
                    {
                        int aoiCellRadius = MinimapRuntimeComponentSystem.ResolveAoiCellRadius(rawAoi);
                        firstFriendlySummary =
                            $"id={unit.Id},rawAoi={rawAoi},aoiCellRadius={aoiCellRadius},pos=({unit.Position.x:F1},{unit.Position.y:F1},{unit.Position.z:F1})";
                    }
                }
            }

            int fogWidth = self.FogTexture?.width ?? 0;
            int fogHeight = self.FogTexture?.height ?? 0;
            string signature =
                $"{runtime.MyUnitId}|{runtime.WorldMinX:F1}|{runtime.WorldMaxX:F1}|{runtime.WorldMinZ:F1}|{runtime.WorldMaxZ:F1}|{runtime.FogCellSize:F1}|{fogVisionRadius:F1}|{gridWidth}|{gridHeight}|{runtime.CurrentVisibleCells.Count}|{runtime.ExploredCells.Count}|{friendlyCount}|{friendlyWithAoiCount}|{myRawAoi}|{myAoiCellRadius}|{inBounds}|{fogWidth}|{fogHeight}|{myPosition.y:F1}";

            if (!shouldLogByTime && string.Equals(signature, self.LastDiagnosticSignature))
            {
                return;
            }

            self.LastDiagnosticLogTime = nowMs;
            self.LastDiagnosticSignature = signature;

            string myPositionText = hasMyPosition
                ? $"({myPosition.x:F1},{myPosition.y:F1},{myPosition.z:F1})"
                : "unavailable";
            string boundsText =
                $"min=({runtime.WorldMinX:F1},{runtime.WorldMinZ:F1}),max=({runtime.WorldMaxX:F1},{runtime.WorldMaxZ:F1})";

            Log.Info(
                $"[SceneFogDiag] map={runtime.MapName}, myUnitId={runtime.MyUnitId}, myPos={myPositionText}, inBounds={inBounds}, bounds={boundsText}, " +
                $"grid={gridWidth}x{gridHeight}, cellSize={runtime.FogCellSize:F1}, fogVisionRadius={fogVisionRadius:F1}, visibleCells={runtime.CurrentVisibleCells.Count}, exploredCells={runtime.ExploredCells.Count}, " +
                $"units={unitCount}, friendly={friendlyCount}, friendlyWithAOI={friendlyWithAoiCount}, myRawAOI={myRawAoi}, myAOICellRadius={myAoiCellRadius}, maxFriendlyRawAOI={maxFriendlyRawAoi}, " +
                $"fogTex={fogWidth}x{fogHeight}, fogPlaneY={myPosition.y:F1}, refreshInterval={self.RefreshInterval:F2}, edgeSoftness={self.EdgeSoftness:F2}, exploredAlphaScale={self.ExploredAlphaScale:F2}, firstFriendly={firstFriendlySummary}");
        }

        private static Color32 ResolveSceneFogColor(this SceneFogComponent self, MinimapRuntimeComponent runtime, string key, string defaultColor)
        {
            string colorText = string.Empty;
            string mapName = runtime?.MapName ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(mapName))
            {
                colorText = global::ET.MinimapConstConfigHelper.GetString(global::ET.MinimapConstKey.GetMapKey(mapName, key), string.Empty);
            }

            if (string.IsNullOrWhiteSpace(colorText))
            {
                colorText = global::ET.MinimapConstConfigHelper.GetString(key, defaultColor);
            }

            if (ColorUtility.TryParseHtmlString(colorText, out Color color))
            {
                return color;
            }

            return Color.clear;
        }

        private static Color32 ScaleColorAlpha(this SceneFogComponent self, Color32 color, float alphaScale)
        {
            byte alpha = (byte)Mathf.Clamp(Mathf.RoundToInt(color.a * Mathf.Clamp01(alphaScale)), 0, byte.MaxValue);
            return new Color32(color.r, color.g, color.b, alpha);
        }

        private static void SetOverlayVisible(this SceneFogComponent self, bool visible)
        {
            if (self.ProjectorView?.MeshRenderer == null)
            {
                return;
            }

            self.ProjectorView.MeshRenderer.enabled = visible;
        }

        private static void DestroyFogTexture(this SceneFogComponent self)
        {
            if (self.FogTexture != null)
            {
                UnityEngine.Object.Destroy(self.FogTexture);
            }

            self.FogTexture = null;
            self.FogPixels = null;
            self.LastRefreshTime = float.MinValue;
        }

        private static void DestroyOverlayPresentation(this SceneFogComponent self)
        {
            if (self.OverlayObject != null)
            {
                UnityEngine.Object.Destroy(self.OverlayObject);
            }

            if (self.OverlayMaterial != null)
            {
                UnityEngine.Object.Destroy(self.OverlayMaterial);
            }

            self.OverlayObject = null;
            self.ProjectorView = null;
            self.OverlayMaterial = null;
        }

        private static void DestroyOverlayResources(this SceneFogComponent self)
        {
            self.DestroyOverlayPresentation();
            self.DestroyFogTexture();

            if (self.OverlayMesh != null)
            {
                UnityEngine.Object.Destroy(self.OverlayMesh);
            }

            self.OverlayMesh = null;
        }

        private static void LogDebugOnce(this SceneFogComponent self, string key, string message)
        {
            if (self.LoggedDebugKeys == null)
            {
                self.LoggedDebugKeys = new System.Collections.Generic.HashSet<string>();
            }

            if (self.LoggedDebugKeys.Contains(key))
            {
                return;
            }

            self.LoggedDebugKeys.Add(key);
            Log.Info(message);
        }
    }
}
