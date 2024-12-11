using UnityEngine;

namespace ET.Client
{
    [EntitySystemOf(typeof(BuffGameObjectComponent))]
    public static partial class BuffGameObjectComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BuffGameObjectComponent self)
        {

        }
        [EntitySystem]
        private static void Destroy(this BuffGameObjectComponent self)
        {
            foreach (GameObject gameObject in self.GameObjects)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
        }
    }
}