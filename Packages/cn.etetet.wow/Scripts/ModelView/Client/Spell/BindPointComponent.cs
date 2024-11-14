using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
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
    }
    
    [EnableClass]
    public class BindPointComparer : IEqualityComparer<BindPoint>
    {
        public bool Equals(BindPoint x, BindPoint y) => x == y;

        public int GetHashCode(BindPoint obj) => ((int)obj).GetHashCode();
    }

    
    [EnableClass]
    [System.Serializable]
    public class BindPointComponent : MonoBehaviour
    {
        [ShowInInspector]
        public Dictionary<BindPoint, Transform> BindPoints = new(new BindPointComparer());
    }
}
