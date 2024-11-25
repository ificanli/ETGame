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
    public class BuffScriptableObject : SerializedScriptableObject
    {
        [NonSerialized, OdinSerialize]
        public BuffConfig BuffConfig;
    }
}


