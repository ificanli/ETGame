namespace ET.Server
{
    public static class ErrorHelper
    {
        public static void MapError(Unit unit, int error, params string[] strs)
        {
            if (unit.Type() != UnitType.Player)
            {
                return;
            }
            M2C_Error m2CError = M2C_Error.Create();
            m2CError.Error = error;
            m2CError.Values.AddRange(strs);
            MapMessageHelper.NoticeClient(unit, m2CError, NoticeType.Self);
        }
    }
}