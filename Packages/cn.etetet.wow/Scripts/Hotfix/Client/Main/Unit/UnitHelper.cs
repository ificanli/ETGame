namespace ET.Client
{
    public static partial class UnitHelper
    {
        public static Unit GetMyUnitFromClientScene(Scene root)
        {
            PlayerComponent playerComponent = root.GetComponent<PlayerComponent>();
            if (playerComponent == null)
            {
                Log.Error($"playerComponent null");
                return default;
            }

            Scene currentScene = root.GetComponent<CurrentScenesComponent>()?.Scene;
            if (currentScene != null)
            {
                Log.Error($"currentScene null");
                return default;
            }

            var unit = currentScene.GetComponent<UnitComponent>().Get(playerComponent.MyId);
            if (unit == null)
            {
                return default;
            }

            return unit;
        }

        public static Unit GetMyUnitFromCurrentScene(Scene currentScene)
        {
            PlayerComponent playerComponent = currentScene.Root().GetComponent<PlayerComponent>();
            return currentScene.GetComponent<UnitComponent>().Get(playerComponent.MyId);
        }

        public static bool IsMyUnit(this Unit self)
        {
            if (self.Root().GetComponent<PlayerComponent>().MyId == self.Id)
            {
                return true;
            }

            return false;
        }
    }
}