using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2024.12.9
    /// Desc
    /// </summary>
    public partial class HPViewComponent : Entity, ILateUpdate
    {
        public EntityRef<Unit> m_OwnerUnit;
        public Unit            OwnerUnit => m_OwnerUnit;

        public Transform HPPoint;

        public EntityRef<Unit> m_Player;
        public Unit            Player => m_Player;

        public EntityRef<NumericComponent> m_Numeric;
        public NumericComponent            Numeric => m_Numeric;
    }
}