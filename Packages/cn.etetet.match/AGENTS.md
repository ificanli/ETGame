# cn.etetet.match - 匹配系统包

## 包概述

匹配系统负责管理玩家的匹配队列和战局创建流程。

### 核心功能

- **匹配队列**：按模式分队列（PVE/1v1/3v3/搜打撤）
- **匹配策略**：超时处理、人数凑齐判定
- **战局创建**：匹配成功后分配地图并通知玩家

---

## 包信息

- **包名**：cn.etetet.match
- **PackageType ID**：54
- **层级**：第2层
- **依赖**：cn.etetet.core, cn.etetet.proto, cn.etetet.unit

---

## 目录结构

```
cn.etetet.match/
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
- 创建 cn.etetet.match 空壳包
