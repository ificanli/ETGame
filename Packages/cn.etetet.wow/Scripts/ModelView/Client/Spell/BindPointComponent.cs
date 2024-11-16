using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace ET
{
    [EnableClass]
    public enum BindPoint
    {
        None,
        Attack,
        Base,
        Head,
        Bullet,
        LeftHand,
        RightHand,
        Buff,
        HP,
        Hitted,
    }
    
    [EnableClass]
    public class BindPointComparer : IEqualityComparer<BindPoint>
    {
        public bool Equals(BindPoint x, BindPoint y) => x == y;

        public int GetHashCode(BindPoint obj) => ((int)obj).GetHashCode();
    }

    
    [EnableClass]
    [System.Serializable]
    public class BindPointComponent : SerializedMonoBehaviour
    {
        [ShowInInspector]
        [OdinSerialize]
        public Dictionary<BindPoint, Transform> BindPoints = new(new BindPointComparer());
    }
}
