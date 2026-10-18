# 统计与市民点选（P0-8）

`SocialEvolution.Stats` 不引用 Unity，也不引用 Economy。人口、就业、时薪和贫富从 World / Labor / Agents 读。三种商品的中间价从 `IMidPriceSource` 读，对齐市场的 `ICityMarket.GetState(good).MidPrice`（`long`）。开局锚定价是食物 10、木材 8、工具 40。`StatsDebugPanel` 在挂上代理人之后调用 `UseLivePrices`，之后面板读的是市场现价。

贫富指标是财富的基尼系数，0 是均等，1 是极分化。现金是 `Citizen.Wealth`。最急需求是紧急度最高的那一项；并列时饥饿、疲劳、财富、安全靠前的胜出。四项紧急度都是 0 时，面板写「最急需求 无」。

## 场景里怎么看

1. 用 Unity Hub 打开仓库根目录，版本 **6000.3.26f1**。
2. 打开 `Assets/Scenes/Bootstrap.unity` 并 Play。
3. `SimulationDriver.Awake` 创建会话，只推进时钟。`StatsDebugPanel.Start` 调用 `DemoReadout.Attach(session.Time, session.World)`。
4. Game 视图左上角：

   - 人口、就业率、平均时薪
   - 食物、木材、工具的中间价
   - 贫富指标
   - 当前市民的现金、工作、最急需求
   - 「最近行动」下列出日志

5. 「上一位」「下一位」在 120 名市民里循环。也可以在文本框填编号（1–120）再点「选中」。
6. 默认 1 现实秒 = 1 游戏时。走满 3 个游戏时后，当前市民至少有 3 行日志，例如 `D1 01:00 购物`。行从旧到新。日志默认留 8 行。

没有相机也能看到这层面板，它是 `OnGUI`。完整皮肤是 P0-9。

若缺脚本，`StatsDebugPanel.cs.meta` 的 guid 是 `367e710eec604fccbc0348c05f47b95e`。确认它和 `SimulationDriver` 挂在同一个 `Simulation` 物体上。

## 场景脚本怎么取 3 行

`StatsDebugPanel` 上的方法可以直接被别的脚本调用：

```csharp
StatsDebugPanel panel = simulation.GetComponent<StatsDebugPanel>();
panel.SelectCitizen(12);
bool ready = panel.TryGetThreeRecentLogs(out string first, out string second, out string third);
```

`ready` 为 true 时，`first`、`second`、`third` 是最近三行，从旧到新。不足三行时返回 false。

不经过 `MonoBehaviour` 时，用同一个读面：

```csharp
GameSession session = GameSession.CreateNew();
DemoReadout readout = DemoReadout.Attach(session.Time, session.World);
readout.Select(new CitizenId(1));
session.Time.StepHours(3);
bool ready = readout.TryGetThreeRecentLogs(out string first, out string second, out string third);
```

`Attach` 会做三件事：给每个编制登记 8 小时白班（不改编制人数）、为每位市民建 `AgentBrain`、把 `AgentsHourDriver` 注册到时钟。每个游戏时，驱动把实际执行的行动写进 `CitizenActionLog`：求职、辞职、上班、进食、睡眠、购物、闲置。同一小时每人一行。`GameSession.CreateNew` 自己不会注册这个驱动；它会注册市场小时钩子。拆掉统计面板之后，Play 仍会生产和调价，但不会应聘。

市场小时钩子先于代理人执行：先生产、卖出超额、结算中间价，然后市民才行动。这一小时的入职要到下一小时才计入产量。

## 中间价改成市场现价

统计签名不变。`StatsDebugPanel.Start` 在 `Attach` 之后做两件事：`session.BindLabor(readout.Labor)`，以及：

```csharp
readout.UseLivePrices(() => new GoodMidPrices(
    market.GetState(Good.Food).MidPrice,
    market.GetState(Good.Wood).MidPrice,
    market.GetState(Good.Tool).MidPrice));
```

绑定之后，农场和工厂的产量按该建筑已登记岗位的在职人数 / 编制缩放，向下取整。开局没有在职，所以第一个游戏时不产，空集市会把中间价向压力价移动 1。有人上班之后，下一小时才按比例生产。不调用 `BindLabor` 时仍是满产。
