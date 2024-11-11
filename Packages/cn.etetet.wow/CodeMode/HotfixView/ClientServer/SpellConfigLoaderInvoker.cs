using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ET.Client
{
    [Invoke]
    public class SpellConfigLoaderInvoker: AInvokeHandler<SpellConfigLoader, SpellConfig>
    {
        public override SpellConfig Handle(SpellConfigLoader args)
        {
            SpellScriptableObject spellScriptableObject = ResourcesComponent.Instance.LoadAssetSync<SpellScriptableObject>(args.Id.ToString());
            return spellScriptableObject.SpellConfig;
        }
    }
}