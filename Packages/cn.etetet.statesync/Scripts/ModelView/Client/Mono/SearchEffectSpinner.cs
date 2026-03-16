using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 搜索动效旋转组件，用于实现转圈圈的加载动画
    /// </summary>
    [EnableClass]
    public class SearchEffectSpinner : MonoBehaviour
    {
        /// <summary>
        /// 旋转速度（度/秒）
        /// </summary>
        public float RotationSpeed = 360f;

        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (_rectTransform == null)
            {
                return;
            }

            // 绕Z轴旋转
            float deltaAngle = RotationSpeed * Time.deltaTime;
            _rectTransform.Rotate(0f, 0f, -deltaAngle);
        }
    }
}