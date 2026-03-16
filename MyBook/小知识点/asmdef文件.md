asmdef 是 Assembly Definition（程序集定义）文件，Unity 用它来控制代码的编译和组织。                                                                                                                                                        

  基本概念                                                                                                                                                                                                                                      
  在 Unity 中：                                                                                                                                                                                                                              
  - 默认情况下，所有脚本都编译到几个预定义的程序集（Assembly-CSharp.dll）                                                                                                                                                                    
  - 每次修改任何脚本，整个程序集都要重新编译，很慢
  - asmdef 可以把代码分割成独立的程序集，只重新编译修改的部分

  主要配置项

  {
      "name": "Ignore.ET.ECA",              // 程序集名称                                                                                                                                                                                    
      "references": ["Unity.Mathematics"],   // 依赖的其他程序集                                                                                                                                                                             
      "autoReferenced": true,                // 是否被自动引用                                                                                                                                                                               
      "defineConstraints": ["IGNORE"],       // 编译条件                                                                                                                                                                                     
      "includePlatforms": [],                // 包含的平台（空=所有）                                                                                                                                                                        
      "excludePlatforms": []                 // 排除的平台                                                                                                                                                                                   
  }

  ET 框架的特殊用法

● 在 ET 框架中，Ignore.ET.ECA.asmdef 的特殊之处：                                                                                                                                                                                            

  1. 名称前缀 "Ignore"：                                                                                                                                                                                                                        - ET 的代码生成工具会跳过这些程序集
    - 因为 MonoBehaviour 不需要 ET 的代码生成                                                                                                                                                                                                
  2. defineConstraints: ["IGNORE"]：                                                                                                                                                                                                         
    - 这个约束条件实际上永远不会满足（项目中没有定义 IGNORE 符号）
    - 但配合 autoReferenced: true，Unity Editor 仍然会加载它
    - 这样既能让 Unity 看到组件，又能让 ET 工具忽略它
  3. autoReferenced: true：
    - Unity Editor 会自动引用这个程序集
    - 所以 Add Component 菜单能找到 ECAPointMarker

  为什么 BindPointComponent 和 ECAPointMarker 需要这样配置

  正常的 ET 代码（需要代码生成）
  └─ ET.Hotfix.asmdef, ET.Model.asmdef 等

  Unity 场景配置代码（不需要代码生成）
  └─ Ignore.ET.Spell.asmdef, Ignore.ET.ECA.asmdef 等

  这样设计的好处：
  - 编译隔离：MonoBehaviour 代码独立编译，不影响 ET 框架
  - 工具兼容：ET 代码生成工具不会处理这些文件
  - 包独立：可以单独发布包，不依赖 Assets 目录

  现在你重启 Unity 后，应该就能在 Add Component 中找到 ECAPointMarker 了。
