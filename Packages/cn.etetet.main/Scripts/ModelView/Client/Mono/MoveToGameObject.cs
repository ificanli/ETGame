using System;
using UnityEngine;

namespace ET.Client
{
    [EnableClass]
    public class MoveToGameObject: MonoBehaviour
    {
        public Transform Target;
        public float Speed;
        
        public void Update()
        {
            this.transform.position = Vector3.MoveTowards(transform.position, Target.position, Speed * Time.deltaTime);
            this.transform.forward = this.Target.position - this.transform.position;
        }
    }
}