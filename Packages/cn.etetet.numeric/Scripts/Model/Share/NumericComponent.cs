using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace ET
{
    public struct NumbericChange
    {
        public EntityRef<Unit> Unit { get; set; }
        public int NumericType {get; set;}
        public long Old  {get; set;}
        public long New  {get; set;}
    }
    
    public partial class Unit
    {
        private EntityRef<NumericComponent> numericComponent;

        public NumericComponent NumericComponent
        {
            get
            {
                return this.numericComponent;
            }
            set
            {
                this.numericComponent = value;
            }
        }
    }

    [DisableGetComponent]
    [ComponentOf(typeof (Unit))]
    public class NumericComponent: Entity, IAwake, ITransfer, IDeserialize
    {
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        public Dictionary<int, long> NumericDic = new();
    }
}