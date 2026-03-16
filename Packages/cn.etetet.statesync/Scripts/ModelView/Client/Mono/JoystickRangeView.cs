using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ET.Client
{
    [EnableClass]
    public class JoystickRangeView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private enum PointerHitType
        {
            EmptyRange,
            Joystick,
            OtherUI,
        }

        public RectTransform JoystickBackground;
        public JoystickView JoystickView;
        public RectTransform RangeRect;

        private bool m_IsActive;

        public void ResetVisualState()
        {
            m_IsActive = false;

            if (JoystickView != null && JoystickView.Knob != null)
            {
                JoystickView.Knob.anchoredPosition = Vector2.zero;
            }

            if (JoystickBackground != null)
            {
                JoystickBackground.anchoredPosition = Vector2.zero;
                if (JoystickBackground.gameObject.activeSelf)
                {
                    JoystickBackground.gameObject.SetActive(false);
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (JoystickBackground == null || JoystickView == null || RangeRect == null)
            {
                return;
            }

            PointerHitType hitType = GetPointerHitType(eventData);
            if (hitType == PointerHitType.OtherUI)
            {
                return;
            }

            m_IsActive = true;
            SetJoystickVisible(true);

            if (hitType == PointerHitType.Joystick)
            {
                JoystickView.OnPointerDown(eventData);
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    RangeRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
            {
                ResetVisualState();
                return;
            }

            JoystickBackground.anchoredPosition = localPoint;
            JoystickView.OnPointerDown(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!m_IsActive)
            {
                return;
            }

            JoystickView.OnDrag(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!m_IsActive)
            {
                return;
            }

            JoystickView.OnPointerUp(eventData);
            ResetVisualState();
        }

        private PointerHitType GetPointerHitType(PointerEventData eventData)
        {
            UnityEngine.EventSystems.EventSystem currentEventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (currentEventSystem == null)
            {
                return PointerHitType.EmptyRange;
            }

            List<RaycastResult> results = new List<RaycastResult>();
            currentEventSystem.RaycastAll(eventData, results);

            foreach (RaycastResult result in results)
            {
                if (result.gameObject == gameObject)
                {
                    continue;
                }

                if (result.gameObject == JoystickBackground.gameObject ||
                    result.gameObject == JoystickView.gameObject ||
                    result.gameObject.transform.IsChildOf(JoystickBackground.transform))
                {
                    return PointerHitType.Joystick;
                }

                if (result.gameObject.GetComponent<Selectable>() != null)
                {
                    return PointerHitType.OtherUI;
                }
            }

            return PointerHitType.EmptyRange;
        }

        private void SetJoystickVisible(bool visible)
        {
            if (JoystickBackground == null)
            {
                return;
            }

            if (JoystickBackground.gameObject.activeSelf != visible)
            {
                JoystickBackground.gameObject.SetActive(visible);
            }
        }
    }
}
