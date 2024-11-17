using UnityEngine;

namespace ET.Client
{
    public static class EffectUnitHelper
    {
        public static GameObject Create(Unit unit, BindPoint bindPoint, GameObject gameObject, bool worldPositionStays)
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
            return effect;
        }
    }
}