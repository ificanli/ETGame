namespace ET.Server
{
    public static class RogueMissionTaskECAActionRegistrar
    {
        public static void EnsureRegistered()
        {
            if (ECAFlowActionRegistry.TryGet(ECAFlowActionKey.StartTask, out _))
            {
                return;
            }

            ECAFlowActionRegistry.Register(ECAFlowActionKey.StartTask, RogueMissionTaskPointHelper.HandleStartTaskAsync);
        }
    }
}
