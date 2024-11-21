using Unity.Mathematics;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class OnSpellTriggerEvent: AEvent<Scene, OnSpellTrigger>
    {
        protected override async ETTask Run(Scene scene, OnSpellTrigger a)
        {
            int spellConfigId = a.SpellConfigId;
            SpellConfig spellConfig = SpellConfigCategory.Instance.Get(spellConfigId);
            
            C2M_SpellCast c2MSpellCast = C2M_SpellCast.Create();
            c2MSpellCast.SpellConfigId = spellConfigId;
            
            Unit unit = a.Unit;
            
            // 这里根据技能目标选择方式，等待目标选择
            switch (spellConfig.TargetSelector)
            {
                case TargetSelectorSingle targetSelectorSingle:
                {
                    // 没有技能指示器
                    
                    // 等待玩家选择目标
                    Unit target = unit.GetComponent<TargetComponent>().Unit;
                    if (target == null)
                    {
                        TextHelper.OutputText(TextConstDefine.SpellCast_NotSelectTarget);
                        return;
                    }

                    float unitRadius = unit.GetComponent<NumericComponent>().GetAsFloat(NumericType.Radius);
                    float targetRadius = target.GetComponent<NumericComponent>().GetAsFloat(NumericType.Radius);
                    float distance = math.distance(unit.Position, target.Position);
                    if (distance > targetSelectorSingle.MaxDistance + unitRadius + targetRadius)
                    {
                        TextHelper.OutputText(TextConstDefine.SpellCast_TargetTooFar);
                        return;
                    }
                    c2MSpellCast.TargetUnitId = target.Id;
                    break;
                }
                case TargetSelectorPosition targetSelectorPosition:
                {
                    // 创建技能指示器
                    SpellIndicatorComponent spellIndicatorComponent = unit.GetComponent<SpellIndicatorComponent>();
                    //c2MSpellCast.TargetPosition = await spellIndicatorComponent.WaitSpellIndicator(targetSelectorPosition.SpellIndicator, 1f);
                    
                    // await 技能按键松开返回鼠标一个位置，表现层技能指示器不一样
                    break;
                }
                case TargetSelectorSector targetSelectorSector:
                {
                    // 创建技能指示器
                    SpellIndicatorComponent spellIndicatorComponent = unit.GetComponent<SpellIndicatorComponent>();
                    //c2MSpellCast.TargetPosition = await spellIndicatorComponent.WaitSpellIndicator(targetSelectorSector);
                    
                    // await 技能按键松开返回鼠标一个位置，表现层技能指示器不一样
                    break;
                }
                case TargetSelectorRectangle targetSelectorRectangle:
                {
                    // 创建技能指示器
                    SpellIndicatorComponent spellIndicatorComponent = unit.GetComponent<SpellIndicatorComponent>();
                    //c2MSpellCast.TargetPosition = await spellIndicatorComponent.WaitSpellIndicator(targetSelectorRectangle);
                    // await 技能按键松开返回鼠标一个位置，表现层技能指示器不一样
                    break;
                }
                
                case TargetSelectorCircle targetSelectorCircle:
                {
                    // 创建技能指示器
                    SpellIndicatorComponent spellIndicatorComponent = unit.GetComponent<SpellIndicatorComponent>();
                    c2MSpellCast.TargetPosition = await spellIndicatorComponent.WaitSpellIndicator(targetSelectorCircle);
                    
                    // await 技能按键松开返回鼠标一个位置，表现层技能指示器不一样
                    break;
                }
            }
            
            SpellHelper.Cast(unit, c2MSpellCast);
        }
    }
}