using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace ET
{
    [EnableClass]
    [Module(ModuleName.Spell)]
    public class BindPointComparer : IEqualityComparer<BindPoint>
    {
        public bool Equals(BindPoint x, BindPoint y) => x == y;

        public int GetHashCode(BindPoint obj) => ((int)obj).GetHashCode();
    }

    
    [EnableClass]
    [Serializable]
    [Module(ModuleName.Spell)]
    public class BindPointComponent : SerializedMonoBehaviour
    {
        [ShowInInspector]
        [NonSerialized, OdinSerialize]
        public Dictionary<BindPoint, Transform> BindPoints = new(new BindPointComparer());

        public Dictionary<BindPoint, Transform> GetBindPoints()
        {
            return BindPoints;
        }
    }
}
