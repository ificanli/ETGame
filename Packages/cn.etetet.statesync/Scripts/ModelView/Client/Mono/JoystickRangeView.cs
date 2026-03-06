using UnityEngine;
using UnityEngine.EventSystems;

namespace ET.Client
{
    /// <summary>
    /// 摇杆范围控件，挂在JoyStickRange上。
    /// 在范围内点击时，将摇杆背景移动到点击位置，并激活摇杆。
    /// </summary>
    [EnableClass]
    public class JoystickRangeView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("摇杆组件引用")]
        [Tooltip("摇杆背景的RectTransform")]
        public RectTransform JoystickBackground;

        [Tooltip("摇杆View脚本")]
        public JoystickView JoystickView;

        [Tooltip("范围区域的RectTransform（即本脚本所在GameObject）")]
        public RectTransform RangeRect;

        private bool m_IsActive;

        public void OnPointerDown(PointerEventData eventData)
        {
            Debug.Log("[JoystickRange] OnPointerDown triggered");

            if (JoystickBackground == null || JoystickView == null || RangeRect == null)
            {
                Debug.LogWarning($"[JoystickRange] 组件引用未设置: Background={JoystickBackground != null}, View={JoystickView != null}, Range={RangeRect != null}");
                return;
            }

            // 检查是否点击到了摇杆本身或其他UI元素
            if (IsPointerOverOtherUIElement(eventData))
            {
                Debug.Log("[JoystickRange] 点击到了摇杆或其他UI元素，让它们自己处理");
                return;
            }

            // 将屏幕坐标转换为RangeRect的本地坐标
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                RangeRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
            {
                Debug.Log($"[JoystickRange] 移动摇杆到位置: {localPoint}");

                // 移动摇杆背景到点击位置
                JoystickBackground.anchoredPosition = localPoint;

                // 激活拖拽状态
                m_IsActive = true;

                // 触发摇杆的点击事件
                JoystickView.OnPointerDown(eventData);
            }
            else
            {
                Debug.LogWarning("[JoystickRange] 坐标转换失败");
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!m_IsActive) return;

            // 将拖拽事件传递给摇杆
            JoystickView.OnDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!m_IsActive) return;

            m_IsActive = false;

            // 将松手事件传递给摇杆
            JoystickView.OnPointerUp(eventData);

            // 将摇杆背景重置到初始位置（中心）
            if (JoystickBackground != null)
            {
                JoystickBackground.anchoredPosition = Vector2.zero;
            }
        }

        /// <summary>
        /// 检查点击位置是否在摇杆或其他UI元素上
        /// </summary>
        private bool IsPointerOverOtherUIElement(PointerEventData eventData)
        {
            // 获取点击位置下的所有UI元素
            var results = new System.Collections.Generic.List<RaycastResult>();
            var currentEventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (currentEventSystem == null)
            {
                return false;
            }

            currentEventSystem.RaycastAll(eventData, results);

            foreach (var result in results)
            {
                // 如果点击到的是自己（Range），跳过继续检查
                if (result.gameObject == gameObject)
                {
                    continue;
                }

                // 如果点击到了摇杆背景或摇杆本身，返回true让它们自己处理
                if (result.gameObject == JoystickBackground.gameObject ||
                    result.gameObject == JoystickView.gameObject ||
                    result.gameObject.transform.IsChildOf(JoystickBackground.transform))
                {
                    Debug.Log($"[JoystickRange] 检测到点击摇杆: {result.gameObject.name}");
                    return true;
                }

                // 如果点击到了其他可交互的UI元素（如按钮），返回true
                if (result.gameObject.GetComponent<UnityEngine.UI.Selectable>() != null)
                {
                    Debug.Log($"[JoystickRange] 检测到点击其他UI: {result.gameObject.name}");
                    return true;
                }
            }

            // 点击的是Range的空白区域
            return false;
        }
    }
}
