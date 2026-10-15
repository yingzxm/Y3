# 接口签名（P0）

引擎：Unity 6.3 LTS **6000.3.26f1**（changeset `de8167e7440c`）。全员用 Hub 安装这一 patch。

程序集：`SocialEvolution.Core`、`SocialEvolution.Domain`、`SocialEvolution.World` 不引用 Unity。`SocialEvolution.Bootstrap` 引用这三者；`MonoBehaviour` 只出现在 `SimulationDriver`。

## 时间

`GameDateTime.HoursPerDay = 24`。游戏日从 1 起，游戏时为 0–23。开局是 `GameDateTime.Origin`（第 1 日 0 时），`TotalHours` 为 0。语言版本对齐 Unity：C# 9。

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

public readonly struct GameDateTime
{
    public const int HoursPerDay = 24;
    public int Day { get; }
    public int Hour { get; }
    public long TotalHours { get; }
    public GameDateTime AddHours(int hours);
}

public interface ISimStepHooks
{
    void RegisterGameHour(Action<GameDateTime> callback);
    void UnregisterGameHour(Action<GameDateTime> callback);
    void RegisterGameDay(Action<GameDateTime> callback);
    void UnregisterGameDay(Action<GameDateTime> callback);
}

public static class TimeScales
{
    public const float X1 = 1f;
    public const float X5 = 5f;
    public const float X20 = 20f;
    public const float X100 = 100f;
    public const float X1000 = 1000f;
    public static bool IsSupported(float scale);
}
```

`TimeService` 的默认 `SecondsPerGameHour` 是 1，`MaxHoursPerAdvance` 默认 24。`AdvanceRealtime` 看暂停和倍率，单次最多推进 `MaxHoursPerAdvance` 个游戏时，超出的积压直接丢掉。`StepHours` 按游戏时直接走，不受这个上限、暂停和倍率影响。进入新的一小时时调用小时回调；从 23 时进入下一日 0 时，先小时回调，再日回调。同一委托重复注册只留一份。回调内再次推进会抛 `InvalidOperationException`。

`GameSession.CreateNew()` 会注册 `SimNoOp.OnGameHour` 和 `SimNoOp.OnGameDay`。这两个方法是空的。

```csharp
public sealed class SimulationDriver : MonoBehaviour
{
    public GameSession? Session { get; }
    public void Pause();
    public void Resume();
    public void SetTimeScale(float scale);
}
```

## 领域

```csharp
public enum Good { Food = 1, Wood = 2, Tool = 3 }

public enum BuildingKind
{
    Farm = 1,
    Factory = 2,
    Housing = 3,
    Market = 4,
    Government = 5,
}

public enum EducationLevel { None = 0, Primary = 1, Secondary = 2, Vocational = 3, Higher = 4 }

public static class SkillIds
{
    public const string Farming = "farming";
    public const string Manufacturing = "manufacturing";
    public const string Trade = "trade";
    public const string Administration = "administration";
    public static bool IsKnown(string skillId);
}

public readonly struct SkillLevel
{
    public SkillLevel(string skillId, int level);
    public string SkillId { get; }
    public int Level { get; }
}

public interface IPersonReadModel
{
    CitizenId Id { get; }
    string Name { get; }
    int Age { get; }
    EducationLevel Education { get; }
    IReadOnlyList<SkillLevel> Skills { get; }
    float HealthFactor { get; }
    long Wealth { get; }
    BuildingId ResidenceId { get; }
}

public sealed class Citizen : IPersonReadModel
{
    public Citizen(
        CitizenId id,
        string name,
        int age,
        EducationLevel education,
        IReadOnlyList<SkillLevel> skills,
        float healthFactor,
        long wealth,
        BuildingId residenceId);
}

public sealed class Treasury
{
    public Treasury(long balance);
    public long Balance { get; }
    public void Credit(long amount);
    public void Debit(long amount); // 允许余额为负，不结束游戏
}

public sealed class JobPosting
{
    public JobPostingId Id { get; }
    public BuildingId BuildingId { get; }
    public string Title { get; }
    public int Openings { get; }
    public long WagePerHour { get; }
    public int MinSkillLevel { get; }
    public string RequiredSkillId { get; }
    public void Configure(string title, int openings, long wagePerHour, int minSkillLevel, string requiredSkillId);
}

public sealed class Building
{
    public Building(BuildingId id, BuildingKind kind, string name, int housingResidentCapacity = 0);
    public BuildingId Id { get; }
    public BuildingKind Kind { get; }
    public string Name { get; }
    public int HousingResidentCapacity { get; }
    public IReadOnlyList<JobPosting> JobPostings { get; }
    internal JobPosting SetJobPosting(JobPostingId id, string title, int openings, long wagePerHour, int minSkillLevel, string requiredSkillId);
    public bool RemoveJobPosting(JobPostingId id);
}
```

`Building.SetJobPosting` 是 internal，`InternalsVisibleTo("SocialEvolution.World")`。外部改编制走 `WorldState.SetJobPosting`。`Openings` 只是编制人数。在职人数不在 Domain。货币字段（金库、工资、个人财富）都是 `long`。

## 世界与种子

```csharp
public interface IInventory
{
    int Capacity { get; }
    int TotalQuantity { get; }
    int GetQuantity(Good good);
    InventoryResult TryDeposit(Good good, int quantity);
    InventoryResult TryWithdraw(Good good, int quantity);
}

public sealed class Inventory : IInventory
{
    public static InventoryResult TryTransfer(IInventory from, IInventory to, Good good, int quantity);
}

public readonly struct InventoryResult
{
    public bool Succeeded { get; }
    public InventoryRejection Rejection { get; }
    public int Quantity { get; }
}

public enum InventoryRejection
{
    None = 0,
    InvalidQuantity = 1,
    ExceedsCapacity = 2,
    InsufficientStock = 3,
    UnknownGood = 4,
    UnknownBuilding = 5,
}

public sealed class WorldState
{
    public WorldState(IReadOnlyList<Citizen> citizens, IReadOnlyList<BuildingSite> sites, Treasury treasury);
    public IReadOnlyList<Citizen> Citizens { get; }
    public IReadOnlyList<BuildingSite> Sites { get; }
    public Treasury Treasury { get; }
    public int GetQuantity(BuildingId buildingId, Good good);
    public InventoryResult TryDeposit(BuildingId buildingId, Good good, int quantity);
    public InventoryResult TryWithdraw(BuildingId buildingId, Good good, int quantity);
    public JobPosting SetJobPosting(BuildingId buildingId, JobPostingId postingId, string title, int openings, long wagePerHour, int minSkillLevel, string requiredSkillId);
    public bool RemoveJobPosting(BuildingId buildingId, JobPostingId postingId);
}

public static class OpeningSeed
{
    public static WorldState Create();
}

public sealed class GameSession
{
    public GameSession(ITimeService time, WorldState world);
    public GameSession(ITimeService time, WorldState world, CityMarket? market);
    public ITimeService Time { get; }
    public WorldState World { get; }
    public CityMarket? Market { get; }
    public static GameSession CreateNew();
}
```

容量是一座建筑里全部商品的件数之和。失败的入出库不改数量，也不部分成交。`Inventory.TryTransfer` 同样整笔成功或双方都不变。

## 市场（P0-6）

程序集 `SocialEvolution.Economy`。引用 Domain、World、Core，不引用 Unity。定价和生产不放进 World：World 只保管库存；市场要直接拿 `GameDateTime` 去挂小时钩子。

本地工程根目录约定为 `A:\UnityProject\Gap`。用 Unity Hub 打开这个目录，编辑器版本仍是 **6000.3.26f1**。

货币是 `long`。商品仍是 `Food`、`Wood`、`Tool`。没有私企，没有点名派工。金库可以为负，不结束游戏。政府买卖是正式入口，不是空接口。建设、设岗和财政面板仍属于 P0-7。

```csharp
public interface ICashLedger
{
    long Balance { get; }
    void Credit(long amount);
    bool TryDebit(long amount);
}

public sealed class CashLedger : ICashLedger
{
    public CashLedger(long balance, bool allowNegative = false);
    public long Balance { get; }
    public bool AllowNegative { get; }
    public void Credit(long amount);
    public bool TryDebit(long amount);
}

public sealed class TreasuryLedger : ICashLedger
{
    public TreasuryLedger(Treasury treasury);
    public long Balance { get; }
    public void Credit(long amount);
    public bool TryDebit(long amount); // 始终成功，金库可为负
}

public enum MarketRejection
{
    None = 0,
    InvalidQuantity = 1,
    UnknownGood = 2,
    InsufficientCash = 3,
    InsufficientStock = 4,
    ExceedsCapacity = 5,
    NoGovernment = 6,
    NoMarket = 7,
}

public readonly struct MarketResult
{
    public bool Succeeded { get; }
    public MarketRejection Rejection { get; }
    public Good Good { get; }
    public int Quantity { get; }
    public long UnitPrice { get; }
    public long CashMoved { get; }
}

public sealed class GoodMarketState
{
    public Good Good { get; }
    public long MidPrice { get; }
    public long AnchorPrice { get; }
    public long FloorPrice { get; }
    public long CeilingPrice { get; }
    public int TargetStock { get; }
    public long InventoryPressure { get; }
    public int RecentVolume { get; }
    public long LastTradePrice { get; }
}

public interface IGovernmentMarketOps
{
    MarketResult TryGovernmentBuy(Good good, int quantity);
    MarketResult TryGovernmentSell(Good good, int quantity);
}

public interface ICityMarket : IGovernmentMarketOps
{
    IReadOnlyList<GoodMarketState> Goods { get; }
    long ClearingCash { get; }
    GoodMarketState GetState(Good good);
    MarketResult TryBuy(ICashLedger payer, IInventory destination, Good good, int quantity);
    MarketResult TrySell(ICashLedger payee, IInventory source, Good good, int quantity);
    void RunProduction();
    void SettlePrices();
    void OnGameHour(GameDateTime now);
}

public sealed class CityMarket : ICityMarket
{
    public const long InitialClearingCash = 2000000L;
    public const int MaxTradePriceStep = 5;
    public const int DriftStep = 1;
    public const int FarmFoodReserve = 100;
    public const long FoodMidPrice = 10L;
    public const long WoodMidPrice = 8L;
    public const long ToolMidPrice = 40L;
    public CityMarket(WorldState world);
    public WorldState World { get; }
    public GameDateTime LastSettled { get; }
}

public static class StateProduction
{
    public const int FarmFoodPerHour = 4;
    public const int FarmWoodPerHour = 1;
    public const int FactoryWoodInput = 1;
    public const int FactoryToolOutput = 1;
    public static void Run(WorldState world);
    public static bool TryConvert(IInventory inventory, Good input, int inputQuantity, Good output, int outputQuantity);
}
```

开局中间价：食物 10（下限 1，上限 100，目标库存 200），木材 8（1–80，目标 80），工具 40（2–400，目标 40）。

买卖按当前中间价加减步长成交。步长是 `min(数量, MaxTradePriceStep)`，至少为 1。买入抬价，卖出压价，然后把 `MidPrice` 设成这笔成交价。现金和库存整笔成功或整笔不变。买方现金不够就拒绝；金库账户不拒绝，余额可以为负。集市建筑的库存是可买供给。清算账户起始 `InitialClearingCash`，允许为负，它不是金库。

每游戏时结算一次。这一小时有成交：`MidPrice` 保持最近成交价。没有成交：向库存压力价移动最多 `DriftStep`（1）。压力价是 `clamp(锚定价 * 目标库存 / max(集市库存, 1), 下限, 上限)`。集市库存高则向下，低则向上。

生产不看在职人数。农场每小时产出 4 食物和 1 木材（没有林场，木材走农场旁路）。工厂每小时消耗 1 木材、产出 1 工具；出库失败或入库失败则这一步不产，输入会退回。日后接 Labor 时，用到岗比例缩放这批产量。

`GameSession.CreateNew()` 在小时钩子上注册 `market.OnGameHour`。回调顺序：`StateProduction.Run` → 农场把超出 100 的食物、以及木材，工厂把工具，用 `TrySell` 卖进集市 → `SettlePrices`。`SimulationDriver.Awake` 调用 `CreateNew()`，所以进 Play 就挂上了。政府厅库存不会被这个钩子自动卖掉。

### Play 验证

1. 用 Unity Hub 打开 `A:\UnityProject\Gap`（本仓库根目录），版本 **6000.3.26f1**。
2. 打开 `Assets/Scenes/Bootstrap.unity`。场景物体 `Simulation` 上有 `SimulationDriver` 和 `MarketDebugPanel`。
3. 点 Play。Game 视图左上角出现面板：日/时、暂停、1x、5x、金库、三种商品的中间价、集市/政府/农场/工厂库存、最近成交。
4. 点「暂停 Pause」。记下食物中间价、集市库存、政府库存和金库。
5. 数量填 `10`，点食物的「卖出 Sell」。中间价下降，集市食物增加，政府食物减少，金库增加。状态行写着成交价和 `中间价 a → b`。
6. 再点食物的「买入 Buy」。中间价上升，库存反向走。把金库先花光之后再买，余额可以变成负数，游戏不结束。
7. 数量填 `0` 或在集市没有货时买入：状态行报失败，库存、金库和中间价都不变。
8. 点「继续 Resume」，再点 5x。每游戏时农场产出入库并卖进集市，三种库存会增加；有成交时中间价停在最近成交价，某种商品这一小时没有成交时，中间价向库存压力价移动 1。
9. 岗位只显示编制和时薪，不能在这里改。设岗设薪是 P0-7。

不打开 Unity 时：

```bash
dotnet test tests/SocialEvolution.Tests/SocialEvolution.Tests.csproj
```
