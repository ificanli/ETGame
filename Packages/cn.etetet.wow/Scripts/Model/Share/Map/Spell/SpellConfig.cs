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
        
        public void Add(SpellConfig spellConfig)
        {
            this.dict.Add(spellConfig.Id, spellConfig);
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
        [BoxGroup("技能信息")]
        [LabelText("技能 ID")]
        public int Id;

        [BoxGroup("技能信息")]
        [LabelText("描述")]
        public string Desc;

#if UNITY
        [BoxGroup("技能信息")]
        [InlineProperty] // 去掉折叠和标题
        [HideReferenceObjectPicker]
        [LabelText("图标")]
        public OdinUnityObject Icon = new();
#endif
        
        [BoxGroup("技能信息")]
        [LabelText("技能CD（毫秒）")]
        public int CD;

        [BoxGroup("技能信息")]
        [LabelText("Buff ID")]
        public int BuffId;

        [LabelText("消耗")]
        public List<CostNode> Cost = new();

        #if UNITY
        [UnityEngine.SerializeReference]
        #endif
        [LabelText("目标选择")]
        public TargetSelector TargetSelector;
    }
}