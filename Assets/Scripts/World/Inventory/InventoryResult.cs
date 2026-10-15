#nullable enable
using System;

namespace SocialEvolution.World
{
    /// <summary>
    /// 入出库结果。失败时数量为 0，调用方的库存应保持原样。
    /// </summary>
    public readonly struct InventoryResult : IEquatable<InventoryResult>
    {
        private InventoryResult(bool succeeded, InventoryRejection rejection, int quantity)
        {
            Succeeded = succeeded;
            Rejection = rejection;
            Quantity = quantity;
        }

        public bool Succeeded { get; }

        public InventoryRejection Rejection { get; }

        public int Quantity { get; }

        public static InventoryResult Ok(int quantity)
        {
            if (quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "成功入出库的数量必须为正。");
            }

            return new InventoryResult(true, InventoryRejection.None, quantity);
        }

        public static InventoryResult Fail(InventoryRejection rejection)
        {
            if (rejection == InventoryRejection.None)
            {
                throw new ArgumentException("失败结果需要具体原因。", nameof(rejection));
            }

            return new InventoryResult(false, rejection, 0);
        }

        public bool Equals(InventoryResult other)
        {
            return Succeeded == other.Succeeded && Rejection == other.Rejection && Quantity == other.Quantity;
        }

        public override bool Equals(object? obj) => obj is InventoryResult other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Succeeded, Rejection, Quantity);

        public override string ToString() => Succeeded ? $"Ok({Quantity})" : Rejection.ToString();
    }
}
