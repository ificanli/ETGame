namespace ET
{
    public static class SpellConfigHelper
    {
        public static bool IsSubSpell(int spellConfigId)
        {
            return spellConfigId % 10 != 0;
        }
        
        public static bool IsMainSpell(int spellConfigId)
        {
            return spellConfigId % 10 == 0;
        }
    }
}