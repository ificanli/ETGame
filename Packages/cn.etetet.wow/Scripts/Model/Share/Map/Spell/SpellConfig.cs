using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using Sirenix.OdinInspector;

namespace ET
{
    public struct SpellConfigLoader
    {
        public int Id;
    }

    public partial class SpellConfigCategory : Singleton<SpellConfigCategory>, ISingletonAwake
    {
        [BsonElement]
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        private Dictionary<int, SpellConfig> dict = new();

        public void Awake()
        {
        }

        public SpellConfig Get(int id)
        {
            this.dict.TryGetValue(id, out SpellConfig item);

            if (item != null)
            {
                return item;
            }

            item = EventSystem.Instance.Invoke<SpellConfigLoader, SpellConfig>(new SpellConfigLoader() { Id = id });
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

    [LabelText("施法类型")]
    public enum CastTimeType
    {
        [LabelText("普通攻击")]
        Attack,

        [LabelText("读条")]
        Cast,

        [LabelText("吟唱")]
        Channeling,
    }

    [Serializable]
    [HideReferenceObjectPicker]
    public partial class SpellConfig : ProtoObject
    {
        [LabelText("技能 ID")]
        public int Id;

        [LabelText("描述")]
        public string Desc;

        //#if UNITY
        //        [UnityEngine.SerializeReference]
        //#endif
        [LabelText("消耗")]
        public List<CostNode> Cost = new();

        #if UNITY
        [UnityEngine.SerializeReference]
        #endif
        [LabelText("目标选择")]
        public TargetSelector TargetSelector;

        [LabelText("CD（毫秒）")]
        public int CD;

        [LabelText("Buff ID")]
        public int BuffId;
    }
}