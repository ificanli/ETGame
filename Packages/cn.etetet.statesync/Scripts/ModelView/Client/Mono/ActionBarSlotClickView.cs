using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ET.Client
{
    [EnableClass]
    public class ActionBarSlotClickView : MonoBehaviour, IPointerClickHandler
    {
        private const float MinTriggerInterval = 0.2f;

        private EntityRef<ActionBarSlotComponent> m_EntityRef;
        private Action<ActionBarSlotComponent> m_OnClick;
        private float m_LastTriggerTime;

        public void SetEntity(ActionBarSlotComponent entity, Action<ActionBarSlotComponent> onClick)
        {
            m_EntityRef = entity;
            m_OnClick = onClick;
        }

        public void ClearEntity()
        {
            m_EntityRef = default;
            m_OnClick = null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (Time.unscaledTime - m_LastTriggerTime < MinTriggerInterval)
            {
                return;
            }

            m_LastTriggerTime = Time.unscaledTime;

            ActionBarSlotComponent entity = m_EntityRef;
            if (entity == null || entity.IsDisposed)
            {
                return;
            }

            m_OnClick?.Invoke(entity);
        }
    }
}
