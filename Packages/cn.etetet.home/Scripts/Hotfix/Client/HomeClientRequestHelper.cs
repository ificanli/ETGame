namespace ET.Client
{
    public static class HomeClientRequestHelper
    {
        public static async ETTask<M2C_HomeBuildResponse> Build(Scene root, int slotId, int configId)
        {
            C2M_HomeBuildRequest request = C2M_HomeBuildRequest.Create();
            request.SlotId = slotId;
            request.ConfigId = configId;
            return await root.GetComponent<ClientSenderComponent>().Call(request) as M2C_HomeBuildResponse;
        }

        public static async ETTask<M2C_HomeUpgradeResponse> Upgrade(Scene root, long buildingId)
        {
            C2M_HomeUpgradeRequest request = C2M_HomeUpgradeRequest.Create();
            request.BuildingId = buildingId;
            return await root.GetComponent<ClientSenderComponent>().Call(request) as M2C_HomeUpgradeResponse;
        }

        public static async ETTask<M2C_HomeDemolishResponse> Demolish(Scene root, long buildingId)
        {
            C2M_HomeDemolishRequest request = C2M_HomeDemolishRequest.Create();
            request.BuildingId = buildingId;
            return await root.GetComponent<ClientSenderComponent>().Call(request) as M2C_HomeDemolishResponse;
        }

        public static async ETTask<M2C_HomeCollectResponse> Collect(Scene root, long buildingId)
        {
            C2M_HomeCollectRequest request = C2M_HomeCollectRequest.Create();
            request.BuildingId = buildingId;
            return await root.GetComponent<ClientSenderComponent>().Call(request, false) as M2C_HomeCollectResponse;
        }

        public static async ETTask<M2C_HomeStartProductionResponse> StartProduction(Scene root, long buildingId, int recipeId)
        {
            C2M_HomeStartProductionRequest request = C2M_HomeStartProductionRequest.Create();
            request.BuildingId = buildingId;
            request.RecipeId = recipeId;
            return await root.GetComponent<ClientSenderComponent>().Call(request) as M2C_HomeStartProductionResponse;
        }

        public static async ETTask<M2C_HomeCollectProductionResponse> CollectProduction(Scene root, long orderId)
        {
            C2M_HomeCollectProductionRequest request = C2M_HomeCollectProductionRequest.Create();
            request.OrderId = orderId;
            return await root.GetComponent<ClientSenderComponent>().Call(request) as M2C_HomeCollectProductionResponse;
        }

        public static async ETTask<M2C_HomeMuseumPlaceResponse> MuseumPlace(Scene root, long buildingId, long itemUid)
        {
            C2M_HomeMuseumPlaceRequest request = C2M_HomeMuseumPlaceRequest.Create();
            request.BuildingId = buildingId;
            request.ItemUid = itemUid;
            return await root.GetComponent<ClientSenderComponent>().Call(request) as M2C_HomeMuseumPlaceResponse;
        }

        public static async ETTask<M2C_HomeMuseumTakeDownResponse> MuseumTakeDown(Scene root, long displayId)
        {
            C2M_HomeMuseumTakeDownRequest request = C2M_HomeMuseumTakeDownRequest.Create();
            request.DisplayId = displayId;
            return await root.GetComponent<ClientSenderComponent>().Call(request) as M2C_HomeMuseumTakeDownResponse;
        }
    }
}
