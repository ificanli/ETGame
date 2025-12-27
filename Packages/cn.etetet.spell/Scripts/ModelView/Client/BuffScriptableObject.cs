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
    public class BuffScriptableObject : SerializedScriptableObject
    {
        [Title("Buff 配置", TitleAlignment = TitleAlignments.Centered)]
        [NonSerialized, OdinSerialize]
        [HideLabel]
        [HideReferenceObjectPicker]
        public BuffConfig BuffConfig = new();
        
        private void OnValidate()
        {
#if UNITY_EDITOR
            int id = int.Parse(this.name);
            this.BuffConfig.Id = id;    // ScriptableObject 的 name 就是 asset 名称
#endif
        }
    }
}

#endif