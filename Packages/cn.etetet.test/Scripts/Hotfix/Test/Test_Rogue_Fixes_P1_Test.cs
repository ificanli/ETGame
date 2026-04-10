using ET.Server;
using Unity.Mathematics;

namespace ET.Test
{
    public class Test_Rogue_Fixes_P1_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_Fixes_P1_Test));
            Scene scene = scope.TestFiber.Root;

            if (scene.CoroutineLockComponent == null)
            {
                scene.AddComponent<CoroutineLockComponent>();
            }

            if (scene.TimerComponent == null)
            {
                scene.AddComponent<TimerComponent>();
            }

            RogueBuffConfigLoader.EnsureRegistered();
            RogueEffectGroupLoader.RegisterAll();

            int nearMonsterSpeedError = VerifyNearMonsterSpeedFix(scene);
            if (nearMonsterSpeedError != ErrorCode.ERR_Success)
            {
                return nearMonsterSpeedError;
            }

            int scaleModifierError = VerifyScaleModifierFix(scene);
            if (scaleModifierError != ErrorCode.ERR_Success)
            {
                return scaleModifierError;
            }

            int healSpiritError = await VerifyHealSpiritFix(scene);
            if (healSpiritError != ErrorCode.ERR_Success)
            {
                return healSpiritError;
            }

            return ErrorCode.ERR_Success;
        }

        private static int VerifyNearMonsterSpeedFix(Scene scene)
        {
            Unit player = TestHelper.CreateServerUnit(scene, UnitType.Player, addProgress: true, campId: 1);
            if (player == null)
            {
                Log.Console("near monster speed player is null");
                return 1;
            }

            player.Position = new float3(5000f, 0f, 5000f);

            if (ApplyOption(player, 1026) != ErrorCode.ERR_Success)
            {
                Log.Console("apply option 1026 failed");
                return 2;
            }

            EffectRogueNearMonsterSpeedBonus effect = BuffConfigCategory.Instance.Get(1026)?.GetEffect<EffectRogueNearMonsterSpeedBonus>();
            if (effect == null || effect.Radius <= 0f)
            {
                Log.Console("rogue buff 1026 effect invalid");
                return 3;
            }

            JoystickMoveComponent moveComponent = player.GetComponent<JoystickMoveComponent>() ?? player.AddComponent<JoystickMoveComponent>();
            Unit monster = CreateMonster(scene, player.Position + new float3(10f, 0f, 0f));
            if (monster == null)
            {
                Log.Console("near monster speed monster is null");
                return 4;
            }

            moveComponent.Direction = float3.zero;
            if (RogueNearMonsterSpeedHelper.IsMovingTowardMonster(player, effect.Radius))
            {
                Log.Console("zero direction should not count as moving toward monster");
                return 5;
            }

            moveComponent.Direction = new float3(1f, 0f, 0f);
            if (!RogueNearMonsterSpeedHelper.IsMovingTowardMonster(player, effect.Radius))
            {
                Log.Console("forward direction should count as moving toward monster");
                return 6;
            }

            moveComponent.Direction = new float3(-1f, 0f, 0f);
            if (RogueNearMonsterSpeedHelper.IsMovingTowardMonster(player, effect.Radius))
            {
                Log.Console("backward direction should not count as moving toward monster");
                return 7;
            }

            moveComponent.Direction = new float3(0f, 0f, 1f);
            if (RogueNearMonsterSpeedHelper.IsMovingTowardMonster(player, effect.Radius))
            {
                Log.Console("orthogonal direction should not count as moving toward monster");
                return 8;
            }

            monster.Position = player.Position + new float3(effect.Radius + 5f, 0f, 0f);
            moveComponent.Direction = new float3(1f, 0f, 0f);
            if (RogueNearMonsterSpeedHelper.IsMovingTowardMonster(player, effect.Radius))
            {
                Log.Console("monster outside radius should not trigger speed bonus");
                return 9;
            }

            return ErrorCode.ERR_Success;
        }

        private static int VerifyScaleModifierFix(Scene scene)
        {
            Unit player = TestHelper.CreateServerUnit(scene, UnitType.Player, addProgress: true, campId: 1);
            if (player == null)
            {
                Log.Console("scale modifier player is null");
                return 20;
            }

            long baseMaxHp = player.NumericComponent?.GetAsLong(NumericType.MaxHP) ?? 0;
            if (baseMaxHp <= 0)
            {
                Log.Console($"scale modifier base max hp invalid: {baseMaxHp}");
                return 21;
            }

            if (ApplyOption(player, 1041) != ErrorCode.ERR_Success)
            {
                Log.Console("apply option 1041 failed");
                return 22;
            }

            long currentMaxHp = player.NumericComponent?.GetAsLong(NumericType.MaxHP) ?? 0;
            if (RogueEffectQueryHelper.GetScaleModifierPermille(player) != 200 || currentMaxHp <= baseMaxHp)
            {
                Log.Console($"scale modifier apply mismatch: scale={RogueEffectQueryHelper.GetScaleModifierPermille(player)}, baseMaxHp={baseMaxHp}, currentMaxHp={currentMaxHp}");
                return 23;
            }

            RogueEffectHelper.RemoveSelectedOption(player, player.GetComponent<RogueProgressComponent>(), 1041);
            long revertedMaxHp = player.NumericComponent?.GetAsLong(NumericType.MaxHP) ?? 0;
            if (RogueEffectQueryHelper.GetScaleModifierPermille(player) != 0 || revertedMaxHp != baseMaxHp)
            {
                Log.Console($"scale modifier revert mismatch: scale={RogueEffectQueryHelper.GetScaleModifierPermille(player)}, revertedMaxHp={revertedMaxHp}, baseMaxHp={baseMaxHp}");
                return 24;
            }

            return ErrorCode.ERR_Success;
        }

        private static async ETTask<int> VerifyHealSpiritFix(Scene scene)
        {
            Unit player = TestHelper.CreateServerUnit(scene, UnitType.Player, addProgress: true, campId: 1);
            if (player == null)
            {
                Log.Console("heal spirit player is null");
                return 40;
            }

            player.Position = new float3(6000f, 0f, 6000f);
            player.Rotation = quaternion.identity;

            if (!TryGetTwoWeaponConfigIds(out int mainWeaponId, out int subWeaponId))
            {
                Log.Console("heal spirit weapon configs missing");
                return 41;
            }

            InitializeWeapons(player, mainWeaponId, subWeaponId);
            WeaponComponent weaponComponent = player.GetComponent<WeaponComponent>();
            float baseDamage = weaponComponent?.GetEffectiveDamage(1) ?? 0f;
            if (weaponComponent == null || baseDamage <= 0f)
            {
                Log.Console($"heal spirit base weapon damage invalid: {baseDamage}");
                return 42;
            }

            NumericComponent numeric = player.NumericComponent;
            long maxHp = numeric?.GetAsLong(NumericType.MaxHP) ?? 0;
            if (numeric == null || maxHp <= 0)
            {
                Log.Console($"heal spirit max hp invalid: {maxHp}");
                return 43;
            }

            long startHp = maxHp / 2;
            if (startHp <= 0)
            {
                startHp = 1;
            }

            numeric.Set(NumericType.HP, startHp);

            if (ApplyOption(player, 1045) != ErrorCode.ERR_Success)
            {
                Log.Console("apply option 1045 failed");
                return 44;
            }

            if (numeric.GetAsLong(NumericType.HP) != startHp)
            {
                Log.Console($"heal spirit should not heal immediately: hp={numeric.GetAsLong(NumericType.HP)}, startHp={startHp}");
                return 45;
            }

            RogueSummonedSpiritStateComponent spiritState = player.GetComponent<RogueSummonedSpiritStateComponent>();
            if (spiritState == null || spiritState.Sources.Count != 1)
            {
                Log.Console($"heal spirit state mismatch: count={spiritState?.Sources.Count ?? 0}");
                return 46;
            }

            long sourceId = 0;
            RogueSummonedSpiritSourceData sourceData = default;
            foreach ((long currentSourceId, RogueSummonedSpiritSourceData currentSourceData) in spiritState.Sources)
            {
                sourceId = currentSourceId;
                sourceData = currentSourceData;
                break;
            }

            Unit spirit = scene.GetComponent<UnitComponent>()?.Get(sourceData.SpiritUnitId);
            if (sourceId == 0 || spirit == null || spirit.IsDisposed)
            {
                Log.Console($"heal spirit unit invalid: sourceId={sourceId}, spiritId={sourceData.SpiritUnitId}");
                return 47;
            }

            RogueWeaponModifierComponent modifierComponent = player.GetComponent<RogueWeaponModifierComponent>();
            int auraDamageBonus = modifierComponent?.GetModifier(1, WeaponModType.BulletDamage) ?? 0;
            if (!sourceData.AuraActive || auraDamageBonus != sourceData.DamageBonusPermille || weaponComponent.GetEffectiveDamage(1) <= baseDamage)
            {
                Log.Console($"heal spirit aura mismatch: active={sourceData.AuraActive}, auraBonus={auraDamageBonus}, damage={weaponComponent.GetEffectiveDamage(1)}, baseDamage={baseDamage}");
                return 48;
            }

            EntityRef<Scene> sceneRef = scene;
            EntityRef<Unit> playerRef = player;
            long spiritId = sourceData.SpiritUnitId;
            ChangePosition_RogueHealSpirit changeHandler = new ChangePosition_RogueHealSpirit();

            float3 oldPos = player.Position;
            player.Position = player.Position + new float3(0f, 0f, 20f);
            await changeHandler.Handle(scene, new ChangePosition
            {
                Unit = player,
                OldPos = oldPos,
            });

            scene = sceneRef;
            player = playerRef;
            spiritState = player.GetComponent<RogueSummonedSpiritStateComponent>();
            if (spiritState == null || !spiritState.Sources.TryGetValue(sourceId, out sourceData) || sourceData.AuraActive)
            {
                Log.Console($"heal spirit aura should be inactive after leaving radius: active={sourceData.AuraActive}");
                return 49;
            }

            weaponComponent = player.GetComponent<WeaponComponent>();
            modifierComponent = player.GetComponent<RogueWeaponModifierComponent>();
            int inactiveAuraBonus = modifierComponent?.GetModifier(1, WeaponModType.BulletDamage) ?? 0;
            if (inactiveAuraBonus != 0 || math.abs(weaponComponent.GetEffectiveDamage(1) - baseDamage) > 0.001f)
            {
                Log.Console($"heal spirit aura should be removed after leaving radius: auraBonus={inactiveAuraBonus}, damage={weaponComponent.GetEffectiveDamage(1)}, baseDamage={baseDamage}");
                return 50;
            }

            Unit spiritBeforePickup = scene.GetComponent<UnitComponent>()?.Get(spiritId);
            if (spiritBeforePickup == null || spiritBeforePickup.IsDisposed)
            {
                Log.Console($"heal spirit pickup target spirit invalid: {spiritId}");
                return 51;
            }

            oldPos = player.Position;
            player.Position = spiritBeforePickup.Position;
            await changeHandler.Handle(scene, new ChangePosition
            {
                Unit = player,
                OldPos = oldPos,
            });

            scene = sceneRef;
            player = playerRef;
            weaponComponent = player.GetComponent<WeaponComponent>();
            NumericComponent numericAfterPickup = player.NumericComponent;
            long expectedHeal = maxHp * sourceData.HealPermille / 1000;
            if (expectedHeal <= 0)
            {
                expectedHeal = 1;
            }

            long expectedHp = startHp + expectedHeal;
            if (expectedHp > maxHp)
            {
                expectedHp = maxHp;
            }

            if (numericAfterPickup == null || numericAfterPickup.GetAsLong(NumericType.HP) != expectedHp)
            {
                Log.Console($"heal spirit pickup heal mismatch: hp={numericAfterPickup?.GetAsLong(NumericType.HP) ?? 0}, expected={expectedHp}");
                return 52;
            }

            if (player.GetComponent<RogueSummonedSpiritStateComponent>() != null)
            {
                Log.Console("heal spirit state should be removed after pickup");
                return 53;
            }

            Unit spiritAfterPickup = scene.GetComponent<UnitComponent>()?.Get(spiritId);
            if (spiritAfterPickup != null && !spiritAfterPickup.IsDisposed)
            {
                Log.Console("heal spirit unit should be disposed after pickup");
                return 54;
            }

            modifierComponent = player.GetComponent<RogueWeaponModifierComponent>();
            if ((modifierComponent?.GetModifier(1, WeaponModType.BulletDamage) ?? 0) != 0)
            {
                Log.Console("heal spirit aura modifier should be removed after pickup");
                return 55;
            }

            if (math.abs(weaponComponent.GetEffectiveDamage(1) - baseDamage) > 0.001f)
            {
                Log.Console($"heal spirit damage should revert after pickup: damage={weaponComponent.GetEffectiveDamage(1)}, baseDamage={baseDamage}");
                return 56;
            }

            return ErrorCode.ERR_Success;
        }

        private static int ApplyOption(Unit unit, int optionId)
        {
            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            RogueProgressComponent progress = unit?.GetComponent<RogueProgressComponent>() ?? RogueProgressHelper.EnsureProgress(unit, false);
            if (unit == null || unit.IsDisposed || progress == null || configCategory == null)
            {
                return ErrorCode.ERR_Cancel;
            }

            if (!configCategory.TryGetOption(optionId, out RogueOptionConfig optionConfig) || optionConfig == null)
            {
                return ErrorCode.ERR_RogueChoiceOptionInvalid;
            }

            return RogueEffectHelper.ApplySelectedOption(unit, progress, optionConfig, optionId, null);
        }

        private static bool TryGetTwoWeaponConfigIds(out int mainWeaponId, out int subWeaponId)
        {
            mainWeaponId = 0;
            subWeaponId = 0;

            foreach (WeaponConfig weaponConfig in WeaponConfigCategory.Instance.DataList)
            {
                if (weaponConfig == null)
                {
                    continue;
                }

                if (mainWeaponId == 0)
                {
                    mainWeaponId = weaponConfig.Id;
                    continue;
                }

                if (weaponConfig.Id != mainWeaponId)
                {
                    subWeaponId = weaponConfig.Id;
                    break;
                }
            }

            return mainWeaponId > 0 && subWeaponId > 0;
        }

        private static void InitializeWeapons(Unit unit, int mainWeaponId, int subWeaponId)
        {
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            EquipmentComponent equipmentComponent = unit.GetComponent<EquipmentComponent>() ?? unit.AddComponent<EquipmentComponent>();
            if (equipmentComponent == null)
            {
                return;
            }

            LoadoutHelper.ApplyLoadout(unit, mainWeaponId, subWeaponId, 0);
            WeaponInitHelper.InitializeWeaponsFromUnit(unit);
        }

        private static Unit CreateMonster(Scene scene, float3 position)
        {
            Unit monster = TestHelper.CreateServerUnit(scene, UnitType.Monster, campId: 2);
            if (monster == null)
            {
                return null;
            }

            monster.Position = position;
            return monster;
        }
    }
}
