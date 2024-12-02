using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
    public static class EffectUnitHelper
    {
        public static GameObject Create(Unit unit, BindPoint bindPoint, GameObject gameObject, bool worldPositionStays, int duration)
        {
            GameObjectComponent gameObjectComponent = unit.GetComponent<GameObjectComponent>();
            BindPointComponent bindPointComponent = gameObjectComponent.GameObject.GetComponent<BindPointComponent>();

            Transform bindPointTransform = bindPointComponent.BindPoints[bindPoint];

            GameObject effect;
            if (worldPositionStays)
            {
                effect = UnityEngine.Object.Instantiate(gameObject, bindPointTransform.position, gameObjectComponent.GameObject.transform.rotation);
            }
            else
            {
                effect = UnityEngine.Object.Instantiate(gameObject, bindPointTransform, false);
            }
            
            if (duration > 0)
            {
                UnityEngine.Object.Destroy(effect, duration / 1000f);
            }
            
            return effect;
        }
        
        public static GameObject Create(GameObject gameObject, Vector3 pos, Quaternion quaternion, int duration)
        {
            GameObject effect = UnityEngine.Object.Instantiate(gameObject, pos, quaternion);
            effect.transform.SetParent(GameObject.Find("/Global/Unit").transform);
            
            if (duration > 0)
            {
                UnityEngine.Object.Destroy(effect, duration / 1000f);
            }
            return effect;
        }
    }
}