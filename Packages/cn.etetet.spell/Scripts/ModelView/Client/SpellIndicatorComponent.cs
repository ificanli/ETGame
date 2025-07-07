using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    [ComponentOf(typeof(Unit))]
    public class SpellIndicatorComponent: Entity, IAwake, IUpdate
    {
        public Dictionary<int, GameObject> Cache = new();

        public Transform Current;

        public ETTask<Vector3> Task;

        public int MaxDistance;
    }
}