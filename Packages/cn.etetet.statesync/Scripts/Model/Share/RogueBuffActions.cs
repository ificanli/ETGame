using Sirenix.OdinInspector;

namespace ET
{
    public class BTRogueAddGold : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        public int Amount;
        public int SourceType = RogueGoldSourceType.RogueCard;
        public bool SyncProgress = true;
    }

    public class BTRogueGrantItem : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";

        public int ItemConfigId;
        public int Count = 1;
    }

    public class BTRogueCleanupTemporaryItems : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";
    }

    public class BTRogueRadarScan : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";
    }

    public class BTRogueGrantRandomCard : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";

        public int Quality;
        public int Count = 1;
        public int FixedOptionId;
    }

    public class BTRogueReplaceAllCards : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";
    }

    public class BTRogueRegisterObjective : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";

        public int ObjectiveId;
        public int GoalValue;
    }

    public class BTRogueUnregisterObjective : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";
    }

    public class BTRogueAddWeaponModifiers : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";
    }

    public class BTRogueRemoveWeaponModifiers : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";
    }

    public class BTRogueApplyInteractRangeBonus : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";
    }

    public class BTRogueRemoveInteractRangeBonus : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";
    }

    public class BTRogueUpdateBagSpaceHpBonus : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";
    }

    public class BTRogueClearBagSpaceHpBonus : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";
    }

    public class BTRogueUpdateNearMonsterSpeed : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";
    }

    public class BTRogueClearNearMonsterSpeed : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";
    }

    public class BTRogueClearOutOfCombatStealth : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";
    }

    public class BTRogueApplySilentSearch : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";
    }

    public class BTRogueRemoveSilentSearch : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";
    }

    public class BTRogueApplyTrapMaster : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";
    }

    public class BTRogueRemoveTrapMaster : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";
    }

    public class BTRogueApplyPassiveEffects : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";
    }

    public class BTRogueRemovePassiveEffects : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";
    }

    public class BTRogueApplyHitHeroCritGrowth : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";
    }

    public class BTRogueRemoveHitHeroCritGrowth : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";
    }

    public class BTRogueApplyReloadFirstShotsBoost : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";
    }

    public class BTRogueRemoveReloadFirstShotsBoost : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";
    }

    public class BTRogueApplyScaleModifier : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";

        public int MaxHpPermille;
    }

    public class BTRogueRevertScaleModifier : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";
    }

    public class BTRogueApplySpeedFinalPct : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";

        public int Value;
    }

    public class BTRogueRemoveSpeedFinalPct : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";
    }

    public class BTRoguePeriodicHpScale : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";

        public int MaxHpPermille;
    }

    public class BTRogueRevertPeriodicHpScale : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";
    }

    public class BTRogueHealMaxHpPermille : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        public int HealPermille;
    }

    public class BTRogueSummonHealSpirit : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";
    }

    public class BTRogueRemoveSummonedSpirit : BTAction
    {
        [BTInput(typeof(Unit))]
        [BoxGroup("输入参数")]
        public string Unit = "Unit";

        [BTInput(typeof(Buff))]
        [BoxGroup("输入参数")]
        public string Buff = "Buff";
    }
}
