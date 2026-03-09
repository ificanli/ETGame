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

        public void OnPointerDown(PointerEventData eventData)
        {
            Debug.Log("[JoystickRange] OnPointerDown triggered");

            if (JoystickBackground == null || JoystickView == null || RangeRect == null)
            {
                Debug.LogWarning($"[JoystickRange] missing refs. Background={JoystickBackground != null}, View={JoystickView != null}, Range={RangeRect != null}");
                return;
            }

            PointerHitType hitType = GetPointerHitType(eventData);
            if (hitType == PointerHitType.OtherUI)
            {
                Debug.Log("[JoystickRange] pointer is over other interactable UI, skip joystick handling");
                return;
            }

            m_IsActive = true;

            if (hitType == PointerHitType.Joystick)
            {
                Debug.Log("[JoystickRange] pointer is over joystick, forward pointer down to JoystickView");
                JoystickView.OnPointerDown(eventData);
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    RangeRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
            {
                m_IsActive = false;
                Debug.LogWarning("[JoystickRange] convert pointer to local point failed");
                return;
            }

            Debug.Log($"[JoystickRange] move joystick background to {localPoint}");
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

            m_IsActive = false;
            JoystickView.OnPointerUp(eventData);

            if (JoystickBackground != null)
            {
                JoystickBackground.anchoredPosition = Vector2.zero;
            }
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
                    Debug.Log($"[JoystickRange] detect joystick hit: {result.gameObject.name}");
                    return PointerHitType.Joystick;
                }

                if (result.gameObject.GetComponent<Selectable>() != null)
                {
                    Debug.Log($"[JoystickRange] detect other ui hit: {result.gameObject.name}");
                    return PointerHitType.OtherUI;
                }
            }

            return PointerHitType.EmptyRange;
        }
    }
}
