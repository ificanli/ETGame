using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    [ComponentOf(typeof(Buff))]
    public class BuffGameObjectComponent: Entity, IAwake, IDestroy
    {
        public List<GameObject> GameObjects { get; set; } = new();
    }
}