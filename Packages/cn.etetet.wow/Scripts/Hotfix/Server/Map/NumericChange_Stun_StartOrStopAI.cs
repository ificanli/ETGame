namespace ET.Server
{
    [NumericWatcher(SceneType.Map, NumericType.Stun)]
    public class NumericChange_Stun_StartOrStopAI: INumericWatcher
    {
        public void Run(Unit unit, NumbericChange args)
        {
            switch (args)
            {
                case { Old: > 0, New: <= 0 }:
                    unit.GetComponent<AIComponent>()?.Start();
                    break;
                case { Old: <= 0, New: > 0 }:
                    BuffHelper.RemoveBuffFlag(unit, BuffFlags.StunRemove);
                    unit.Stop(0);
                    unit.GetComponent<AIComponent>()?.Stop();
                    break;
            }
        }
    }
}