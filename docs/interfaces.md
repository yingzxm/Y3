# 接口签名（P0）

引擎：Unity 6.3 LTS **6000.3.26f1**（changeset `de8167e7440c`）。全员用 Hub 安装这一 patch。

程序集：`SocialEvolution.Core`、`SocialEvolution.Domain`、`SocialEvolution.World`、`SocialEvolution.Economy`、`SocialEvolution.Labor`、`SocialEvolution.Agents`、`SocialEvolution.Stats` 不引用 Unity。`SocialEvolution.Bootstrap` 引用这些程序集。`MonoBehaviour` 只出现在 `Bootstrap/Unity`（`SimulationDriver`、`StatsDebugPanel`、`MarketDebugPanel`）。Economy 引用 Domain、World、Core，不引用 Labor 或 Agents。Agents 另引用 Labor。Stats 不引用 Economy，中间价由调用方填进 `IMidPriceSource`。

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

`GameSession.CreateNew()` 会注册 `SimNoOp.OnGameHour`、`CityMarket.OnGameHour` 和 `SimNoOp.OnGameDay`。`SimNoOp` 仍是空的。市场小时钩子会生产并结算中间价。招聘和市民决策不会因此自动发生。

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

## 劳动力

程序集 `SocialEvolution.Labor`（`noEngineReferences: true`，C# 9），引用 Core、Domain、World。`JobPosting` 包装 Domain 编制：`Openings`、`WagePerHour`（`long`）、`RequiredSkillId`、`MinSkillLevel` 都读 Domain。工时、强度、班次、学历和年龄在 `JobConditions`，以 `JobPostingId` 为键。`FilledCount` 和雇佣记录只在 Labor。Domain 岗位没有 `VacantSlots`。

`GetVacantSlots` = max(0, 编制 − 在职)。门槛不够不占坑：编制 N、只有 M 名合格者应聘时，录用 M，空缺为 N−M。技能看 `IPersonReadModel.Skills` 里 `RequiredSkillId` 的等级，没有这项技能视为 0。标识只用 `SkillIds`。

招聘只有申请人 `TryApply`（合格且有空缺则当场入职）。没有雇主申请池，也没有同时接收市民和岗位并强制入职的派工 API。一个人同时只做一份工作，已入职再申请得到 `RejectedAlreadyEmployed`。辞职用 `TryQuit`，空缺回升。

求职评分是 `IJobSeekScorer` / `SimpleJobSeekScorer`。权重只来自注入的 `JobSeekScoreWeights`（`DemoDefaults` 为占位）。正式数值从 JSON 或 ScriptableObject 填进这个类型；本程序集不引用引擎。

求职和辞职若跟着时钟走，用 `LaborHourDriver.Register` 挂到 `ISimStepHooks.RegisterGameHour`（`ITimeService` 的小时钩子），每个游戏时一轮：先辞职，再求职。不按帧。`SimNoOp` 仍是空的，`GameSession.CreateNew` 不自动招聘。入职时间用 `GameDateTime`。

```csharp
public sealed class JobConditions
{
    public JobConditions(
        JobPostingId postingId,
        float workHours,
        float workIntensity,
        WorkShift shift,
        EducationLevel minEducation = EducationLevel.None,
        int minAge = 0,
        int? maxAge = null,
        int priority = 0);
    public JobPostingId PostingId { get; }
    public float WorkHours { get; }
    public float WorkIntensity { get; }
    public WorkShift Shift { get; }
    public EducationLevel MinEducation { get; }
    public int MinAge { get; }
    public int? MaxAge { get; }
    public int Priority { get; }
}

public enum WorkShift { Day = 0, Evening = 1, Night = 2, Flexible = 3 }

public sealed class JobPosting
{
    public JobPosting(Domain.JobPosting source, JobConditions conditions);
    public int Openings { get; }
    public long WagePerHour { get; }
    public string RequiredSkillId { get; }
    public int MinSkillLevel { get; }
    public float WorkHours { get; }
    public float WorkIntensity { get; }
    public WorkShift Shift { get; }
    public bool IsEligible(IPersonReadModel person);
}

public interface ILaborMarket
{
    void SetConditions(JobConditions conditions);
    bool TryGetPosting(JobPostingId postingId, out JobPosting? posting);
    IReadOnlyList<JobPosting> GetPostings();
    IReadOnlyList<JobPosting> GetOpenPostings();
    int GetFilledCount(JobPostingId postingId);
    int GetVacantSlots(JobPostingId postingId);
    IReadOnlyList<JobEmployment> GetEmployments(JobPostingId postingId);
    bool TryGetEmployment(CitizenId citizenId, out JobEmployment? employment);
    LaborApplyResult TryApply(IPersonReadModel applicant, JobPostingId postingId, GameDateTime now);
    bool TryQuit(IPersonReadModel applicant);
}

public enum LaborApplyResult
{
    Hired = 0,
    RejectedIneligible = 1,
    RejectedNoVacancy = 2,
    RejectedAlreadyEmployed = 3,
    PostingNotFound = 4,
}

public interface IJobSeekScorer
{
    float Score(IPersonReadModel person, JobPosting posting, JobSeekContext context);
}

public sealed class SimpleJobSeekScorer : IJobSeekScorer
{
    public SimpleJobSeekScorer(JobSeekScoreWeights weights);
}

public sealed class JobSeekScoreWeights
{
    public float Wage { get; set; }
    public float HoursPenalty { get; set; }
    public float CommutePenalty { get; set; }
    public float IntensityPenalty { get; set; }
    public float Safety { get; set; }
    public float Development { get; set; }
    public float Status { get; set; }
    public float SkillMatch { get; set; }
    public float Stability { get; set; }
    public float FullSkillMatch { get; set; }
    public static JobSeekScoreWeights DemoDefaults { get; }
}

public static class FatigueFormula
{
    // Fatigue + WorkHours × WorkIntensity × HealthFactor − Recovery
    public static float Compute(float currentFatigue, float workHours, float workIntensity, float healthFactor, float recovery);
}

public sealed class JobSeeker
{
    public JobSeeker(ILaborMarket market, IJobSeekScorer scorer);
    public LaborApplyResult TrySeekBest(IPersonReadModel person, GameDateTime now, JobSeekContext context, out JobPostingId? chosenPostingId);
}

public sealed class LaborHourDriver
{
    public LaborHourDriver(ILaborMarket market, IJobSeekScorer scorer);
    public void RequestApply(IPersonReadModel person, JobSeekContext context);
    public void RequestQuit(IPersonReadModel person);
    public void OnGameHour(GameDateTime now);
    public void Register(ISimStepHooks hooks);
    public void Unregister(ISimStepHooks hooks);
}
```

撤下编制后，下一次劳动查询会放开仍记在该岗位上的人。编制改到低于在职人数时，空缺为 0，已在职的人不被赶走。

## 代理人

程序集 `SocialEvolution.Agents`（`noEngineReferences: true`，C# 9），引用 Core、Domain、Labor。每个游戏时决策一次，不在渲染帧里跑，也不调用语言模型。

`AgentBrain.Tick` 先选 NeedScore 最高的一项需求，再执行对应的一个 `AgentAction`。并列时靠前的需求胜出：饥饿、疲劳、财富、安全。分数不大于 0 则闲置。饥饿永远映射成进食；繁荣指数不能把它关掉。

繁荣指数只软乘基础分。饥饿和疲劳在指数为 1 时保留 `ProsperityBasicNeedFloor`（默认 0.7，最低按 0.01）。财富和安全最多再乘 `1 + 0.15 × 指数`。人格三个标量（风险、工作积极性、储蓄）同样只改权重。在职、安全很急、而且风险偏好高时，安全需求会辞职；否则购物。

求职用 Labor 的 `JobSeeker`（注入的 `JobSeekScoreWeights`）再 `TryApply`。辞职用 `TryQuit`。工作按 `FatigueFormula.Compute` 累加疲劳，睡眠用同一公式扣恢复量。进食和购物只改满足度，不改 `long` 财富。没有派工 API。

`AgentsHourDriver.Register` 挂到和 `LaborHourDriver` 相同的 `ISimStepHooks.RegisterGameHour`。同一小时先辞职；有人辞职时，还没入职的人再决策一次，以便申请刚空出的岗位。每执行一个行动，往 `CitizenActionLog` 追加一行：求职、辞职、上班、进食、睡眠、购物、闲置都会记，同一小时每人一行。`GameSession.CreateNew` 不会自动注册它。场景接入见 [agents.md](agents.md)，点选和统计见 [stats.md](stats.md)。

```csharp
public enum NeedKind { Hunger = 0, Fatigue = 1, Wealth = 2, Safety = 3 }

public enum AgentAction
{
    None = 0,
    SeekJob = 1,
    Quit = 2,
    Work = 3,
    Eat = 4,
    Sleep = 5,
    Shop = 6,
    Idle = 7,
}

public sealed class AgentNeeds
{
    public const float DefaultMaxFatigue = 100f;
    public float HungerSatisfaction { get; set; }
    public float FatigueSatisfaction { get; set; }
    public float WealthSatisfaction { get; set; }
    public float SafetySatisfaction { get; set; }
    public float FatigueLoad { get; }
    public float GetUrgency(NeedKind kind);
    public float GetNeedScore(NeedKind kind, float baseScore);
    public void SyncFatigueFromLoad(float fatigueLoad, float maxFatigue = 100f);
}

public sealed class PersonalityScalars
{
    public PersonalityScalars(float riskPreference, float workDrive, float savingTendency);
    public float RiskPreference { get; set; }
    public float WorkDrive { get; set; }
    public float SavingTendency { get; set; }
}

public interface IDecisionMaker
{
    AgentAction Decide(in AgentDecisionContext context);
}

public sealed class MaxNeedDecisionMaker : IDecisionMaker
{
    public const float MinimumProsperityFloor = 0.01f;
    public float ProsperityBasicNeedFloor { get; set; }
    public AgentAction Decide(in AgentDecisionContext context);
    public NeedScores Evaluate(in AgentDecisionContext context);
}

public sealed class AgentBrain
{
    public AgentBrain(
        IPersonReadModel person,
        ILaborMarket laborMarket,
        IDecisionMaker? decisionMaker = null,
        AgentNeeds? needs = null,
        PersonalityScalars? personality = null,
        JobSeekScoreWeights? jobSeekWeights = null);
    public CitizenId CitizenId { get; }
    public AgentNeeds Needs { get; }
    public PersonalityScalars Personality { get; }
    public AgentAction LastAction { get; }
    public bool IsEmployed { get; }
    public void BindPerson(IPersonReadModel person);
    public AgentAction SelectAction(float? prosperityIndex = null);
    public void Execute(AgentAction action, GameDateTime now, JobSeekContext? seekContext = null);
    public AgentAction Tick(GameDateTime now, JobSeekContext? seekContext = null, float? prosperityIndex = null);
}

public sealed class AgentsHourDriver
{
    public AgentsHourDriver();
    public AgentsHourDriver(IEnumerable<AgentBrain> brains);
    public AgentsHourDriver(IEnumerable<AgentBrain> brains, CitizenActionLog log);
    public float? ProsperityIndex { get; set; }
    public JobSeekContext SeekContext { get; set; }
    public int HoursProcessed { get; }
    public CitizenActionLog Log { get; }
    public void Add(AgentBrain brain);
    public bool Remove(AgentBrain brain);
    public bool TryGetBrain(CitizenId citizenId, out AgentBrain? brain);
    public void OnGameHour(GameDateTime now);
    public void Register(ISimStepHooks hooks);
    public void Unregister(ISimStepHooks hooks);
}

public readonly struct CitizenLogLine
{
    public GameDateTime When { get; }
    public CitizenId CitizenId { get; }
    public AgentAction Action { get; }
    public string Text { get; }
}

public static class CitizenLogText
{
    public static string Format(GameDateTime when, AgentAction action);
    public static string Verb(AgentAction action);
}

public sealed class CitizenActionLog
{
    public const int MinimumCapacity = 3;
    public const int DefaultCapacity = 8;
    public CitizenActionLog(int capacityPerCitizen = 8);
    public int CapacityPerCitizen { get; }
    public void Append(CitizenId citizenId, GameDateTime when, AgentAction action, string text);
    public int Count(CitizenId citizenId);
    public IReadOnlyList<CitizenLogLine> GetRecent(CitizenId citizenId);
}
```

## 统计与点选

程序集 `SocialEvolution.Stats`（`noEngineReferences: true`，C# 9），引用 Core、Domain、World、Labor、Agents。不引用 Unity，也不引用 Economy 或 Bootstrap。中间价对齐 Economy 的 `ICityMarket.GetState(good).MidPrice`（`GoodMarketState.MidPrice`，`long`）。价格从 `IMidPriceSource` 读入。演示锚定价是食物 10、木材 8、工具 40。场景面板每个游戏时：

```csharp
readout.UseLivePrices(() => new GoodMidPrices(
    market.GetState(Good.Food).MidPrice,
    market.GetState(Good.Wood).MidPrice,
    market.GetState(Good.Tool).MidPrice));
```

就业率 = 在职人数 / 人口。平均时薪是在职市民 `WagePerHour` 的算术平均，没有在职时为 0。贫富指标是财富的基尼系数，范围 [0, 1]；财富有负数时先平移到最小值为 0。现金是 `Citizen.Wealth`。最急需求看 `AgentNeeds` 的紧急度，并列时饥饿、疲劳、财富、安全靠前的胜出；紧急度全是 0 则没有最急需求。

`DemoReadout.Attach` 给每个编制登记 8 小时白班（不改编制人数），为每位市民建 `AgentBrain`，并把 `AgentsHourDriver` 挂到时钟。`GameSession.CreateNew` 不注册这个驱动，但会注册市场小时钩子。`StatsDebugPanel` 在 `Attach` 之后调用 `GameSession.BindLabor`，产量改为按在职/编制缩放。场景面板见 [stats.md](stats.md)。同一小时先跑市场，再跑代理人。

```csharp
public interface IMidPriceSource
{
    bool TryGetMidPrice(Good good, out long midPrice);
}

public sealed class GoodMidPrices : IMidPriceSource
{
    public const long DemoFood = 10L;
    public const long DemoWood = 8L;
    public const long DemoTool = 40L;
    public GoodMidPrices(long food, long wood, long tool);
    public static GoodMidPrices DemoOpening();
}

public readonly struct CityStats
{
    public int Population { get; }
    public int EmployedCount { get; }
    public double EmploymentRate { get; }
    public double AverageWage { get; }
    public long FoodMidPrice { get; }
    public long WoodMidPrice { get; }
    public long ToolMidPrice { get; }
    public bool HasMidPrices { get; }
    public double RichPoorIndex { get; }
}

public static class CityStatsReader
{
    public static CityStats Read(WorldState world, ILaborMarket labor, IMidPriceSource prices);
}

public static class RichPoor
{
    public static double Compute(IReadOnlyList<Citizen> citizens);
}

public readonly struct CitizenView
{
    public CitizenId Id { get; }
    public string Name { get; }
    public long Cash { get; }
    public bool Employed { get; }
    public string JobTitle { get; }
    public long WagePerHour { get; }
    public bool HasTopNeed { get; }
    public NeedKind TopNeed { get; }
    public float TopNeedUrgency { get; }
    public string TopNeedLabel { get; }
    public IReadOnlyList<CitizenLogLine> RecentLogs { get; }
    public bool TryGetThreeRecentLogs(out string first, out string second, out string third);
}

public sealed class CitizenPicker
{
    public CitizenPicker(WorldState world, ILaborMarket labor, AgentsHourDriver agents);
    public CitizenId? SelectedId { get; }
    public bool Select(CitizenId id);
    public void Clear();
    public bool SelectNext();
    public bool SelectPrevious();
    public bool TryGetSelected(out CitizenView view);
    public bool TryRead(CitizenId id, out CitizenView view);
}

public sealed class DemoReadout
{
    public const float DemoShiftHours = 8f;
    public DemoReadout(WorldState world, ILaborMarket labor, AgentsHourDriver agents, IMidPriceSource prices);
    public static DemoReadout Attach(ITimeService time, WorldState world);
    public static DemoReadout Attach(ITimeService time, WorldState world, IMidPriceSource prices);
    public static void PublishDayShift(ILaborMarket labor, WorldState world);
    public void UseLivePrices(Func<IMidPriceSource> readEachTime);
    public CityStats Read();
    public bool Select(CitizenId id);
    public bool SelectNext();
    public bool SelectPrevious();
    public bool TryGetSelected(out CitizenView view);
    public bool TryGetThreeRecentLogs(out string first, out string second, out string third);
}
```

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
    public void BindLabor(ILaborMarket labor);
}
```

容量是一座建筑里全部商品的件数之和。失败的入出库不改数量，也不部分成交。`Inventory.TryTransfer` 同样整笔成功或双方都不变。

## 市场（P0-6）

程序集 `SocialEvolution.Economy`（`noEngineReferences: true`，C# 9）。引用 Domain、World、Core，不引用 Unity，也不引用 Labor、Agents、Stats。定价和生产不放进 World：World 只保管库存；市场要直接拿 `GameDateTime` 去挂小时钩子。到岗比例由 Bootstrap 的 `LaborOutputScale` 算好再交进来。

货币是 `long`。商品仍是 `Food`、`Wood`、`Tool`。没有私企，没有点名派工。金库可以为负，不结束游戏。政府买卖是正式入口。建设、设岗和财政面板仍属于 P0-7。`Citizen.Wealth` 仍是只读档案，购物不改它。

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
}

public sealed class TreasuryLedger : ICashLedger
{
    public TreasuryLedger(Treasury treasury);
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
    public bool ScalesWithStaffing { get; }
    public void BindOutputScale(Func<BuildingId, float>? scale);
}

public static class StateProduction
{
    public const int FarmFoodPerHour = 4;
    public const int FarmWoodPerHour = 1;
    public const int FactoryWoodInput = 1;
    public const int FactoryToolOutput = 1;
    public static void Run(WorldState world);
    public static void Run(WorldState world, Func<BuildingId, float>? outputScale);
    public static bool TryConvert(IInventory inventory, Good input, int inputQuantity, Good output, int outputQuantity);
}

public static class LaborOutputScale
{
    public static float ForBuilding(ILaborMarket labor, WorldState world, BuildingId buildingId);
}
```

开局中间价：食物 10（下限 1，上限 100，目标库存 200），木材 8（1–80，目标 80），工具 40（2–400，目标 40）。

买卖按当前中间价加减步长成交。步长是 `min(数量, MaxTradePriceStep)`，至少为 1。买入抬价，卖出压价，然后把 `MidPrice` 设成这笔成交价。现金和库存整笔成功或整笔不变。买方现金不够就拒绝；金库账户不拒绝，余额可以为负。集市建筑的库存是可买供给。清算账户起始 `InitialClearingCash`，允许为负，它不是金库。

每游戏时结算一次。这一小时有成交：`MidPrice` 保持最近成交价。没有成交：向库存压力价移动最多 `DriftStep`（1）。压力价是 `clamp(锚定价 * 目标库存 / max(集市库存, 1), 下限, 上限)`。集市库存高则向下，低则向上。

不绑定劳动力时，农场每小时产出 4 食物和 1 木材，工厂每小时消耗 1 木材、产出 1 工具。出库失败或入库失败则这一步不产，输入会退回。`BindLabor` / `BindOutputScale` 之后，产量按该建筑已登记岗位的在职人数 / 编制之和缩放并向下取整。没招到人则这一小时不产。没有已登记岗位的建筑仍按满产。

`GameSession.CreateNew()` 在小时钩子上注册 `market.OnGameHour`。回调顺序：生产 → 农场把超出 100 的食物以及木材、工厂把工具用 `TrySell` 卖进集市 → `SettlePrices`。`SimulationDriver.Awake` 调用 `CreateNew()`。`StatsDebugPanel.Start` 再注册代理人并 `BindLabor`。政府厅库存不会被这个钩子自动卖掉。

### Play 验证

1. 用 Unity Hub 打开本仓库根目录，版本 **6000.3.26f1**。
2. 打开 `Assets/Scenes/Bootstrap.unity`。物体 `Simulation` 上有 `SimulationDriver`、`StatsDebugPanel` 和 `MarketDebugPanel`。
3. 点 Play。左侧是统计和市民点选，宽屏时右侧是市场面板（窄屏时市场面板在统计下面）：日/时、暂停、1x、5x、金库、三种中间价、集市/政府/农场/工厂库存。
4. 点「暂停 Pause」。数量填 `10`，点食物「卖出 Sell」。中间价下降，集市食物增加，政府食物减少，金库增加。状态行写 `中间价 a → b`。
5. 点食物「买入 Buy」。中间价上升。金库可以买成负数，游戏不结束。
6. 数量 `0`，或集市没货时买入：失败，库存、金库和中间价都不变。
7. 「继续 Resume」再点 5x。统计面板已经绑定劳动力，没人上班的小时不生产，空集市会把中间价向压力价移动 1。有人入职后的下一小时按到岗比例生产，超出储备的产出卖进集市。

不打开 Unity 时：

```bash
dotnet test tests/SocialEvolution.Tests/SocialEvolution.Tests.csproj
```
