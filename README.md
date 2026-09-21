# AI NPC Task System

Unity 场景中的 NPC 通过 Python HTTP 服务获取任务，依次完成移动、购买和交付，并回报执行结果。当前使用固定任务列表，尚未接入大语言模型。

## 演示流程

走到商店附近 → 购买一份食物 → 返回家中 → 交付到储物箱。

初始金币为 10，食物单价为 3。成功执行后，NPC 剩余 7 金币、0 份食物，储物箱有 1 份食物。

- 移动前检查目标是否在地图范围内。
- 购买前检查商店距离、价格和余额。
- 交付前检查储物箱距离和背包数量。
- 后端核对指令编号，收到完成回报后推进任务。
- 指令被拒绝或通信失败时，客户端停止任务流程。

## 项目结构

```text
backend/
  server.py                 HTTP 接口与内存中的任务进度
  test_server.py            协议回归测试
unity/
  Assets/Scenes/Main.unity  演示场景
  Assets/Scripts/           通信、移动、背包与储物箱组件
  Assets/Editor/            场景准备与规则验证工具
  Packages/                Unity 包清单与锁文件
  ProjectSettings/         Unity 项目设置
```

## 运行

环境：Python 3.10+，Unity 6000.0.60f1。Python 后端只使用标准库，无需安装依赖。

1. 在仓库根目录启动后端：

   ```powershell
   python backend/server.py
   ```

2. 在 Unity Hub 中添加仓库里的 `unity` 文件夹，打开 `Assets/Scenes/Main.unity`。
3. 等待脚本编译完成，点击 Play。
4. 在 Play 模式下检查 NPC 和 HomeStorage 的食物数量。

后端监听 `127.0.0.1:8001`。浏览器访问 `/health` 可检查服务，`/status` 可查看进度。浏览器不参与任务执行。

**每次重新完整演示前，先退出 Play 并重启 Python。** Unity 的运行时数据和 Python 的任务进度属于两个进程，退出 Play 不会重置后端。

如新脚本没有生效，在 Project 面板右键对应脚本选择 Reimport。检查 BackendConnection 的 NPC、Inventory 引用，以及 NPCInventory 的 Shop、Home Storage 引用。

## 接口

| 方法 | 路径 | 用途 |
|---|---|---|
| GET | /health | 服务状态 |
| GET | /command | 当前指令；执行完毕返回 `{"action":"done"}` |
| GET | /status | 最近回报、当前列表下标和任务数量 |
| POST | /result | 提交 `id` 和 `status` |

指令示例：

```json
{"id": 1, "action": "move", "x": 3.0, "z": 0.8}
```

回报示例：

```json
{"id": 1, "status": "completed"}
```

动作包括 `move`、`buy_food`、`deliver_food`；回报包括 `accepted`、`rejected`、`completed`。正常顺序是 accepted → completed。购买和交付在本地立即完成，成功后依次发送这两次回报。

## 验证

后端测试：

```powershell
python -m unittest discover -s backend -v
```

覆盖完整序列、错误编号、过期回报、非法状态顺序、拒绝和异常请求。

Unity 编辑器中选择 **NPC Demo → Prepare and Validate Scene**，验证购买、余额不足、距离限制、交付和重复交付。此工具会打开并保存 Main 场景，恢复初始金币和背包数量；运行前保存自己的场景修改。

手动验收：

| 场景 | 预期 |
|---|---|
| 默认四条任务 | 返回家中并交付一份食物 |
| 初始金币改为 2 | 购买被拒绝，金币和食物不变，流程停止 |
| 在商店外手动购买 | 距离检查拒绝 |
| 空背包交付 | 储物箱数量不增加 |

## 当前边界

这是单 NPC、本地开发用的任务执行原型。移动为直线运动，没有寻路和避障；商店库存无限。任务保存在进程内，未实现存档、自动重试、跨会话编号、完整状态机和多客户端协调。HTTP 服务不用于公网部署。

网络回报失败时，本地已经发生的购买或移动不会回滚。后续重试设计需要先解决动作去重与执行状态核对。

下一步：结构化失败原因、游戏状态观测，再接入模型生成任务计划。
