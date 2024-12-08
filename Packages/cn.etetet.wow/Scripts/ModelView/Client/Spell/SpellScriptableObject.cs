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
    public class SpellScriptableObject : SerializedScriptableObject
    {
        [Title("技能 配置", TitleAlignment = TitleAlignments.Centered)]
        [NonSerialized, OdinSerialize]
        [HideLabel]
        [HideReferenceObjectPicker]
        public SpellConfig SpellConfig = new();
    }
}