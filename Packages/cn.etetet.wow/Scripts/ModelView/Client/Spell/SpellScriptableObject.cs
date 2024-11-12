using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    [CreateAssetMenu(menuName = "ET/SpellScriptableObject")]
    [EnableClass]
    public class SpellScriptableObject : ScriptableObject
    {
        public SpellConfig SpellConfig;
    }
}


