using System.Reflection;
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 按 Buff 时长驱动线形预警的填充进度，避免始终满格显示。
    /// </summary>
    [EnableClass]
    public class LineRegionFillProgressDriver: MonoBehaviour
    {
        private Component lineRegion;

        private PropertyInfo fillProgressProperty;

        private float startTime;

        private float durationSeconds;

        private bool isInitialized;

        public void Init(Component targetLineRegion, int durationMs)
        {
            this.lineRegion = targetLineRegion;
            this.fillProgressProperty = this.lineRegion?.GetType().GetProperty("FillProgress");
            this.durationSeconds = Mathf.Max(durationMs / 1000f, 0.01f);
            this.startTime = Time.unscaledTime;
            this.isInitialized = true;

            this.CreateMaterialInstances();
            this.SetFillProgress(0f);
        }

        private void Update()
        {
            if (!this.isInitialized || this.lineRegion == null || this.fillProgressProperty == null)
            {
                Destroy(this);
                return;
            }

            float progress = Mathf.Clamp01((Time.unscaledTime - this.startTime) / this.durationSeconds);
            this.SetFillProgress(progress);
            if (progress >= 1f)
            {
                Destroy(this);
            }
        }

        private void SetFillProgress(float progress)
        {
            this.fillProgressProperty?.SetValue(this.lineRegion, progress);
        }

        private void CreateMaterialInstances()
        {
            foreach (MeshRenderer renderer in this.lineRegion.GetComponentsInChildren<MeshRenderer>(true))
            {
                renderer.materials = renderer.materials;
            }
        }
    }
}
