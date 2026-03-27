# cn.etetet.login

## 概述

`cn.etetet.login` 负责 Realm / Gate 登录链路、Session 与 Player 绑定，以及登录阶段的玩家初始化。

## 开发约定

- Gate Handler 只负责会话校验、玩家定位和跨服务转发，不直接落业务存储实现。
- 登录成功后的玩家初始化，优先通过各业务包提供的 Helper 完成，不在 Handler 内堆业务细节。
- `SessionPlayerComponent` 只维护当前 Session 与 `Player` 的关联，不承载额外业务状态。
- 新增 Gate 外网协议时，优先放在与业务最接近的包中定义 proto，再在本包补 Session Handler。

## 边界

- `Player` 负责账号维度玩家实体。
- `PlayerComponent` 负责 Gate 侧在线玩家管理。
- `SessionPlayerComponent` / `PlayerSessionComponent` 负责 Session 与 Player 双向关联。
- 档案、仓库、战绩等运行时数据不在本包存储，由对应业务包服务化提供。
