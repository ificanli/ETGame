using ET.Client;

namespace ET
{
    [NumericWatcher(SceneType.Current, (int)NumericType.HP)]
    public class NumericWatcher_UnitInfo_Hp : INumericWatcher
    {
        public void Run(Unit unit, NumbericChange args)
        {
            //Log.Error($"HP Name:{args.Unit.Config().Name},{args.Unit.Id}, Type:{args.NumericType}, New:{args.New}, Old:{args.Old}");

            //临时处理全监听
            YIUIMgrComponent.Inst.DynamicEvent(args).NoContext();
            
            var oldHP  = args.Old;
            var newHP  = args.New;
            var damage = oldHP - newHP;
            if (damage > 0)
            {
                //临时处理 只要少血就飘字
                DamageTipsHelper.Show3D("Damage_3D_SawNeon", args.Unit, damage).NoContext();
            }
        }
    }

    [NumericWatcher(SceneType.Current, (int)NumericType.MaxHP)]
    public class NumericWatcher_UnitInfo_HPMax : INumericWatcher
    {
        public void Run(Unit unit, NumbericChange args)
        {
            //Log.Error($"MaxHP Name:{args.Unit.Config().Name},{args.Unit.Id}, Type:{args.NumericType}, New:{args.New}, Old:{args.Old}");

            //临时处理全监听
            YIUIMgrComponent.Inst.DynamicEvent(args).NoContext();
        }
    }

    [NumericWatcher(SceneType.Current, (int)NumericType.MP)]
    public class NumericWatcher_UnitInfo_MP : INumericWatcher
    {
        public void Run(Unit unit, NumbericChange args)
        {
            //Log.Error($"MP Name:{args.Unit.Config().Name},{args.Unit.Id}, Type:{args.NumericType}, New:{args.New}, Old:{args.Old}");

            //临时处理全监听
            YIUIMgrComponent.Inst.DynamicEvent(args).NoContext();
        }
    }

    [NumericWatcher(SceneType.Current, (int)NumericType.MaxMP)]
    public class NumericWatcher_UnitInfo_MPMax : INumericWatcher
    {
        public void Run(Unit unit, NumbericChange args)
        {
            //Log.Error($"MaxMP Name:{args.Unit.Config().Name},{args.Unit.Id}, Type:{args.NumericType}, New:{args.New}, Old:{args.Old}");

            //临时处理全监听
            YIUIMgrComponent.Inst.DynamicEvent(args).NoContext();
        }
    }
}