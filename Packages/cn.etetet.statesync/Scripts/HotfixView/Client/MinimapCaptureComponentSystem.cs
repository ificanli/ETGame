using UnityEngine;

namespace ET.Client
{
    [EntitySystemOf(typeof(MinimapCaptureComponent))]
    public static partial class MinimapCaptureComponentSystem
    {
        private const string CaptureObjectName = "MinimapCaptureCamera";
        private const float MinSpan = 16f;
        private const float MinCameraHeight = 192f;
        private const float HeightPadding = 128f;
        private const float MinFarClip = 512f;

        [EntitySystem]
        private static void Awake(this MinimapCaptureComponent self)
        {
            self.CameraObject = null;
            self.CaptureCamera = null;
            self.TargetTexture = null;
            self.MissingTargetLogged = false;
        }

        [EntitySystem]
        private static void Destroy(this MinimapCaptureComponent self)
        {
            self.DestroyCaptureCamera();
            self.TargetTexture = null;
            self.MissingTargetLogged = false;
        }

        [EntitySystem]
        private static void Update(this MinimapCaptureComponent self)
        {
            Scene scene = self.GetParent<Scene>();
            if (scene == null || scene.IsDisposed)
            {
                return;
            }

            MinimapRuntimeComponent runtime = scene.GetComponent<MinimapRuntimeComponent>();
            if (runtime == null)
            {
                self.SetCaptureActive(false);
                return;
            }

            if (runtime.ShouldRetryResolveWorldBounds())
            {
                runtime.RefreshWorldBoundsFromTerrain();
            }

            if (!self.TryResolveTargetTexture(scene.Root(), out RenderTexture targetTexture))
            {
                self.SetCaptureActive(false);
                return;
            }

            self.MissingTargetLogged = false;
            if (!self.EnsureCaptureCamera())
            {
                return;
            }

            self.RefreshCaptureCamera(runtime, targetTexture);
            self.RenderCapture(scene);
        }

        private static bool TryResolveTargetTexture(this MinimapCaptureComponent self, Scene root, out RenderTexture targetTexture)
        {
            targetTexture = null;
            MainPanelComponent mainPanel = root?.YIUIMgr()?.GetPanel<MainPanelComponent>();
            if (mainPanel?.MinimapTexture?.texture is not RenderTexture renderTexture)
            {
                if (!self.MissingTargetLogged)
                {
                    Log.Warning("[MinimapCapture] missing render texture on MainPanel.MinimapTexture");
                    self.MissingTargetLogged = true;
                }

                return false;
            }

            targetTexture = renderTexture;
            return true;
        }

        private static bool EnsureCaptureCamera(this MinimapCaptureComponent self)
        {
            if (self.CameraObject != null && self.CaptureCamera != null)
            {
                return true;
            }

            self.DestroyCaptureCamera();

            GameObject cameraObject = new GameObject(CaptureObjectName, typeof(Camera));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;

            Camera captureCamera = cameraObject.GetComponent<Camera>();
            captureCamera.orthographic = true;
            captureCamera.clearFlags = CameraClearFlags.SolidColor;
            captureCamera.backgroundColor = Color.black;
            captureCamera.allowHDR = false;
            captureCamera.allowMSAA = false;
            captureCamera.useOcclusionCulling = false;
            captureCamera.enabled = false;

            self.CameraObject = cameraObject;
            self.CaptureCamera = captureCamera;
            return true;
        }

        private static void RefreshCaptureCamera(
            this MinimapCaptureComponent self,
            MinimapRuntimeComponent runtime,
            RenderTexture targetTexture)
        {
            Camera captureCamera = self.CaptureCamera;
            if (captureCamera == null)
            {
                return;
            }

            float width = runtime.WorldMaxX - runtime.WorldMinX;
            float height = runtime.WorldMaxZ - runtime.WorldMinZ;
            if (width <= 0f || height <= 0f)
            {
                self.SetCaptureActive(false);
                return;
            }

            float aspect = targetTexture.height > 0 ? targetTexture.width / (float)targetTexture.height : 1f;
            float spanX = Mathf.Max(width, MinSpan);
            float spanZ = Mathf.Max(height, MinSpan);
            float orthographicSize = Mathf.Max(
                spanZ * 0.5f,
                spanX * 0.5f / Mathf.Max(aspect, 0.0001f));
            float centerX = (runtime.WorldMinX + runtime.WorldMaxX) * 0.5f;
            float centerZ = (runtime.WorldMinZ + runtime.WorldMaxZ) * 0.5f;
            float cameraHeight = Mathf.Max(MinCameraHeight, Mathf.Max(spanX, spanZ) + HeightPadding);

            Camera mainCamera = Camera.main;
            captureCamera.cullingMask = mainCamera != null ? mainCamera.cullingMask : ~0;
            captureCamera.aspect = aspect;
            captureCamera.targetTexture = targetTexture;
            captureCamera.orthographicSize = orthographicSize;
            captureCamera.nearClipPlane = 0.3f;
            captureCamera.farClipPlane = Mathf.Max(MinFarClip, cameraHeight * 2f);
            captureCamera.transform.position = new Vector3(centerX, cameraHeight, centerZ);
            captureCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            self.TargetTexture = targetTexture;
            self.SetCaptureActive(true);
        }

        private static void RenderCapture(this MinimapCaptureComponent self, Scene scene)
        {
            Camera captureCamera = self.CaptureCamera;
            if (captureCamera == null || self.CameraObject == null || !self.CameraObject.activeInHierarchy || self.TargetTexture == null)
            {
                return;
            }

            MeshRenderer fogRenderer = scene?.GetComponent<SceneFogComponent>()?.ProjectorView?.MeshRenderer;
            bool restoreFogRenderer = fogRenderer != null;
            bool fogRendererEnabled = restoreFogRenderer && fogRenderer.enabled;
            if (restoreFogRenderer)
            {
                fogRenderer.enabled = false;
            }

            try
            {
                captureCamera.Render();
            }
            finally
            {
                if (restoreFogRenderer)
                {
                    fogRenderer.enabled = fogRendererEnabled;
                }
            }
        }

        private static void SetCaptureActive(this MinimapCaptureComponent self, bool active)
        {
            if (self.CameraObject == null)
            {
                return;
            }

            if (self.CameraObject.activeSelf != active)
            {
                self.CameraObject.SetActive(active);
            }

            if (self.CaptureCamera != null && self.CaptureCamera.enabled != active)
            {
                self.CaptureCamera.enabled = false;
            }
        }

        private static void DestroyCaptureCamera(this MinimapCaptureComponent self)
        {
            if (self.CameraObject != null)
            {
                UnityEngine.Object.Destroy(self.CameraObject);
            }

            self.CameraObject = null;
            self.CaptureCamera = null;
        }
    }
}
