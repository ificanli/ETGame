#define DNP_NEWPOOL
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using TMPro;
using DamageNumbersPro.Internal;
using Sirenix.OdinInspector;

#if ENABLE_INPUT_SYSTEM && DNP_NewInputSystem
using UnityEngine.InputSystem;
#endif

namespace DamageNumbersPro
{
    public abstract partial class DamageNumber
    {
        public DamageNumber OriginalPrefab
        {
            get => this.originalPrefab;
            set => this.originalPrefab = value;
        }

        public Action<DamageNumber> PoolPutAction;
        
        #if DNP_NEWPOOL
        
        private static bool NewPool = true;

        public DamageNumber Spawn()
        {
            if (!NewPool)
            {
                DamageNumber newDN      = default;
                int          instanceID = GetInstanceID();

                //Check Pool:
                if (enablePooling && PoolAvailable(instanceID))
                {
                    //Get from Pool:
                    foreach (DamageNumber dn in pools[instanceID])
                    {
                        newDN = dn; //This is the only way I can get a unknown element from a hashset, using a single loop iteration.
                        break;
                    }

                    pools[instanceID].Remove(newDN);
                }
                else
                {
                    #if UNITY_EDITOR
                    if (enablePooling)
                    {
                        Debug.LogError($"{this.gameObject.name} 开启了对象池 但是并未初始化 需要手动调用一次 PrewarmPool 方法 ");
                    }
                    #endif

                    //Create New:
                    GameObject newGO = Instantiate<GameObject>(gameObject);
                    newDN = newGO.GetComponent<DamageNumber>();

                    if (enablePooling)
                    {
                        newDN.originalPrefab = this;
                    }
                }

                newDN.gameObject.SetActive(true); //Active Gameobject
                newDN.OnPreSpawn();

                if (enablePooling)
                {
                    newDN.SetPoolingID(instanceID);
                    newDN.destroyAfterSpawning = false;
                }

                return newDN;
            }
            else
            {
                return Spawn2();
            }
        }

        /// <summary>
        /// 禁用这个脚本本身的这个Spawn方法 我自己管理
        /// 我自己用其他方式来管理
        /// </summary>
        private DamageNumber Spawn2()
        {
            #if UNITY_EDITOR
            if (enablePooling)
            {
                Debug.LogError($"不需要开对象池功能 内部默认开启对象池 使用其他技术");
            }
            #endif
            this.OnPreSpawn();
            return this;
        }

        public void DestroyDNP()
        {
            if (!NewPool)
            {
                //Updater:
                DNPUpdater.UnregisterPopup(unscaledTime, updateDelay, this);

                //Pooling / Destroying:
                if (enablePooling && originalPrefab != null)
                {
                    if (pools == null)
                    {
                        pools = new Dictionary<int, HashSet<DamageNumber>>();
                    }

                    if (!pools.ContainsKey(poolingID))
                    {
                        pools.Add(poolingID, new HashSet<DamageNumber>());
                    }

                    RemoveFromDictionary();

                    if (pools[poolingID].Count < poolSize)
                    {
                        PreparePooling();
                    }
                    else
                    {
                        Destroy(gameObject); //Not enough pool space.
                    }
                }
                else
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                DestroyDNP2();
            }
        }

        private void DestroyDNP2()
        {
            DNPUpdater.UnregisterPopup(unscaledTime, updateDelay, this);
            if (PoolPutAction != null)
            {
                RemoveFromDictionary();
                PreparePooling2();
                PoolPutAction.Invoke(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void PreparePooling2()
        {
            //Queue Restart:
            performRestart = true;

            //Reset Runtime Variables:
            transform.localScale = originalScale;
            lastTargetPosition   = targetOffset = Vector3.zero;

            //Clear Combination Targets:
            myAbsorber = null;

            //Reset some Setting Variables:
            spamGroup       = originalPrefab.spamGroup;
            leftText        = originalPrefab.leftText;
            rightText       = originalPrefab.rightText;
            followedTarget  = originalPrefab.followedTarget;
            enableCollision = originalPrefab.enableCollision;
            enablePush      = originalPrefab.enablePush;
        }

        #endif
        
        #region Pooling
        
        private bool isUnregisterPopup;
        public  bool IsUnregisterPopup => isUnregisterPopup;

        //不摧毁但是断开Updat UpdateDamageNumber
        public void UnregisterPopup()
        {
            isUnregisterPopup = true;
            DNPUpdater.UnregisterPopup(unscaledTime, updateDelay, this);
        }

        //重新连接Update
        public void PerformRestart()
        {
            if (!isUnregisterPopup) return;
            performRestart    = true;
            isUnregisterPopup = false;
        }

        #endregion
    }
}