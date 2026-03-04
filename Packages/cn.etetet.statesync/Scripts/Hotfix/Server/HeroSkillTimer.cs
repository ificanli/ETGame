namespace ET.Server
{
    [Invoke(TimerInvokeType.HeroSkillTimer)]
    public class HeroSkillTimer : ATimer<HeroSkillComponent>
    {
        protected override void Run(HeroSkillComponent self)
        {
            self.OnSkillTick();
        }
    }
}
