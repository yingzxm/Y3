#nullable enable

namespace SocialEvolution.Domain
{
    /// <summary>P0 技能编号。岗位的 <c>RequiredSkillId</c> 只用这些常量。</summary>
    public static class SkillIds
    {
        public const string Farming = "farming";

        public const string Manufacturing = "manufacturing";

        public const string Trade = "trade";

        public const string Administration = "administration";

        public static bool IsKnown(string skillId)
        {
            return skillId == Farming
                || skillId == Manufacturing
                || skillId == Trade
                || skillId == Administration;
        }
    }
}
