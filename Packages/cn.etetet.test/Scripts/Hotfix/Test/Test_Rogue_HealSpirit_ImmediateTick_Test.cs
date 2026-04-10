using ET.Server;
using Unity.Mathematics;

namespace ET.Test
{
    public class Test_Rogue_HealSpirit_ImmediateTick_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_HealSpirit_ImmediateTick_Test));
            Fiber testFiber = scope.TestFiber;
            Scene scene = testFiber.Root;

            UnitComponent unitComponent = scene.GetComponent<UnitComponent>() ?? scene.AddComponent<UnitComponent>();
            if (scene.CoroutineLockComponent == null)
            {
                scene.AddComponent<CoroutineLockComponent>();
            }

            UnitConfig playerConfig = null;
            foreach (UnitConfig config in UnitConfigCategory.Instance.DataList)
            {
                if (config.UnitType == UnitType.Player)
                {
                    playerConfig = config;
                    break;
                }
            }

            if (playerConfig == null)
            {
                Log.Console("player unit config is null");
                return 1;
            }

            Unit unit = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), playerConfig.Id);
            unit.UnitType = UnitType.Player;

            NumericComponent numeric = unit.AddComponent<NumericComponent>();
            foreach ((int numericType, long numericValue) in playerConfig.KV)
            {
                numeric.SetNoEvent(numericType, numericValue);
            }

            unit.AddComponent<BuffComponent>();

            RogueBuffConfigLoader.EnsureRegistered();
            RogueEffectGroupLoader.RegisterAll();

            BuffConfig buffConfig = BuffConfigCategory.Instance.Get(1045);
            if (buffConfig == null)
            {
                Log.Console("rogue buff config 1045 is null");
                return 2;
            }

            EffectServerBuffAdd addEffect = buffConfig.GetEffect<EffectServerBuffAdd>();
            EffectServerBuffTick tickEffect = buffConfig.GetEffect<EffectServerBuffTick>();
            if (addEffect == null || tickEffect == null)
            {
                Log.Console("rogue buff 1045 add/tick effect is null");
                return 3;
            }

            if (addEffect.Unit != "Unit")
            {
                Log.Console($"rogue buff 1045 add root unit key invalid: {addEffect.Unit}");
                return 4;
            }

            if (tickEffect.Unit != "Unit")
            {
                Log.Console($"rogue buff 1045 tick root unit key invalid: {tickEffect.Unit}");
                return 5;
            }

            if (addEffect.Children == null || addEffect.Children.Count != 1 || addEffect.Children[0] is not BTRogueSummonHealSpirit addNode)
            {
                Log.Console($"rogue buff 1045 add children invalid, count={addEffect.Children?.Count ?? 0}");
                return 6;
            }

            if (tickEffect.Children == null || tickEffect.Children.Count != 1 || tickEffect.Children[0] is not BTRogueSummonHealSpirit tickNode)
            {
                Log.Console($"rogue buff 1045 tick children invalid, count={tickEffect.Children?.Count ?? 0}");
                return 7;
            }

            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            if (configCategory == null || !configCategory.TryGetOption(1045, out RogueOptionConfig optionConfig) || optionConfig == null)
            {
                Log.Console("rogue option config 1045 is null");
                return 8;
            }

            if (!RogueOptionConfigHelper.TryGetBuffConfigId(optionConfig, out int resolvedBuffConfigId) || resolvedBuffConfigId != 1045)
            {
                Log.Console($"rogue option 1045 resolved buff invalid: {resolvedBuffConfigId}");
                return 9;
            }

            if (addNode.Unit != "Unit" || tickNode.Unit != "Unit")
            {
                Log.Console($"rogue buff 1045 summon node unit key invalid: add={addNode.Unit}, tick={tickNode.Unit}");
                return 10;
            }

            long maxHp = numeric.GetAsLong(NumericType.MaxHP);
            if (maxHp <= 0)
            {
                Log.Console($"max hp invalid: {maxHp}");
                return 11;
            }

            long startHp = maxHp / 2;
            if (startHp <= 0)
            {
                startHp = 1;
            }

            numeric.Set(NumericType.HP, startHp);

            EffectRogueHealSpirit healSpiritEffect = buffConfig.GetEffect<EffectRogueHealSpirit>();
            if (healSpiritEffect == null || healSpiritEffect.HealPermille <= 0)
            {
                Log.Console("rogue buff 1045 heal spirit effect invalid");
                return 12;
            }

            long expectedHeal = maxHp * healSpiritEffect.HealPermille / 1000;
            if (expectedHeal <= 0)
            {
                expectedHeal = 1;
            }

            long expectedHp = startHp + expectedHeal;
            if (expectedHp > maxHp)
            {
                expectedHp = maxHp;
            }

            RogueProgressComponent progress = RogueProgressHelper.EnsureProgress(unit, false);
            if (progress == null)
            {
                Log.Console("rogue progress is null");
                return 13;
            }

            int applyError = RogueEffectHelper.ApplySelectedOption(unit, progress, optionConfig, 1045, null);
            if (applyError != ErrorCode.ERR_Success)
            {
                Log.Console($"apply option 1045 failed, error={applyError}");
                return 14;
            }

            unit.Position = float3.zero;
            unit.Rotation = quaternion.identity;

            long actualHp = unit.NumericComponent?.GetAsLong(NumericType.HP) ?? 0;
            if (actualHp != startHp)
            {
                Log.Console($"rogue option 1045 should not heal immediately, hp={actualHp}, startHp={startHp}, expectedPickupHp={expectedHp}");
                return 15;
            }

            RogueSummonedSpiritStateComponent spiritState = unit.GetComponent<RogueSummonedSpiritStateComponent>();
            if (spiritState == null || spiritState.Sources.Count != 1)
            {
                Log.Console($"rogue option 1045 spirit state mismatch, count={spiritState?.Sources.Count ?? 0}");
                return 16;
            }

            long spiritUnitId = 0;
            RogueSummonedSpiritSourceData spiritSourceData = default;
            foreach (RogueSummonedSpiritSourceData sourceData in spiritState.Sources.Values)
            {
                spiritUnitId = sourceData.SpiritUnitId;
                spiritSourceData = sourceData;
                break;
            }

            if (spiritUnitId == 0)
            {
                Log.Console("rogue option 1045 spirit unit id invalid");
                return 17;
            }

            Unit spirit = scene.GetComponent<UnitComponent>()?.Get(spiritUnitId);
            if (spirit == null || spirit.IsDisposed || spirit.UnitType != UnitType.Pet)
            {
                Log.Console($"rogue option 1045 spirit unit invalid, id={spiritUnitId}, type={spirit?.UnitType}");
                return 18;
            }

            PetComponent petComponent = spirit.GetComponent<PetComponent>();
            if (petComponent == null || petComponent.OwnerId != unit.Id)
            {
                Log.Console($"rogue option 1045 pet owner mismatch, ownerId={petComponent?.OwnerId ?? 0}, expected={unit.Id}");
                return 19;
            }

            RogueWeaponModifierComponent modifierComponent = unit.GetComponent<RogueWeaponModifierComponent>();
            int auraDamageBonus = modifierComponent?.GetModifier(1, WeaponModType.BulletDamage) ?? 0;
            if (!spiritSourceData.AuraActive || auraDamageBonus != healSpiritEffect.DamageBonusPermille)
            {
                Log.Console($"rogue option 1045 aura mismatch, active={spiritSourceData.AuraActive}, damageBonus={auraDamageBonus}, expected={healSpiritEffect.DamageBonusPermille}");
                return 20;
            }

            RogueEffectHelper.RemoveSelectedOption(unit, progress, 1045);
            if (unit.GetComponent<RogueSummonedSpiritStateComponent>() != null)
            {
                Log.Console("rogue option 1045 spirit state should be removed after option remove");
                return 21;
            }

            spirit = scene.GetComponent<UnitComponent>()?.Get(spiritUnitId);
            if (spirit != null && !spirit.IsDisposed)
            {
                Log.Console("rogue option 1045 spirit should be disposed after option remove");
                return 22;
            }

            modifierComponent = unit.GetComponent<RogueWeaponModifierComponent>();
            if (modifierComponent != null && modifierComponent.GetModifier(1, WeaponModType.BulletDamage) != 0)
            {
                Log.Console("rogue option 1045 aura modifier should be removed after option remove");
                return 23;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
