using DamageNumbersPro;
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 伤害飘字助手
    /// </summary>
    public static class DamageTipsHelper
    {
        #region 3D

        public static async ETTask<DamageNumber> Show3D(Scene scene, string prefabName, Unit unit, string newText, bool follow = true)
        {
            var unitObj = GetUnitGameObject(unit);
            if (unitObj == null) return null;
            var tsf = unitObj.transform;

            var damageNumber = await Show3D(scene, prefabName, tsf.position, 0, newText, follow ? tsf : null);
            if (damageNumber == null) return null;
            damageNumber.enableNumber = false;
            return damageNumber;
        }

        public static async ETTask<DamageNumber> Show3D(Scene scene, string prefabName, Unit unit, float newNumber, bool follow = true)
        {
            var unitObj = GetUnitGameObject(unit);
            if (unitObj == null) return null;
            var tsf = unitObj.transform;

            return await Show3D(scene, prefabName, tsf.position, newNumber, null, follow ? tsf : null);
        }

        private static GameObject GetUnitGameObject(Unit unit)
        {
            if (unit == null)
            {
                Log.Error("目标为空");
                return null;
            }

            var unitObj = unit?.GetComponent<GameObjectComponent>();
            if (unitObj == null)
            {
                Log.Error($"{unit.GetType().Name} 目标没有 GameObjectComponent 组件");
                return null;
            }

            if (unitObj.GameObject == null)
            {
                Log.Error($"{unit.GetType().Name} 目标没有 GameObject 实体");
                return null;
            }

            return unitObj.GameObject;
        }

        public static async ETTask<DamageNumber> Show3D(Scene scene, string prefabName, Vector3 newPosition, float newNumber, string newText = null, Transform followedTransform = null)
        {
            var damageNumber = await Get(scene, prefabName);
            if (damageNumber == null) return null;

            damageNumber.Spawn(newPosition);

            damageNumber.enableNumber = true;
            damageNumber.number = newNumber;

            if (!string.IsNullOrEmpty(newText))
            {
                damageNumber.enableLeftText = true;
                damageNumber.leftText = newText;
            }

            if (followedTransform != null)
            {
                damageNumber.SetFollowedTarget(followedTransform);
            }

            return damageNumber;
        }

        #endregion

        #region UI

        public static async ETTask<DamageNumber> ShowUIByRectTransform(Scene scene, string prefabName, RectTransform rect, float newNumber)
        {
            return await ShowUIByScreenPoint(scene, prefabName, rect.position, newNumber);
        }

        public static async ETTask<DamageNumber> ShowUIByWorldPoint(Scene scene, string prefabName, Vector3 worldPoint, float newNumber)
        {
            var screenPoint = Camera.main.WorldToScreenPoint(worldPoint);
            return await ShowUIByScreenPoint(scene, prefabName, screenPoint, newNumber);
        }

        public static async ETTask<DamageNumber> ShowUIByScreenPoint(Scene scene, string prefabName, Vector2 screenPoint, float newNumber)
        {
            EntityRef<Scene> sceneRef = scene;
            var damageNumber = await Get(scene, prefabName);
            if (damageNumber == null) return null;

            var rectTsf = damageNumber.GetComponent<RectTransform>();
            scene = sceneRef;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTsf.parent.GetComponent<RectTransform>(),
                screenPoint,
                scene.YIUIMgr().UICamera, out var localPos);

            rectTsf.localPosition = localPos;
            damageNumber.enableNumber = true;
            damageNumber.number = newNumber;
            return damageNumber;
        }

        #endregion

        public static async ETTask<DamageNumber> Get(Scene scene, string prefabName)
        {
            var panel = await GetDamageTipsPanel(scene);
            if (panel == null) return null;
            return await panel.Get(prefabName);
        }

        private static async ETTask<DamageTipsPanelComponent> GetDamageTipsPanel(Scene scene)
        {
            if (scene.IsDisposed)
            {
                return null;
            }

            var panel = scene.YIUIMgr().GetPanel<DamageTipsPanelComponent>();
            if (panel == null)
            {
                panel = await scene.YIUIRoot().OpenPanelAsync<DamageTipsPanelComponent>();
            }

            return panel;
        }
    }
}