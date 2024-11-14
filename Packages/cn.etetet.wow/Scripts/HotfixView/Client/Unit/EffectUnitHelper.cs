using UnityEngine;

namespace ET.Client
{
    public static class EffectUnitHelper
    {
        public static GameObject Create(Unit unit, BindPoint bindPoint, GameObject gameObject, bool worldPositionStays)
        {
            GameObjectComponent gameObjectComponent = unit.GetComponent<GameObjectComponent>();
            BindPointComponent bindPointComponent = gameObjectComponent.GameObject.GetComponent<BindPointComponent>();
            return UnityEngine.Object.Instantiate(gameObject, bindPointComponent.BindPoints[bindPoint], worldPositionStays);
        }
    }
}