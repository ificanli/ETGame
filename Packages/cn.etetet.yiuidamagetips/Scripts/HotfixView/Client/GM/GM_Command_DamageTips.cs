using System.Collections.Generic;
using UnityEngine;
using YIUIFramework;

namespace ET.Client
{
    //主要用于在GM包上测试功能
    //当前包没有强制引用GM包
    //如果没有引用GM包  请删除这个文件
    [GM(EGMType.DamageTips, 0, "3D伤害提示")]
    public class GM_DamageTips_1 : IGMCommand
    {
        public List<GMParamInfo> GetParams()
        {
            return new()
            {
                new GMParamInfo(EGMParamType.String, "预制", "Damage_3D_SawNeon"),
            };
        }

        public async ETTask<bool> Run(Scene clientScene, ParamVo paramVo)
        {
            var unitComponent = clientScene.CurrentScene().GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                Log.Error("unitComponent is null");
                return false;
            }

            var prefabName = paramVo.Get<string>();

            EntityRef<Scene> sceneRef = clientScene;
            foreach (var child in unitComponent.Children)
            {
                if (child.Value is Unit unit)
                {
                    var damage = Random.Range(1, 100);
                    clientScene = sceneRef;
                    await DamageTipsHelper.Show3D(clientScene, prefabName, unit, damage);
                }
            }

            return true;
        }
    }

    [GM(EGMType.DamageTips, 0, "UI伤害提示")]
    public class GM_DamageTips_2 : IGMCommand
    {
        public List<GMParamInfo> GetParams()
        {
            return new()
            {
                new GMParamInfo(EGMParamType.String, "预制", "Damage_UI_PixelHeal"),
            };
        }

        public async ETTask<bool> Run(Scene clientScene, ParamVo paramVo)
        {
            var prefabName = paramVo.Get<string>();
            var damage = Random.Range(1, 100);
            Vector2 screenPoint = new(Screen.width / 2, Screen.height / 2);
            DamageTipsHelper.ShowUIByScreenPoint(clientScene, prefabName, screenPoint, damage).Coroutine();
            await ETTask.CompletedTask;
            return true;
        }
    }
}