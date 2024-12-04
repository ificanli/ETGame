namespace ET
{
    public static class UnitTypeHelper
    {
        public static bool IsSame(this UnitType a, UnitType b)
        {
            return (a & b) != 0;
        }
    }
}