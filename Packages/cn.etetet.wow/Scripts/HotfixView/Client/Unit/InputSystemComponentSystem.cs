using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ET.Client
{
    [EntitySystemOf(typeof(InputSystemComponent))]
    public static partial class InputSystemComponentSystem
    {
        [EntitySystem]
        private static void Awake(this InputSystemComponent self)
        {
            self.CinemachineComponent = self.GetParent<Unit>().GetComponent<CinemachineComponent>();
            
            self.InputSystem = new InputSystem();
            self.InputSystem.Player.Enable();

            self.InputSystem.Player.Look.performed += self.Look;
            self.InputSystem.Player.Jump.started += self.Jump;
            self.InputSystem.Player.SelectTarget.canceled += self.SelectTarget;
            self.InputSystem.Player.ChangeTarget.canceled += self.ChangeTarget;
            self.InputSystem.Player.Spell.started += (contex)=>
            {
                self.CastSpell(contex).WithContext(new ETCancellationToken());
            };
        }
        
        [EntitySystem]
        private static void Update(this InputSystemComponent self)
        {
            if (self.InputSystem.Player.Move.IsPressed())
            {
                if (TimeInfo.Instance.FrameTime - self.PressTime > 100)
                {
                    self.PressTime = TimeInfo.Instance.FrameTime;
                    Vector2 v = self.InputSystem.Player.Move.ReadValue<Vector2>();
                    self.Move(v);    
                }
            }
        }

        private static void Look(this InputSystemComponent self, InputAction.CallbackContext context)
        {
            if (!Mouse.current.rightButton.isPressed)
            {
                return;
            }
            
            Vector2 v = context.ReadValue<Vector2>();
            CinemachineComponent cinemachineComponent = self.CinemachineComponent;
            cinemachineComponent.RotationFollow(v);
        }
        
        private static void Move(this InputSystemComponent self, Vector2 v)
        {
            if (self.IsJumping)
            {
                return;
            }
            
            if (v.magnitude < 0.001f)
            {
                return;
            }
            
            Unit unit = self.GetParent<Unit>();
            v = v.normalized * 2;
            
            CinemachineComponent cinemachineComponent = self.CinemachineComponent;
            Vector3 eulerAngles = cinemachineComponent.Follow.rotation.eulerAngles;
            eulerAngles.x = 0;
            eulerAngles.z = 0;
            
            Vector3 rotV = Quaternion.Euler(eulerAngles) * new float3(v.x, 0, v.y);

            float3 targetPos = new float3(rotV) + unit.Position;
            
            unit.MoveToAsync(targetPos).NoContext();
        }
        
        // 鼠标左键点击目标，设置主角的目标
        private static void SelectTarget(this InputSystemComponent self, InputAction.CallbackContext context)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePosition);
            const float maxDistance = 100.0f;
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, maxDistance, LayerMask.NameToLayer("Unit")))
            {
                return;
            }
            GameObject clickedObject = hit.collider.gameObject;
            
            Unit targetUnit = (Unit)clickedObject.GetComponent<GameObjectEntityRef>().EntityRef.Entity;
            Unit myUnit = UnitHelper.GetMyUnitFromCurrentScene(self.Scene());
            myUnit.GetComponent<TargetComponent>().Unit = targetUnit;
        }
        
        private static void ChangeTarget(this InputSystemComponent self, InputAction.CallbackContext context)
        {
            
        }

        private static void Jump(this InputSystemComponent self, InputAction.CallbackContext context)
        {
            
        }
        
        private static async ETTask CastSpell(this InputSystemComponent self, InputAction.CallbackContext context)
        {
            if (context.control is not KeyControl keyControl)
            {
                return;
            }

            int spellConfigId = keyControl.keyCode - Key.Digit1 + 10000;
            SpellConfig spellConfig = SpellConfigCategory.Instance.Get(spellConfigId);
            
            C2M_SpellCast c2MSpellCast = C2M_SpellCast.Create();
            c2MSpellCast.SpellConfigId = spellConfigId;
            
            Unit unit = self.GetParent<Unit>();
            
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