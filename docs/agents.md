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

`OnGameHour` 每执行一个行动，就给这位市民的 `CitizenActionLog` 追加一行。求职、辞职、上班、进食、睡眠、购物、闲置都会记。同一小时每人只有一行。默认留下最近 8 行，至少 3 行。点选和面板见 [stats.md](stats.md)。

如果同时还注册了 `LaborHourDriver`，不要为同一个人再 `RequestApply` / `RequestQuit`。Agents 自己会调用 `TryApply` 和 `TryQuit`。重复投递只会得到 `RejectedAlreadyEmployed`，或者把刚入职的人又辞掉。

求职权重从 `AgentBrain` 的构造函数注入 `JobSeekScoreWeights`。不传时用 `DemoDefaults`。

## 地图姿态（只读）

地图层不要再维护一份市民名单。`PersonMapPresence` 读的是已经在跑的 `AgentsHourDriver`。`GameSession.AttachMapPresence` 会订上这份驱动。轮询 `IPersonMapPresence.Poses` 或 `TryGet` 即可；要跟着游戏时刷新，先 `FollowHours`，再听 `Changed`。信号在每个游戏时、行动执行完之后发一次。开局时刻不发。不订阅也可以直接轮询。

`InBuilding` / `AgentBrain.IsInsideWorkplace` 只在「本小时行动是上班，并且劳动市场仍能解析到工作场所」时为 true。失业、求职、辞职、进食、睡眠（包括在家）、购物、闲置都是 false，人留在户外格上，地图应显示。进楼不改格子，只把人隐藏。

格子是演示用的。仓库里还没有 Construction / DemoMap 的格子。`PersonMapCells` 按 `CitizenId` 从城心 (16, 16) 附近铺开，没有随机数：市民 1 在 (10, 11)，每行 12 人，市民 120 在 (21, 20)。`DemoMapCellSpace` 换成世界坐标：地面 XZ，格大小 1，用格心。市民 1 的世界坐标是 (10.5, 0, 11.5)。

Unity 侧合同不要改名：`ICitizenMapPoseSource.TryGet(CitizenId id, out Vector3 world, out bool inBuilding)`。返回 true 表示这个市民有姿态；`inBuilding` 为 true 时隐藏精灵，为 false 时画在 `world`。`CitizenMapPoseSource` 做格子到世界的转换。`StatsDebugPanel.Start` 在 `DemoReadout.Attach` 之后调用 `CitizenMapPoseAutoAttach.Apply`，把姿态写到 `GameSession.MapPresence`，并赋给场景里每个 `CitizenMapVisualController.PoseSource`。适配器也留在 `CitizenMapPoseAutoAttach.Current`。控制器若来得更晚，`LateUpdate` 会在 `PoseSource` 仍为空时接上 `Current`。`PoseSource` 为空时地图层继续用自己的占位散布。这里不绑精灵。
