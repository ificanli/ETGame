namespace ET
{
    public static class MapHelper
    {
        public static string GetMapName(this string mapFullName)
        {
            if (mapFullName.Contains('@'))
            {
                return mapFullName.Split('@')[0];
            }
            return mapFullName;
        }
        
        public static int GetMapLine(this string mapFullName)
        {
            if (mapFullName.Contains('@'))
            {
                return int.Parse(mapFullName.Split('@')[1]);
            }
            return 0;
        }
    }
}