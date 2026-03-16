# et-test-run - 测试执行专家

这个skill专门负责执行测试、查看日志和分析失败原因。

## 使用场景

- 执行测试（最常用）
- 查看测试日志
- 调试测试失败
- 验证修改是否正确

## 命令参数

- `--Name`：处理器类名正则，默认 `.*`，匹配所有测试用例
- `--Console=1`：启用Console输出（可选）
- 解析与分发：控制台命令由 `TestConsoleHandler` 解析，按正则从 `TestDispatcher` 获取匹配的处理器并逐一运行

## 快速执行测试

### 编译测试场景,注意,必须用Debug模式
```bash
dotnet build ET.sln
```

### 启动测试场景

```bash
dotnet ./Bin/ET.App.dll --SceneName=Test
```

进程会输出 `>` 等待输入

### 执行测试命令

#### 执行所有测试

输入：`Test`

#### 执行指定测试（支持正则匹配）

输入：`Test --Name=CreateRobot`
输入：`Test --Name=Quest.*`

### 完整单命令执行

```bash
printf "Test\n" | pwsh -Command "dotnet ./Bin/ET.App.dll --SceneName=Test"
```

或执行指定测试：

```bash
printf "Test --Name=CreateRobot\n" | pwsh -Command "dotnet ./Bin/ET.App.dll --SceneName=Test"
```

## 重要提醒

1. **测试前删除Logs目录**，方便查找问题

## 示例输出

### 启动并执行

```bash
dotnet ./Bin/ET.App.dll --SceneName=Test
> Test
Test.Test_CreateRobot start
Test.Test_CreateRobot success
```

### 执行指定测试

```bash
> Test --Name=CreateRobot
Test.Test_CreateRobot start
Test.Test_CreateRobot success
```

### 未找到测试

```bash
> Test --Name=CreateRobot2
not found test! package: .* name: CreateRobot2
```

### 成功输出

```
Test.Test_XXX_Test success
```

### 失败输出

```
Test.Test_XXX_Test fail
[详细错误信息]
```

## 日志分析

### 日志位置

- 日志目录：`Logs/`
- 完整日志：`Logs/All.log`

### 查看日志

```bash
cat Logs/All.log
```

### 日志规范

- 测试日志应使用英文
- 普通日志用Log.Debug
- 错误日志用Log.Error
- Console输出用Log.Console（需要启动参数Console=1）

## 调试失败用例

### 1. 查看输出错误信息

测试失败时会输出详细错误，先看控制台输出

### 2. 检查All.log

```bash
cat Logs/All.log | grep -i error
```

### 3. 常见失败原因

- **Entity已失效**：检查await后是否用EntityRef重新获取
- **数据不一致**：检查测试数据准备逻辑
- **消息超时**：检查网络消息是否正确发送
- **Fiber未找到**：检查场景和Fiber名称

### 4. 重新执行测试

修复代码后：
1. 编译项目（使用et-build skill）
2. 删除Logs目录
3. 重新执行测试

## 测试工作流

```
修改代码 → 编译(et-build) → 删除Logs → 执行测试 → 查看结果 → 检查日志
```

## 优势

- **支持正则匹配**：灵活筛选测试用例
- **一次启动多次测试**：提高效率
- **保持进程状态**：减少启动开销
- **实时查看结果**：快速反馈

## 需要编写测试用例？

如果需要编写新的测试用例，请使用 `/et-test-write` skill。
