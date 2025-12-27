#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace ET.Client
{
    [CreateAssetMenu(menuName = "ET/SpellScriptableObject")]
    [EnableClass]
    [HideMonoScript]
    public class SpellScriptableObject : SerializedScriptableObject
    {
        [Title("技能 配置", TitleAlignment = TitleAlignments.Centered)]
        [NonSerialized, OdinSerialize]
        [HideLabel]
        [HideReferenceObjectPicker]
        public SpellConfig SpellConfig = new();
        
        private void OnValidate()
        {
#if UNITY_EDITOR
            int id = int.Parse(this.name);
            this.SpellConfig.Id = id;    // ScriptableObject 的 name 就是 asset 名称
            this.SpellConfig.BuffId = id + 100000;
#endif
        }
    }
}
#endif