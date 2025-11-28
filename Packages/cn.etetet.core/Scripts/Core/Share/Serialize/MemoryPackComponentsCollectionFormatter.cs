using MemoryPack.Internal;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using MemoryPack;
using MemoryPack.Formatters;

#nullable enable
namespace ET
{
    /// <summary>
    /// 替换默认的SortedDictionaryFormatter
    /// </summary>
    [Preserve]
    public sealed class MemoryPackComponentsCollectionFormatter : MemoryPackFormatter<ComponentsCollection>
    {
        [Preserve]
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, ref ComponentsCollection? value)
        {
            if (value == null)
            {
                writer.WriteNullCollectionHeader();
                return;
            }
            
            // writer.WriteCollectionHeader(value.Count);
            var formatter = writer.GetFormatter<Entity>();
            ref byte spanReference = ref writer.GetSpanReference(4);
            writer.Advance(4);
            int count = 0;
            foreach (var kv in value)
            {
                Entity entity = kv.Value;
                if (entity.IsSerializeWithParent)
                {
                    ++count;
                    formatter.Serialize(ref writer, ref entity!);
                }
            }
            SafeUnsafe.WriteUnaligned(ref spanReference, count);
        }

        [Preserve]
        public override void Deserialize(ref MemoryPackReader reader, ref ComponentsCollection? value)
        {
            if (!reader.TryReadCollectionHeader(out int length))
            {
                value = null;
                return;
            }

            if (value == null)
            {
                // 这里甚至可以用对象池
                value = ComponentsCollection.Create(true);
            }
            else
            {
                value.Clear();
            }

            var formatter = reader.GetFormatter<Entity>();
            for (int i = 0; i < length; i++)
            {
                Entity entity = null!;
                formatter.Deserialize(ref reader, ref entity!);
                entity.IsSerializeWithParent = true;
                long key = entity.GetLongHashCode();
                value.Add(key, entity);
            }
        }
    }
}