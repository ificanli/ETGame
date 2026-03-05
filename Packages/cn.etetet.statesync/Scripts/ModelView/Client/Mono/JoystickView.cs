using UnityEngine;
using UnityEngine.EventSystems;

namespace ET.Client
{
    /// <summary>
    /// 摇杆UI控件（纯MonoBehaviour）。
    /// 挂在摇杆小球GameObject上，Background/Knob通过Inspector赋值。
    /// 需要HotfixView层调用 SetEntity 注入Entity引用，才能发送ET事件。
    /// </summary>
    [EnableClass]
    public class JoystickView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("摇杆组件")]
        [Tooltip("背景圆盘的RectTransform")]
        public RectTransform Background;

        [Tooltip("摇杆小球的RectTransform（即本脚本所在GameObject）")]
        public RectTransform Knob;

        [Tooltip("摇杆最大半径（像素）")]
        public float MaxRadius = 80f;

        [Tooltip("发送间隔（秒），防止消息过于频繁被服务端踢掉")]
        public float SendInterval = 0.1f;

        private bool m_IsDragging;
        private EntityRef<Entity> m_EntityRef;
        private float m_LastSendTime;
        private float m_LastTraceLogTime;

        /// <summary>
        /// 由HotfixView层调用，注入Entity引用（任意Entity即可，用于获取Root Scene）
        /// </summary>
        public void SetEntity(Entity entity)
        {
            m_EntityRef = entity;
            Debug.Log($"[JoystickTrace][ClientUI] SetEntity entityId={entity?.Id ?? 0}");
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            m_IsDragging = true;
            UpdateKnob(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!m_IsDragging) return;
            UpdateKnob(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            m_IsDragging = false;
            Knob.anchoredPosition = Vector2.zero;
            // 松手时立即发送停止，不受频率限制
            PublishInput(0f, 0f, true);
        }

        private void UpdateKnob(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                Background,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint);

            if (localPoint.magnitude > MaxRadius)
                localPoint = localPoint.normalized * MaxRadius;

            Knob.anchoredPosition = localPoint;

            Vector2 dir = localPoint / MaxRadius;
            PublishInput(dir.x, dir.y, false);
        }

        private void PublishInput(float dirX, float dirZ, bool force)
        {
            if (!force)
            {
                float now = Time.unscaledTime;
                if (now - m_LastSendTime < SendInterval) return;
                m_LastSendTime = now;
            }

            Entity entity = m_EntityRef;
            if (entity == null || entity.IsDisposed)
            {
                Debug.LogWarning($"[JoystickTrace][ClientUI] PublishInput skipped: entity invalid, force={force}, dir=({dirX:F3},{dirZ:F3})");
                return;
            }

            Scene root = entity.Root();
            float nowLog = Time.unscaledTime;
            if (force || nowLog - m_LastTraceLogTime >= 0.5f)
            {
                m_LastTraceLogTime = nowLog;
                Debug.Log($"[JoystickTrace][ClientUI] PublishInput scene={root?.Name}, force={force}, dir=({dirX:F3},{dirZ:F3})");
            }

            EventSystem.Instance?.Publish(root, new EventMain_JoystickInput
            {
                SceneInstanceId = root.InstanceId,
                DirX = dirX,
                DirZ = dirZ,
            });
        }
    }
}
