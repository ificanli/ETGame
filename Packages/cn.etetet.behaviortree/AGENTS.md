# cn.etetet.behaviortree

## 概述

行为树核心与编辑器包，负责 BTNode 定义、行为树运行时支持，以及编辑器中节点可视化与参数配置。

## 目录约定

- `Scripts/Model/Share`：行为树节点与通用模型代码
- `Editor/BehaviorTreeEditor`：行为树编辑器与 Inspector 扩展

## 开发约定

- 运行时逻辑与编辑器逻辑分离，编辑器代码必须放在 `Editor` 目录下
- BTInput/BTOutput 参数只用于节点参数的输入输出描述，不直接参与运行时逻辑
