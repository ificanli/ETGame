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
        [NonSerialized, OdinSerialize]
        public SpellConfig SpellConfig;
    }
}


