using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using Sirenix.OdinInspector;

namespace ET
{
    public struct BuffConfigLoader
    {
        public int Id;
    }

    public partial class BuffConfigCategory : Singleton<BuffConfigCategory>, ISingletonAwake
    {
        [BsonElement]
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        private Dictionary<int, BuffConfig> dict = new();

        public void Awake()
        {
        }

        public BuffConfig Get(int id)
        {
            this.dict.TryGetValue(id, out BuffConfig item);

            if (item != null)
            {
                return item;
            }

            item = EventSystem.Instance.Invoke<BuffConfigLoader, BuffConfig>(new BuffConfigLoader() { Id = id });
            if (item == null)
            {
                throw new Exception($"not found spell config: {id}");
            }

            this.dict.Add(id, item);
            return item;
        }

        public bool Contain(int id)
        {
            return this.dict.ContainsKey(id);
        }
    }

    [HideReferenceObjectPicker]
    [System.Serializable]
    public partial class BuffConfig : ProtoObject
    #if UNITY
            ,UnityEngine.ISerializationCallbackReceiver
    #endif
    {
        [LabelText("ID")]
        public int Id;

        [LabelText("描述")]
        public string Desc;

        [LabelText("持续时间")]
        public int Duration = 1000000000;

        [LabelText("Tick间隔时间")]
        public int TickTime;

        [LabelText("最大层数")]
        public int MaxStack = 1;

        [LabelText("层数")]
        public int Stack = 1;

        [LabelText("叠加规则类型")]
        public OverLayRuleType OverLayRuleType;

        [LabelText("移除条件")]
        public HashSet<BuffFlags> Flags = new();

        [LabelText("广播客户端类型")]
        public NoticeType NoticeType;

        #if UNITY
        [UnityEngine.SerializeReference]
        #endif
        [LabelText("  ")]
        //[HideReferenceObjectPicker]
        [BoxGroup("效果节点")]
        public List<EffectNode> Effects = new();

        #if UNITY
        [NonSerialized]
        [UnityEngine.HideInInspector]
        #endif
        public Dictionary<Type, EffectNode> effectDict;

        public void OnBeforeSerialize()
        {
            this.effectDict ??= new Dictionary<Type, EffectNode>();
            this.effectDict.Clear();
            foreach (EffectNode effectNode in this.Effects)
            {
                this.effectDict.Add(effectNode.GetType(), effectNode);
            }
        }

        public void OnAfterDeserialize()
        {
            this.effectDict ??= new Dictionary<Type, EffectNode>();
            this.effectDict.Clear();
            foreach (EffectNode effectNode in this.Effects)
            {
                this.effectDict.Add(effectNode.GetType(), effectNode);
            }
        }

        public T GetEffect<T>() where T : EffectNode
        {
            this.effectDict.TryGetValue(typeof(T), out EffectNode effectNode);
            return effectNode as T;
        }
    }

    public enum OverLayRuleType
    {
        [LabelText("无")]
        None,

        [LabelText("层数")]
        AddStack,

        [LabelText("时间")]
        AddTime,

        [LabelText("替换")]
        Replace,
    }
}