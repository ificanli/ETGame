using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2024.12.8
    /// Desc
    /// </summary>
    public partial class CastSliderComponent : Entity, ILateUpdate, IDynamicEvent<BTEvent_ShowCastSlider>
    {
        public EntityRef<Unit> m_Player;
        public Unit            Player => m_Player;

        public EntityRef<Buff> m_CurrentBuff;
        public Buff            CurrentBuff => m_CurrentBuff;

        public bool m_CurrentActiveState;

        public bool m_IsIncrease;
    }
}