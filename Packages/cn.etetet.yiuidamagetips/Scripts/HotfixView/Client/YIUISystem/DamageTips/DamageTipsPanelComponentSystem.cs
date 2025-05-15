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
            GameObject poolGameobject = new("Damage Number Pool");
            UnityEngine.Object.DontDestroyOnLoad(poolGameobject);
            self.m_PoolParent             = poolGameobject.transform;
            self.m_PoolParent.localScale  = Vector3.one;
            self.m_PoolParent.eulerAngles = self.m_PoolParent.position = Vector3.zero;
        }

        [EntitySystem]
        private static void Destroy(this DamageTipsPanelComponent self)
        {
            foreach (var pool in self.m_Pool.Values)
            {
                pool.Clear((obj) => UnityEngine.Object.Destroy(obj.gameObject));
            }

            foreach (var original in self.m_Original.Values)
            {
                UnityEngine.Object.Destroy(original.gameObject);
            }

            if (self.m_PoolParent != null)
                UnityEngine.Object.Destroy(self.m_PoolParent);

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
                using var coroutineLock = await YIUIMgrComponent.Inst?.Root().GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.YIUIFramework, presetName.GetHashCode());

                self = selfRef;
                if (!self.m_Original.ContainsKey(presetName))
                {
                    var original = await BaseCreate();
                    if (original == null) return null;
                    var obj = original.gameObject;
                    obj.name = obj.name.Replace("(Clone)", "(Original)");
                    obj.SetActive(false);
                    self = selfRef;
                    self.m_Original.Add(presetName, original);
                    self.m_Pool.Add(presetName, new ObjAsyncCache<DamageNumber>(Create));
                }

                void Put(DamageNumber damageNumber)
                {
                    damageNumber.gameObject.SetActive(false);
                    self.m_Pool[presetName].Put(damageNumber);
                }

                async ETTask<DamageNumber> BaseCreate()
                {
                    var baseObj = await YIUIFactory.InstantiateGameObjectAsync(presetName);
                    if (baseObj == null) return null;
                    var damageNumber = baseObj.GetComponent<DamageNumber>();
                    if (damageNumber == null)
                    {
                        damageNumber = baseObj.AddComponent<DamageNumber>();
                        Log.Error($"{presetName} 目标必须挂载 DamageNumber脚本 请检查 为了不泄露这里会手动挂载脚本");
                    }

                    var rectTsf = damageNumber.GetComponent<RectTransform>();
                    if (rectTsf == null) //3D
                    {
                        //TODO 这里应该还有个2D判断 但是我们的3D游戏 所以不考虑
                        self = selfRef;
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
                    if (damageNumber == null) return null;
                    damageNumber.PoolPutAction  = Put;
                    self = selfRef;
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