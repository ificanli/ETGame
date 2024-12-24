using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace ET.Client
{
    [CreateAssetMenu(menuName = "ET/AIScriptableObject")]
    [EnableClass]
    [HideMonoScript]
    public class AIScriptableObject : SerializedScriptableObject
    {
        [Title("AI 配置", TitleAlignment = TitleAlignments.Centered)]
        [NonSerialized, OdinSerialize]
        [HideLabel]
        [HideReferenceObjectPicker]
        public AIConfig AIConfig = new();
    }
}