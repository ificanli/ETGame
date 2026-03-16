# cn.etetet.battle - 战局系统包

## 包概述

战局系统负责管理战局的生命周期，包括战局会话管理、状态机流转和结算框架。

### 核心功能

- **战局会话**：创建/销毁战局实例
- **状态机**：准备 → 战斗 → 结算
- **结算框架**：击杀数、存活状态、财富收集

---

## 包信息

- **包名**：cn.etetet.battle
- **PackageType ID**：55
- **层级**：第3层
- **依赖**：cn.etetet.core, cn.etetet.proto, cn.etetet.unit, cn.etetet.match

---

## 目录结构

```
cn.etetet.battle/
├── packagegit.json
├── package.json
├── AGENTS.md
├── Scripts/
│   ├── Model/
│   │   ├── Share/
│   │   │   └── PackageType.cs
│   │   └── Server/
│   └── Hotfix/
│       ├── Server/
│       └── Test/
```

---

## 开发规范

### Entity 规范

- 继承 Entity
- 实现 IAwake, IDestroy
- 添加 [ComponentOf] 特性
- 只包含数据字段，无方法

### System 规范

- 静态类 + partial
- 添加 [EntitySystemOf] 特性
- 生命周期方法添加 [EntitySystem] 特性
- 业务方法是静态扩展方法

### EntityRef 规范

- await 前创建 EntityRef
- await 后重新获取
- 检查有效性后再使用

### 禁止项

1. Entity 中定义方法
2. hard code 配置值
3. await 后直接使用 Entity
4. Hotfix 程序集中声明非 const 字段

---

## 更新日志

### 2026-02-27
- 创建 cn.etetet.battle 空壳包
