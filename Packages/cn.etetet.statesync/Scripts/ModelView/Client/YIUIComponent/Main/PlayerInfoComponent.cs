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
    public partial class PlayerInfoComponent : Entity
    {
        public EntityRef<Unit> m_Unit;
        public Unit            Unit => m_Unit;

        public UnitConfig m_UnitConfig;
    }
}