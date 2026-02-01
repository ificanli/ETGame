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
    public partial class UnitInfoComponent : Entity, IDynamicEvent<NumbericChange>
    {
        public EntityRef<Unit> m_Unit;
        public Unit            Unit => m_Unit;

        public EntityRef<NumericComponent> m_Numeric;
        public NumericComponent            Numeric => m_Numeric;

        public UnitConfig m_UnitConfig;
    }
}