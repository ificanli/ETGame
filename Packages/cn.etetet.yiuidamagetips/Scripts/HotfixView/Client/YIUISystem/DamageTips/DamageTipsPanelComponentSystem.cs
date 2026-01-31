using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;
using DamageNumbersPro;

namespace ET.Client
{
    [FriendOf(typeof(DamageTipsPanelComponent))]
    public static partial class DamageTipsPanelComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this DamageTipsPanelComponent self)
        {
            GameObject poolGameObject = new("Damage Number Pool");
            UnityEngine.Object.DontDestroyOnLoad(poolGameObject);
            poolGameObject.hideFlags = HideFlags.HideInHierarchy;
            self.m_PoolParent = poolGameObject.transform;
            self.m_PoolParent.localScale = Vector3.one;
            self.m_PoolParent.eulerAngles = self.m_PoolParent.position = Vector3.zero;
        }

        [EntitySystem]
        private static void Destroy(this DamageTipsPanelComponent self)
        {
            foreach (var data in self.m_Pool)
            {
                var pool = data.Value;
                if (pool != null)
                {
                    pool.Clear((obj) =>
                    {
                        if (obj != null)
                        {
                            UnityEngine.Object.Destroy(obj.gameObject);
                        }
                    });
                }
            }

            foreach (var data in self.m_Original)
            {
                var original = data.Value;
                if (original != null)
                {
                    UnityEngine.Object.Destroy(original.gameObject);
                }
            }

            if (self.m_PoolParent != null)
            {
                UnityEngine.Object.Destroy(self.m_PoolParent.gameObject);
            }

            self.m_Original.Clear();
            self.m_Pool.Clear();
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this DamageTipsPanelComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        public static async ETTask<DamageNumber> Get(this DamageTipsPanelComponent self, string presetName)
        {
            EntityRef<DamageTipsPanelComponent> selfRef = self;
            if (!self.m_Pool.ContainsKey(presetName))
            {
                using var _ = await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.YIUIPackage, presetName.GetHashCode());
                self = selfRef;
                if (!self.m_Pool.ContainsKey(presetName))
                {
                    var original = await BaseCreate();
                    if (original == null) return null;
                    var obj = original.gameObject;
                    obj.name = obj.name.Replace("(Clone)", "(Original)");
                    obj.SetActive(false);
                    self = selfRef;
                    self.m_Original.Add(presetName, original);
                    self.m_Pool.Add(presetName, new(self, Create));
                }

                void Put(DamageNumber damageNumber)
                {
                    damageNumber.gameObject.SetActive(false);
                    self.m_Pool[presetName].Put(damageNumber);
                }

                async ETTask<DamageNumber> BaseCreate()
                {
                    var baseObj = await YIUIFactory.InstantiateGameObjectAsync(self.Scene(), presetName);
                    if (baseObj == null) return null;
                    var damageNumber = baseObj.GetComponent<DamageNumber>();
                    if (damageNumber == null)
                    {
                        damageNumber = baseObj.AddComponent<DamageNumber>();
                        Log.Error($"{presetName} 目标必须挂载 DamageNumber脚本 请检查 为了不泄露这里会手动挂载脚本");
                    }

                    self = selfRef;
                    var rectTsf = damageNumber.GetComponent<RectTransform>();
                    if (rectTsf == null) //3D
                    {
                        //TODO 这里应该还有个2D判断 但是我们的3D游戏 所以不考虑
                        damageNumber.transform.SetParent(self.m_PoolParent, true);
                    }
                    else //UI
                    {
                        rectTsf.SetParent(self.u_ComUIPoolParent, false);
                    }

                    return damageNumber;
                }

                async ETTask<DamageNumber> Create()
                {
                    var damageNumber = await BaseCreate();
                    self = selfRef;
                    if (damageNumber == null) return null;
                    damageNumber.PoolPutAction = Put;
                    damageNumber.OriginalPrefab = self.m_Original[presetName];
                    return damageNumber;
                }
            }

            var damageNumber = await self.m_Pool[presetName].Get();
            #if UNITY_EDITOR

            //编辑器下 可以手动删除对象池就会返回空 方便调试这里一定会返回一个存在的对象
            if (damageNumber == null)
            {
                self = selfRef;
                damageNumber = await self.Get(presetName);
            }
            #endif
            if (damageNumber == null)
            {
                Log.Error($"{presetName} 错误 没有得到一个正确的对象");
                return null;
            }

            damageNumber.gameObject.SetActive(true);
            return damageNumber;
        }

        #region YIUIEvent开始

        #endregion YIUIEvent结束
    }
}