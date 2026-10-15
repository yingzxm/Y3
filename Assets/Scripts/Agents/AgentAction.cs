#nullable enable

namespace SocialEvolution.Agents
{
    /// <summary>
    /// 写死的行动。每个游戏时只执行其中一个，不调用语言模型。
    /// </summary>
    public enum AgentAction
    {
        /// <summary>还没有做过决策。</summary>
        None = 0,

        /// <summary>经 Labor 的求职评分后申请人 <c>TryApply</c>。</summary>
        SeekJob = 1,

        /// <summary>经 <c>ILaborMarket.TryQuit</c> 辞去自己的那一份工作。</summary>
        Quit = 2,

        /// <summary>上班。疲劳按 <c>FatigueFormula</c> 累加。</summary>
        Work = 3,

        /// <summary>进食，提高饥饿满足。</summary>
        Eat = 4,

        /// <summary>睡眠，按疲劳公式扣减负荷。</summary>
        Sleep = 5,

        /// <summary>购物，提高安全满足。财富结算留给市场。</summary>
        Shop = 6,

        /// <summary>没有更急的需求，或没有可做的事。</summary>
        Idle = 7,
    }
}
