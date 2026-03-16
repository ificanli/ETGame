# Home 城建系统测试设计

## 测试目标

验证 Home 基地建设系统的所有核心功能，确保服务端权威校验、数据持久化、离线结算等功能正确。

## 测试用例清单

### 阶段二：进入Home加载

#### Home_EnterLoad_Test
- 创建机器人进入 Home
- 验证 PlayerHomeComponent 已加载到服务端 Unit 上
- 验证首次进入时建筑数量为 0

### 阶段三：建造、升级、拆除

#### Home_Build_Test
- 给玩家添加足够资源
- 发送建造请求（合法槽位 + 合法建筑）
- 验证 HomeBuilding 子Entity 创建成功
- 验证建筑的 ConfigId、Level、SlotId 正确
- 验证资源正确扣除

#### Home_BuildIllegal_Test
- 资源不足时建造 → 期望错误码 ERR_HomeResourceNotEnough
- 槽位已占用时建造 → 期望错误码 ERR_HomeSlotOccupied

#### Home_Upgrade_Test
- 建造建筑后发送升级请求
- 验证 Level + 1
- 验证资源扣除
- 最大等级时升级 → 期望错误码 ERR_HomeBuildingMaxLevel

#### Home_Demolish_Test
- 建造建筑后发送拆除请求
- 验证建筑 Entity 已移除
- 验证槽位已释放

### 阶段四：收取、生产与离线结算

#### Home_Collect_Test
- 建造补给站
- 手动设置 LastCollectTime 为1小时前
- 调用收取
- 验证获得正确数量物品

#### Home_Production_Test
- 建造工坊，给予材料
- 发送生产请求
- 验证材料扣除，订单创建
- 设置 FinishTime 为过去时间
- 收取产物，验证获得正确物品

#### Home_Relogin_Test
- 建造建筑 → 退出 → 重新进入
- 验证建筑数据完整恢复

### 阶段五：悬赏合同

#### Home_Contract_Test
- 建造情报站
- 触发合同刷新
- 接取合同
- 模拟撤离结算
- 验证合同完成，领取奖励

## 测试规范

- 测试命名：`Home_{TestName}_Test`
- 测试文件位置：`Scripts/Hotfix/Test/`
- 错误码直接返回数字，不定义测试专用 ErrorCode
- 使用 `Log.Console` 输出错误信息
- 每个错误点的返回码唯一
