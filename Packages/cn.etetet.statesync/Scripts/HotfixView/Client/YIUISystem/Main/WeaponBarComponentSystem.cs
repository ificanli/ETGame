using System;
using System.Collections;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;
using System.Reflection;

namespace ET.Client
{
    /// <summary>
    /// 武器栏组件系统
    /// </summary>
    [FriendOf(typeof(WeaponBarComponent))]
    public static partial class WeaponBarComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this WeaponBarComponent self)
        {
            self.EnsureWeaponItemOwnersCreated();
            self.BindWeaponItems();
            self.RefreshCurrentPlayerWeaponBar();
        }

        [EntitySystem]
        private static void Destroy(this WeaponBarComponent self)
        {
            self.Slot1Item = null;
            self.Slot2Item = null;
        }

        /// <summary>
        /// 刷新武器栏显示
        /// </summary>
        public static void RefreshWeaponBar(this WeaponBarComponent self, WeaponComponent weaponComp)
        {
            if (weaponComp == null)
            {
                Log.Warning("[WeaponBar] WeaponComponent is null");
                return;
            }

            self.BindWeaponItems();

            WeaponItemComponent slot1Item = self.Slot1Item;
            WeaponItemComponent slot2Item = self.Slot2Item;
            if (slot1Item == null && slot2Item == null)
            {
                Log.Warning("[WeaponBar] No weapon items bound");
                return;
            }

            slot1Item?.RefreshSlot(weaponComp);
            slot2Item?.RefreshSlot(weaponComp);
        }

        /// <summary>
        /// 使用当前玩家武器数据刷新武器栏
        /// </summary>
        public static void RefreshCurrentPlayerWeaponBar(this WeaponBarComponent self)
        {
            WeaponComponent weaponComponent = self.GetMyWeaponComponent();
            if (weaponComponent == null)
            {
                return;
            }

            self.RefreshWeaponBar(weaponComponent);
        }

        /// <summary>
        /// 绑定武器槽位组件，保持主界面与子组件的稳定引用
        /// </summary>
        private static void BindWeaponItems(this WeaponBarComponent self)
        {
            self.EnsureWeaponItemOwnersCreated();

            WeaponItemComponent slot1Item = self.Slot1Item;
            WeaponItemComponent slot2Item = self.Slot2Item;
            if (slot1Item != null && !slot1Item.IsDisposed &&
                slot2Item != null && !slot2Item.IsDisposed)
            {
                return;
            }

            self.Slot1Item = null;
            self.Slot2Item = null;

            List<WeaponItemComponent> itemComponents = self.CollectBoundWeaponItems();
            if (itemComponents.Count == 0)
            {
                // 兼容旧数据：如果子实体列表还没建好，回退到 owner 查找。
                itemComponents = self.CollectBoundWeaponItemsByOwnerLookup();
            }

            for (int i = 0; i < itemComponents.Count && i < 2; ++i)
            {
                WeaponItemComponent item = itemComponents[i];
                if (item == null || item.IsDisposed)
                {
                    continue;
                }

                item.SlotIndex = i + 1;
                if (i == 0)
                {
                    self.Slot1Item = item;
                }
                else
                {
                    self.Slot2Item = item;
                }
            }

            slot1Item = self.Slot1Item;
            slot2Item = self.Slot2Item;
        }

        private static List<WeaponItemComponent> CollectBoundWeaponItems(this WeaponBarComponent self)
        {
            List<WeaponItemComponent> itemComponents = new();
            Transform rootTransform = self.UIBase?.OwnerGameObject?.transform;
            if (rootTransform == null || self.ChildrenCount() <= 0)
            {
                return itemComponents;
            }

            foreach (Entity entity in self.Children.Values)
            {
                if (entity is not WeaponItemComponent item || item.IsDisposed)
                {
                    continue;
                }

                Transform itemTransform = item.UIBase?.OwnerGameObject?.transform;
                if (itemTransform == null || itemTransform.parent != rootTransform)
                {
                    continue;
                }

                if (!itemTransform.name.StartsWith(WeaponItemComponent.ResName, StringComparison.Ordinal))
                {
                    continue;
                }

                itemComponents.Add(item);
            }

            itemComponents.Sort((left, right) =>
            {
                int leftIndex = left?.UIBase?.OwnerGameObject?.transform?.GetSiblingIndex() ?? int.MaxValue;
                int rightIndex = right?.UIBase?.OwnerGameObject?.transform?.GetSiblingIndex() ?? int.MaxValue;
                return leftIndex.CompareTo(rightIndex);
            });

            return itemComponents;
        }

        private static List<WeaponItemComponent> CollectBoundWeaponItemsByOwnerLookup(this WeaponBarComponent self)
        {
            List<WeaponItemComponent> itemComponents = new();
            Transform rootTransform = self.UIBase?.OwnerGameObject?.transform;
            if (rootTransform == null)
            {
                return itemComponents;
            }

            List<Transform> itemTransforms = new();
            foreach (Transform child in rootTransform)
            {
                if (!child.name.StartsWith(WeaponItemComponent.ResName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (child.GetComponent<UIBindCDETable>() == null)
                {
                    continue;
                }

                itemTransforms.Add(child);
            }

            itemTransforms.Sort((left, right) => left.GetSiblingIndex().CompareTo(right.GetSiblingIndex()));

            foreach (Transform itemTransform in itemTransforms)
            {
                WeaponItemComponent item = self.UIBase.CDETable.FindUIOwner<WeaponItemComponent>(itemTransform.name);
                if (item == null || item.IsDisposed)
                {
                    continue;
                }

                itemComponents.Add(item);
            }

            return itemComponents;
        }

        /// <summary>
        /// 获取当前玩家的武器组件
        /// </summary>
        private static WeaponComponent GetMyWeaponComponent(this WeaponBarComponent self)
        {
            Scene root = self.Root();
            Scene currentScene = root?.CurrentScene();
            Unit myUnit = UnitHelper.GetMyUnitFromCurrentScene(currentScene);
            if (myUnit == null || myUnit.IsDisposed)
            {
                return null;
            }

            return myUnit.GetComponent<WeaponComponent>();
        }

        /// <summary>
        /// WeaponBar 预制有时缺少 AllChildCdeTable 记录，这里运行时补全一次，确保 WeaponItem 组件能被创建并注册。
        /// </summary>
        private static void EnsureWeaponItemOwnersCreated(this WeaponBarComponent self)
        {
            UIBindCDETable cdeTable = self.UIBase?.CDETable;
            Transform rootTransform = self.UIBase?.OwnerGameObject?.transform;
            if (cdeTable == null || rootTransform == null)
            {
                return;
            }

            List<UIBindCDETable> childCdeTables = CollectWeaponItemChildCdeTables(rootTransform);

            if (childCdeTables.Count == 0)
            {
                return;
            }

            FieldInfo allOwnerField = typeof(UIBindCDETable).GetField("m_AllChildUIOwner", BindingFlags.Instance | BindingFlags.NonPublic);
            IDictionary ownerDict = allOwnerField?.GetValue(cdeTable) as IDictionary;
            if (ownerDict == null)
            {
                return;
            }

            HashSet<string> validOwnerNames = new(StringComparer.Ordinal);
            List<string> staleOwnerKeys = new();
            foreach (DictionaryEntry pair in ownerDict)
            {
                if (pair.Key is not string ownerName || string.IsNullOrWhiteSpace(ownerName))
                {
                    continue;
                }

                if (!TryGetOwnerEntity(pair.Value, out Entity owner) || owner == null || owner.IsDisposed)
                {
                    staleOwnerKeys.Add(ownerName);
                    continue;
                }

                validOwnerNames.Add(ownerName);
            }

            if (staleOwnerKeys.Count > 0)
            {
                foreach (string staleKey in staleOwnerKeys)
                {
                    ownerDict.Remove(staleKey);
                }
            }

            List<UIBindCDETable> missingChildCdeTables = new();
            foreach (UIBindCDETable childCde in childCdeTables)
            {
                if (childCde == null)
                {
                    continue;
                }

                if (validOwnerNames.Contains(childCde.gameObject.name))
                {
                    continue;
                }

                missingChildCdeTables.Add(childCde);
            }

            if (missingChildCdeTables.Count == 0)
            {
                return;
            }

            try
            {
                FieldInfo allChildField = typeof(UIBindCDETable).GetField("AllChildCdeTable", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo createComponentMethod = typeof(YIUIFactory).GetMethod(
                    "CreateComponent",
                    BindingFlags.Static | BindingFlags.NonPublic,
                    null,
                    new[] {typeof(UIBindCDETable), typeof(Entity)},
                    null);

                if (allChildField == null || createComponentMethod == null)
                {
                    return;
                }

                List<UIBindCDETable> originalChildCdeTables = allChildField.GetValue(cdeTable) as List<UIBindCDETable>;
                try
                {
                    allChildField.SetValue(cdeTable, missingChildCdeTables);
                    createComponentMethod.Invoke(null, new object[] {cdeTable, self});
                }
                finally
                {
                    allChildField.SetValue(cdeTable, originalChildCdeTables ?? childCdeTables);
                }
            }
            catch (Exception e)
            {
                Log.Error($"[WeaponBar] Runtime补全WeaponItem绑定失败: {e}");
            }
        }

        private static bool TryGetOwnerEntity(object rawOwner, out Entity owner)
        {
            owner = null;
            if (rawOwner is not EntityRef<Entity> ownerRef)
            {
                return false;
            }

            owner = ownerRef;
            return true;
        }

        private static List<UIBindCDETable> CollectWeaponItemChildCdeTables(Transform rootTransform)
        {
            List<UIBindCDETable> childCdeTables = new();
            foreach (Transform child in rootTransform)
            {
                if (!child.name.StartsWith(WeaponItemComponent.ResName, StringComparison.Ordinal))
                {
                    continue;
                }

                UIBindCDETable childCde = child.GetComponent<UIBindCDETable>();
                if (childCde != null)
                {
                    childCdeTables.Add(childCde);
                }
            }

            childCdeTables.Sort((left, right) =>
            {
                int leftIndex = left?.transform?.GetSiblingIndex() ?? int.MaxValue;
                int rightIndex = right?.transform?.GetSiblingIndex() ?? int.MaxValue;
                return leftIndex.CompareTo(rightIndex);
            });

            return childCdeTables;
        }
    }
}
