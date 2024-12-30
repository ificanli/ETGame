#if UNITY
using System;
using MongoDB.Bson.Serialization.Attributes;
using Sirenix.OdinInspector;

namespace ET
{
    [EnableClass]
    public class OdinUnityObject
    {

        [HorizontalGroup("Split", 0.3f)]
        [BsonIgnore]
        [HideLabel]
        [ShowInInspector, PreviewField(45, ObjectFieldAlignment.Left)]
        [OnValueChanged("OnValueChanged")]
        [NonSerialized]
        private UnityEngine.Object Object;
        
        [HideLabel]
        [HorizontalGroup("Split", 0.7f)]
        [ReadOnly]
        [ShowIf("Show")]
        public string Name;

        private void OnValueChanged()
        {
            if (this.Object == null)
            {
                this.Name = "";
                return;
            }

            this.Name = this.Object.name;
        }

        private bool Show()
        {
            if (string.IsNullOrEmpty(this.Name))
            {
                this.Object         = null;
                return true;
            }

#if UNITY_EDITOR
            string s = this.Name;
            if (this.Object != null)
            {
                s = $"{s} t:{this.Object.GetType().Name}";
            }
            foreach (string guid in UnityEditor.AssetDatabase.FindAssets($"{s}", null))
            {
                string path   = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                UnityEngine.Object obj = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (obj == null)
                {
                    UnityEngine.Debug.LogError($"找不到资源: {this.Name}");
                }
                else
                {
                    this.Object = obj;
                    break;
                }
            }
#endif
            return true;
        }

    }
}
#endif