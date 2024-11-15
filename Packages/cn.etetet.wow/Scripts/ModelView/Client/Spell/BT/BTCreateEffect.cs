using UnityEngine;

namespace ET
{
    public class BTCreateEffect: BTNode
    {
        [BTInput(typeof(Unit))]
        public string Unit;
        
        public BindPoint BindPoint;

        public GameObject Effect;
        
        public int Duration;
    }
}