#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace ET.Client
{
    [CreateAssetMenu(menuName = "ET/BuffScriptableObject")]
    [EnableClass]
    [HideMonoScript]
    [Module(ModuleName.Spell)]
    public class BuffScriptableObject : SerializedScriptableObject
    {
        [Title("Buff 配置", TitleAlignment = TitleAlignments.Centered)]
        [NonSerialized, OdinSerialize]
        [HideLabel]
        [HideReferenceObjectPicker]
        public BuffConfig BuffConfig = new();
    }
}

#endif