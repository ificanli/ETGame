using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2024.12.14
    /// Desc
    /// </summary>
    public partial class ActionBarSlotComponent : Entity, ILateUpdate
    {
        public SpellConfig     m_SpellConfig;
        public EntityRef<Unit> m_Player;
        public Unit            Player => m_Player;

        public EntityRef<NumericComponent> m_Numeric;
        public NumericComponent            Numeric => m_Numeric;

        public EntityRef<SpellComponent> m_SpellComponent;
        public SpellComponent            SpellComponent => m_SpellComponent;

        public int GGCD = 2000; //公共CD SpellComponent 暂时写的2秒 所以这里写死 有需求再改
    }
}