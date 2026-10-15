# 代理人（P0-5）

`SocialEvolution.Agents` 不引用 Unity。决策是纯数值：每个游戏时看一次需求，选分数最高的那一项，执行一个行动。没有语言模型，也没有派工。

繁荣指数只软乘 NeedScore 的基础分。饥饿和疲劳有下限（默认保留 0.7，设置得更低时也至少保留 0.01），不会因为繁荣被关掉。

## 和时钟对齐

`AgentBrain.Tick` 对应一个游戏时，不要放进 `Update`。`AgentsHourDriver` 和 Labor 的 `LaborHourDriver` 用同一套小时钩子：`ITimeService` / `ISimStepHooks.RegisterGameHour`。`AdvanceRealtime` 和 `StepHours` 每进入一个游戏时回调一次。开局时刻本身不回调。

同一小时里，驱动先执行辞职。如果这一小时有人辞职，还没入职的人会再决策一次，这样可以申请刚空出来的岗位。已经在职、而且这次不辞职的人，沿用第一次的决策。每个人这一小时仍然只执行一个行动。

`GameSession.CreateNew()` 只挂 `SimNoOp`。不注册驱动时，播放场景仍然只走时钟，不会应聘、进食或改空缺。

## Bootstrap / SimulationDriver 怎么注册

P0-9 再做暂停、倍速和统计面板。现在只要在创建会话之后挂上小时驱动。`SimulationDriver` 还没有引用 Agents；接入时给 `SocialEvolution.Bootstrap` 加上对 `SocialEvolution.Labor` 和 `SocialEvolution.Agents` 的 asmdef 引用，并让 `ArchitectureGuardTests` 里 Bootstrap 的引用列表和 asmdef 一致。

```csharp
GameSession session = GameSession.CreateNew();
var market = new LaborMarket(session.World);
// 每个编制仍要 SetConditions，和 Labor 一样。Agents 不改编制。

var brains = new List<AgentBrain>();
foreach (Citizen citizen in session.World.Citizens)
{
    brains.Add(new AgentBrain(citizen, market));
}

var agents = new AgentsHourDriver(brains);
agents.ProsperityIndex = 0.4f; // 可空。只软改 NeedScore，不关门。
agents.Register(session.Time);

// SimulationDriver.Awake，在 Session = GameSession.CreateNew() 之后：
// agents.Register(Session.Time);
// Update 里继续只调用 AdvanceRealtime。不要在 Update 里 Tick。
```

直接跑逻辑、不进场景时，用 `session.Time.StepHours`。它不受暂停和倍率影响，每个游戏时仍会进 `OnGameHour`。

如果同时还注册了 `LaborHourDriver`，不要为同一个人再 `RequestApply` / `RequestQuit`。Agents 自己会调用 `TryApply` 和 `TryQuit`。重复投递只会得到 `RejectedAlreadyEmployed`，或者把刚入职的人又辞掉。

求职权重从 `AgentBrain` 的构造函数注入 `JobSeekScoreWeights`。不传时用 `DemoDefaults`。
