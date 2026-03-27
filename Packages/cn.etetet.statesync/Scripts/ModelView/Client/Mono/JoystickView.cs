using UnityEngine;
using UnityEngine.EventSystems;

namespace ET.Client
{
    /// <summary>
    /// 摇杆UI控件（纯MonoBehaviour）。
    /// 挂在摇杆小球GameObject上，Background/Knob通过Inspector赋值。
    /// 需要HotfixView层调用 SetEntity 注入Entity引用，才能发送ET事件。
    /// 支持从JoystickRangeView触发点击。
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
        public float SendInterval = 0.016f;

        private bool m_IsDragging;
        private EntityRef<Entity> m_EntityRef;
        private float m_LastSendTime;
        private Vector2 m_LastPublishedInput;
        private float m_LastTraceTime;
        private Vector2 m_LastTracedInput;

        /// <summary>
        /// 由HotfixView层调用，注入Entity引用（任意Entity即可，用于获取Root Scene）
        /// </summary>
        public void SetEntity(Entity entity)
        {
            m_EntityRef = entity;
            if (SendInterval > 0.016f)
            {
                SendInterval = 0.016f;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Log.Info($"[NavMove][UIPointer] source=joystick action=down pos=({eventData.position.x:F1},{eventData.position.y:F1})");
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
            Log.Info($"[NavMove][UIPointer] source=joystick action=up pos=({eventData.position.x:F1},{eventData.position.y:F1})");
            ForceRelease("pointer-up");
        }

        private void Update()
        {
            if (!m_IsDragging)
            {
                return;
            }

            bool pointerReleased = Input.touchSupported
                ? Input.touchCount == 0
                : !Input.GetMouseButton(0);
            if (!pointerReleased)
            {
                return;
            }

            ForceRelease("update-release-check");
        }

        private void OnDisable()
        {
            ForceRelease("disable");
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                ForceRelease("focus-lost");
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                ForceRelease("pause");
            }
        }

        public void ForceRelease(string reason = "unknown")
        {
            if (!m_IsDragging && m_LastPublishedInput.sqrMagnitude < 0.000001f)
            {
                return;
            }

            Vector2 lastPublishedInput = m_LastPublishedInput;
            m_IsDragging = false;
            if (Knob != null)
            {
                Knob.anchoredPosition = Vector2.zero;
            }

            m_LastPublishedInput = Vector2.zero;
            Log.Info($"[NavMove][JoystickRelease] reason={reason}, lastInput=({lastPublishedInput.x:F2}, {lastPublishedInput.y:F2})");
            // 兜底释放：UI失焦、禁用、丢失PointerUp时也必须归零。
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

            Vector2 previousInput = m_LastPublishedInput;
            m_LastPublishedInput = new Vector2(dirX, dirZ);
            this.TracePublishedInput(previousInput, m_LastPublishedInput, force);

            Entity entity = m_EntityRef;
            if (entity == null || entity.IsDisposed)
            {
                return;
            }

            Scene root = entity.Root();
            EventSystem.Instance?.Publish(root, new EventMain_JoystickInput
            {
                SceneInstanceId = root.InstanceId,
                DirX = dirX,
                DirZ = dirZ,
            });
        }

        private void TracePublishedInput(Vector2 previousInput, Vector2 currentInput, bool force)
        {
            float now = Time.unscaledTime;
            bool changed = (currentInput - m_LastTracedInput).sqrMagnitude >= 0.01f;
            if (!force && !changed && now - m_LastTraceTime < 0.08f)
            {
                return;
            }

            m_LastTraceTime = now;
            m_LastTracedInput = currentInput;
            Log.Info(
                $"[NavMove][UIPublish] force={force}, dragging={m_IsDragging}, prev=({previousInput.x:F2},{previousInput.y:F2}), current=({currentInput.x:F2},{currentInput.y:F2}), uiTime={now:F3}");
        }
    }
}
