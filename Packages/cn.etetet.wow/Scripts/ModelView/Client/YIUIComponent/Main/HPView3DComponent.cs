using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2024.12.10
    /// Desc
    /// </summary>
    public partial class HPView3DComponent : Entity, ILateUpdate
    {
        public EntityRef<Unit> m_OwnerUnit;
        public Unit            OwnerUnit => m_OwnerUnit;

        public EntityRef<Unit> m_Player;
        public Unit            Player => m_Player;
    }
}