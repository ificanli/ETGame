using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;
using DamageNumbersPro;

namespace ET.Client
{
    public partial class DamageTipsPanelComponent : Entity
    {
        public Dictionary<string, DamageNumber>                m_Original = new();
        public Dictionary<string, ObjAsyncCache<DamageNumber>> m_Pool     = new();
        public Transform                                       m_PoolParent;
    }
}