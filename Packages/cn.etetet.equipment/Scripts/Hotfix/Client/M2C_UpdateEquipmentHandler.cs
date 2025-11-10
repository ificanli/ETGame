namespace ET.Client
{
    /// <summary>
    /// 装备更新通知Handler
    /// </summary>
    [MessageHandler(SceneType.Client)]
    public class M2C_UpdateEquipmentHandler : MessageHandler<Scene, M2C_UpdateEquipment>
    {
        protected override async ETTask Run(Scene scene, M2C_UpdateEquipment message)
        {
            EquipmentComponent equipmentComponent = scene.GetComponent<EquipmentComponent>();
            if (equipmentComponent == null)
            {
                Log.Error("EquipmentComponent not found in scene");
                return;
            }

            // 更新装备数据
            equipmentComponent.UpdateEquipment(message.SlotType, message.ItemId, message.ConfigId);
            
            await ETTask.CompletedTask;
        }
    }
}