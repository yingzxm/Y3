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
    public ITimeService Time { get; }
    public WorldState World { get; }
    public static GameSession CreateNew();
}
```

容量是一座建筑里全部商品的件数之和。失败的入出库不改数量，也不部分成交。`Inventory.TryTransfer` 同样整笔成功或双方都不变。
