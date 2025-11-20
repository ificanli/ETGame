using System;
using System.Collections.Generic;

namespace ET
{
    public class SceneTypeSingleton: EnumSingleton<SceneTypeSingleton>, ISingletonAwake<Type>
    {
        public static bool IsSame(int a, int b)
        {
            if (a == b)
            {
                return true;
            }

            if (a == 0)
            {
                return true;
            }

            if (b == 0)
            {
                return true;
            }

            return false;
        }
        
        public void Awake(Type type)
        {
            Init(type);
        }
		
        public string GetSceneName(int sceneType)
        {
            return this.GetStringByValue(sceneType);
        }
		
        public int GetSceneType(string sceneName)
        {
            int type = this.GetValueByName(sceneName);
            if (type == 0)
            {
                throw new Exception($"not found scene type: {type} {sceneName}");
            }
            return type;
        }

        public Dictionary<int, string> GetAll()
        {
            return this.enumValueString.GetAll();
        }
    }
}