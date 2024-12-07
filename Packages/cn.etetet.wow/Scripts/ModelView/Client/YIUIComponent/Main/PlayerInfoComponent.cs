using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2024.12.7
    /// Desc
    /// </summary>
    public partial class PlayerInfoComponent : Entity, IDynamicEvent<NumbericChange>
    {
        public EntityRef<Unit> m_Player;
        public Unit            Player => this.m_Player;

        public EntityRef<NumericComponent> m_Numeric;
        public NumericComponent            Numeric => this.m_Numeric;

        public UnitConfig m_UnitConfig;
    }
}