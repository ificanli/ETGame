using System;

namespace ET.Client
{
    public static partial class CurrentScenesComponentSystem
    {
        public static Scene CurrentScene(this Scene root)
        {
            return root.GetComponent<CurrentScenesComponent>()?.Scene;
        }
    }
}