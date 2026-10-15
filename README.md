# 《社会演化：AI经济模拟》P0 骨架

Unity 6.3 LTS，团队统一使用 **6000.3.26f1**（changeset `de8167e7440c`）。请用 Unity Hub 安装这一 patch，不要换别的 6000.3 版本。版本锁在 `ProjectSettings/ProjectVersion.txt`。

这是 P0-1～P0-3：能推进时钟，有开局种子，建筑能入出库。没有招聘撮合、市场结算、发薪、生产，也没有数值 Utility 的决策循环。

## 目录

```text
Assets/Scripts/Core/        时钟与步进钩子（不引用引擎）
Assets/Scripts/Domain/      市民、建筑、岗位、商品、金库（不引用引擎）
Assets/Scripts/World/       建筑工地与库存（不引用引擎）
Assets/Scripts/Bootstrap/   开局种子、GameSession、Unity 帧驱动
Assets/Scenes/Bootstrap.unity
docs/                       接口签名与开局种子
tests/                      纯 C# 回归，不进 Unity 导入
```

`Domain`、`Core`、`World` 的 asmdef 设置了 `noEngineReferences: true`。只有 `Bootstrap` 引用 Unity，而且引擎代码只在 `Bootstrap/Unity/SimulationDriver.cs`。

## 用编辑器推进时钟

1. 用 Hub 打开本仓库根目录，编辑器版本选 **6000.3.26f1**。
2. 首次打开会生成 `Library/` 和其余 `ProjectSettings`。不要提交 `Library/`。
3. 打开 `Assets/Scenes/Bootstrap.unity`。场景里有一个 `Simulation` 物体，挂着 `SimulationDriver`。
4. 进入 Play。`Awake` 调用 `GameSession.CreateNew()`，`Update` 调用 `AdvanceRealtime(Time.deltaTime)`。
5. 默认 **1 现实秒 = 1 游戏时**（1x）。一天 24 游戏时，所以 1x 下一天是 24 现实秒。
6. 暂停：`driver.Pause()`。恢复：`driver.Resume()`。暂停期间倍率保持不变。
7. 五倍速：`driver.SetTimeScale(TimeScales.X5)`。也接受 20、100、1000 和任意有限正数。`0` 不是倍率，暂停用 `Pause()`。

场景没有相机，Game 视图是空的。这局里只有时钟在走。

若场景在导入时提示缺脚本，确认 `SimulationDriver.cs.meta` 的 guid 是 `e7c4a1b25d6f4e0a9c3b5d7e1f2a4b6c`。也可以新建空场景，加一个空物体，挂上 `SimulationDriver`，再 Play。

## 不打开 Unity 时推进时钟

```bash
dotnet test tests/SocialEvolution.Tests/SocialEvolution.Tests.csproj
```

测试工程把 `Assets/Scripts` 里的纯 C# 编成四个程序集（不含 `SimulationDriver`）。播放模式的推进对应 `AdvanceRealtime`；测试和脚本用 `StepHours`，它不受暂停和倍率影响。

```csharp
GameSession session = GameSession.CreateNew(); // 第 1 日 0 时，已挂空的小时/日回调
session.Time.StepHours(24);                    // 进入第 2 日 0 时；先小时回调，跨日后再日回调
session.Time.SetTimeScale(TimeScales.X5);
session.Time.AdvanceRealtime(1f);              // 1x 下 1 秒 = 1 游戏时，5x 下这 1 秒走 5 游戏时
session.Time.Pause();                          // 之后 AdvanceRealtime 不再走字
```

回调里不能再调用 `StepHours` / `AdvanceRealtime`（禁止重入）。开局时刻本身不触发回调。

## 开局种子

`OpeningSeed.Create()`，细目见 [docs/opening-seed.md](docs/opening-seed.md)。

| 项 | 内容 |
|---|---|
| 市民 | 120，`市民001`–`市民120`，含年龄、学历、技能、健康系数、个人财富和住所 |
| 金库 | 500_000，允许之后被扣成负数，不因此结束游戏 |
| 建筑 | 政府厅、城郊农场、工具工坊、集市、公共住宅 ×4 |
| 政府库存 | 食物 2400、木材 100、工具 20 |
| 农场库存 | 食物 100、工具 10 |
| 工厂库存 | 木材 50 |
| 市场 / 住宅库存 | 空 |
| 岗位 | 农民 20、农场主管 2、工人 12、商人 4、公务员 5。`Openings` 只是编制，Domain 不记录在职人数 |
| 住所 | 4 栋住宅各 30 人，记在市民的 `ResidenceId` |

食物 2400 = 120 人 × 20 日的账面储备。本里程碑不吃掉食物。

## 硬约束

- 没有私企模块。
- 没有点名派工。不存在把市民和岗位绑在一起的 API。`WorldState.SetJobPosting` 只改编制、时薪和 `RequiredSkillId`。在职人数不在 Domain。
- `Treasury.Debit` 可以把余额扣成负数。没有 Game Over。
- 决策如果以后要做，只用纯数值 Utility。这里没有聊天，也没有语言模型调用。
- 商品只有 `Food`、`Wood`、`Tool`。

## 接口摘要

完整签名在 [docs/interfaces.md](docs/interfaces.md)。

```csharp
public interface ITimeService : ISimStepHooks
{
    GameDateTime Now { get; }
    int Day { get; }
    int Hour { get; }
    bool IsPaused { get; }
    float TimeScale { get; }
    float SecondsPerGameHour { get; }
    int MaxHoursPerAdvance { get; }
    void Pause();
    void Resume();
    void SetTimeScale(float scale);
    int AdvanceRealtime(float deltaSeconds);
    int StepHours(int gameHours);
}

public interface ISimStepHooks
{
    void RegisterGameHour(Action<GameDateTime> callback);
    void UnregisterGameHour(Action<GameDateTime> callback);
    void RegisterGameDay(Action<GameDateTime> callback);
    void UnregisterGameDay(Action<GameDateTime> callback);
}

public enum Good { Food = 1, Wood = 2, Tool = 3 }
public enum BuildingKind { Farm = 1, Factory = 2, Housing = 3, Market = 4, Government = 5 }

public static class OpeningSeed
{
    public static WorldState Create();
}

public interface IInventory
{
    int Capacity { get; }
    int TotalQuantity { get; }
    int GetQuantity(Good good);
    InventoryResult TryDeposit(Good good, int quantity);
    InventoryResult TryWithdraw(Good good, int quantity);
}

public static class Inventory
{
    public static InventoryResult TryTransfer(IInventory from, IInventory to, Good good, int quantity);
}
```

库存按总件数计容量。数量不是正数、超过容量、库存不够、未知商品，都整笔失败，库存不变。未知建筑由 `WorldState.TryDeposit` / `TryWithdraw` 返回 `UnknownBuilding`。

## 本里程碑不做

招聘撮合、离职、发薪、生产配方、市场订单、价格、税收、移民、存档、Utility 评分、聊天。小时和日回调的默认实现是 `SimNoOp`，推进时钟不会改金库、库存、人口或空岗。
