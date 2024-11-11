namespace ET
{
    public static class NumericTypeHelper
    {
        public static long Get(this NumericComponent self, NumericType numericType)
        {
            return self.GetByKey((int)numericType);
        }

        public static int GetAsInt(this NumericComponent self, NumericType numericType)
        {
            return self.GetAsInt((int)numericType);
        }

        public static long GetAsLong(this NumericComponent self, NumericType numericType)
        {
            return self.GetAsLong((int)numericType);
        }

        public static float GetAsFloat(this NumericComponent self, NumericType numericType)
        {
            return self.GetAsFloat((int)numericType);
        }
        
        public static void Set(this NumericComponent self, NumericType nt, int value)
        {
            self[(int)nt] = value;
        }

        public static void Set(this NumericComponent self, NumericType nt, long value)
        {
            self[(int)nt] = value;
        }

        public static void SetNoEvent(this NumericComponent self, NumericType numericType, long value)
        {
            self.Insert((int)numericType, value, false);
        }
    }
}