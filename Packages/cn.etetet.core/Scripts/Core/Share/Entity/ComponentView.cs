#if ENABLE_VIEW && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ET
{
    [HideMonoScript]
    public class ComponentView : MonoBehaviour
    {
        // 你的 Entity 对象
        public EntityRef<Entity> Component
        {
            get;
            set;
        }
        
        [ShowInInspector, DisplayAsString]
        public long InstanceId => this.Entity?.InstanceId ?? 0;

        [ShowInInspector, DisplayAsString]
        public long Id => this.Entity?.Id ?? 0;

        private readonly List<MemberSnapshotBase> properties = new();

        [ShowInInspector, ShowIf(nameof(HasProperties)), PropertyOrder(110), ListDrawerSettings(ShowFoldout = false, HideAddButton = true, HideRemoveButton = true, DraggableItems = false, ShowIndexLabels = false, ShowItemCount = false)]
        private List<MemberSnapshotBase> Properties
        {
            get
            {
                this.RefreshDebugMembers();
                return this.properties;
            }
            set // 这里不能删除，删除后会变成无法编辑
            {
            }
        }

        private bool HasProperties()
        {
            this.RefreshDebugMembers();
            return this.properties.Count > 0;
        }

        [ShowInInspector, InlineProperty, HideLabel, HideReferenceObjectPicker]
        [PropertyOrder(100)]
        private Entity Entity
        {
            get => Component;
            set => Component = value;
        }

        [InlineProperty, HideLabel, HideReferenceObjectPicker]
        private abstract class MemberSnapshotBase
        {
            protected MemberSnapshotBase(string name)
            {
                this.Name = name;
            }

            public string Name { get; }
        }

        private sealed class MemberSnapshot<T> : MemberSnapshotBase
        {
            private readonly EntityRef<Entity> targetRef;
            private readonly PropertyInfo property;

            public MemberSnapshot(Entity target, PropertyInfo property) : base(property.Name)
            {
                this.targetRef = target;
                this.property = property;
            }

            [ShowInInspector, LabelText("@Name")]
            public T Value
            {
                get
                {
                    Entity target = this.targetRef;
                    if (target == null)
                    {
                        return default;
                    }
                    object value;
                    try
                    {
                        value = this.property.GetValue(target);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[ComponentView] 读取调试字段失败: {this.property?.DeclaringType?.FullName}.{this.property?.Name} {ex.GetType().Name} {ex.Message}");
                        return default;
                    }
                    if (value == null)
                    {
                        return default;
                    }
                    return (T)value;
                }
                set
                {
                    try
                    {
                        Entity target = this.targetRef;
                        if (target == null)
                        {
                            return;
                        }
                        this.property.SetValue(target, value);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[ComponentView] 写入调试字段失败: {this.property?.DeclaringType?.FullName}.{this.property?.Name} {ex.GetType().Name} {ex.Message}");
                    }
                }
            }
        }

        private void RefreshDebugMembers()
        {
            Entity entity = this.Entity;
            if (entity == null)
            {
                this.properties.Clear();
                return;
            }

            this.properties.Clear();

            PropertyInfo[] pInfos = entity.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (PropertyInfo property in pInfos)
            {
                if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                if (property.DeclaringType == typeof(Entity))
                {
                    continue;
                }

                Type propertyType = property.PropertyType;
                if (propertyType.IsByRef || propertyType.IsPointer)
                {
                    continue;
                }

                Type snapshotType = typeof(MemberSnapshot<>).MakeGenericType(propertyType);
                try
                {
                    var snapshot = (MemberSnapshotBase)Activator.CreateInstance(snapshotType, entity, property);
                    if (snapshot != null)
                    {
                        this.properties.Add(snapshot);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[ComponentView] 创建调试字段失败: {entity?.GetType().FullName}.{property?.Name} {ex.GetType().Name} {ex.Message}");
                }
            }
        }

    }
}
#endif
