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

        public static async ETTask<DamageNumber> Show3D(string prefabName,
                                                        Unit   unit,
                                                        string newText,
                                                        bool   follow = true)
        {
            var unitObj = GetUnitGamobject(unit);
            if (unitObj == null) return null;
            var tsf = unitObj.transform;

            var damageNumber = await Show3D(prefabName, tsf.position, 0, newText, follow ? tsf : null);
            if (damageNumber == null) return null;
            damageNumber.enableNumber = false;
            return damageNumber;
        }

        public static async ETTask<DamageNumber> Show3D(string prefabName,
                                                        Unit   unit,
                                                        float  newNumber,
                                                        bool   follow = true)
        {
            var unitObj = GetUnitGamobject(unit);
            if (unitObj == null) return null;
            var tsf = unitObj.transform;

            return await Show3D(prefabName, tsf.position, newNumber, null, follow ? tsf : null);
        }

        private static GameObject GetUnitGamobject(Unit unit)
        {
            var unitObj = unit?.GetComponent<GameObjectComponent>();
            if (unitObj == null) return null;
            if (unitObj.GameObject == null)
            {
                Log.Error($"{unit.GetType().Name} 目标没有 GameObject 实体");
                return null;
            }

            return unitObj.GameObject;
        }

        public static async ETTask<DamageNumber> Show3D(string    prefabName,
                                                        Vector3   newPosition,
                                                        float     newNumber,
                                                        string    newText           = null,
                                                        Transform followedTransform = null)
        {
            var damageNumber = await Get(prefabName);
            if (damageNumber == null) return null;

            damageNumber.Spawn(newPosition);

            damageNumber.enableNumber = true;
            damageNumber.number       = newNumber;

            if (!string.IsNullOrEmpty(newText))
            {
                damageNumber.enableLeftText = true;
                damageNumber.leftText       = newText;
            }

            if (followedTransform != null)
            {
                damageNumber.SetFollowedTarget(followedTransform);
            }

            return damageNumber;
        }

        #endregion

        #region UI

        public static async ETTask<DamageNumber> ShowUI(string prefabName, RectTransform rect, float newNumber)
        {
            return await ShowUI(prefabName, rect.position, newNumber);
        }

        public static async ETTask<DamageNumber> ShowUI(string prefabName, Vector3 worldPoint, float newNumber)
        {
            var screenPoint = RectTransformUtility.WorldToScreenPoint(YIUIMgrComponent.Inst.UICamera, worldPoint);
            return await ShowUI(prefabName, screenPoint, newNumber);
        }

        public static async ETTask<DamageNumber> ShowUI(string prefabName, Vector2 screenPoint, float newNumber)
        {
            var damageNumber = await Get(prefabName);
            if (damageNumber == null) return null;

            var rectTsf = damageNumber.GetComponent<RectTransform>();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTsf.parent.GetComponent<RectTransform>(),
                screenPoint,
                YIUIMgrComponent.Inst.UICamera, out var localPos);

            rectTsf.localPosition     = localPos;
            damageNumber.enableNumber = true;
            damageNumber.number       = newNumber;
            return damageNumber;
        }

        #endregion

        public static async ETTask<DamageNumber> Get(string prefabName)
        {
            var panel = await GetDamageTipsPanel();
            if (panel == null) return null;
            return await panel.Get(prefabName);
        }

        private static async ETTask<DamageTipsPanelComponent> GetDamageTipsPanel()
        {
            if (YIUIMgrComponent.Inst == null)
            {
                return null;
            }

            var panel = YIUIMgrComponent.Inst.GetPanel<DamageTipsPanelComponent>();
            if (panel == null)
            {
                panel = await YIUIMgrComponent.Inst.Root.OpenPanelAsync<DamageTipsPanelComponent>();
            }

            return panel;
        }
    }
}