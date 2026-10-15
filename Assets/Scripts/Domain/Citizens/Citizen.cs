#nullable enable
using System;

namespace SocialEvolution.Domain
{
    /// <summary>
    /// 市民身份。P0 只有编号和名字，没有职业、现金、需求分或对话。
    /// </summary>
    public sealed class Citizen
    {
        public Citizen(CitizenId id, string name)
        {
            if (id.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id), "市民编号必须为正整数。");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("市民名字不能为空。", nameof(name));
            }

            Id = id;
            Name = name.Trim();
        }

        public CitizenId Id { get; }

        public string Name { get; }
    }
}
