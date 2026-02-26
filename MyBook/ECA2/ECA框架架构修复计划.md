# ECA 框架架构修复计划

## 问题总结

当前代码违反了 ET 框架的多个规范，需要系统性重构。

## 修复步骤

### 1. Component 类移到 Model 程序集（修复 ET0004）
- ECAPointComponent: Hotfix/Server → Model/Server
- ECAStateComponent: Hotfix/Server → Model/Server  
- PlayerEvacuationComponent: Hotfix/Server → Model/Server

### 2. Struct 移到 Model 程序集（修复 ET0033）
- StateChangeRecord: Hotfix → Model/Server
- EvacuationParam: Hotfix → Model/Share
- SpawnMonsterParam: Hotfix → Model/Share
- StateCheckParam: Hotfix → Model/Share

### 3. 修正命名空间（修复 ET0112）
- Hotfix/Server 中的类使用 `namespace ET.Server`

### 4. 使用 EntityRef（修复 ETAE001）
- 所有 await 后访问 Entity 的地方改用 EntityRef

### 5. 移除 Unity 类型依赖
- ECAConfig 不使用 Vector3，改用 float x, y, z
- Hotfix/Server 不能引用 UnityEngine

## 当前状态
等待用户确认是否继续修复
