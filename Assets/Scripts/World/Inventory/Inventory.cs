#nullable enable
using System;
using System.Collections.Generic;
using SocialEvolution.Domain;

namespace SocialEvolution.World
{
    public sealed class Inventory : IInventory
    {
        private readonly Dictionary<Good, int> _quantities = new Dictionary<Good, int>
        {
            { Good.Food, 0 },
            { Good.Wood, 0 },
            { Good.Tool, 0 },
        };

        public Inventory(int capacity)
            : this(capacity, Array.Empty<(Good, int)>())
        {
        }

        public Inventory(int capacity, params (Good Good, int Quantity)[] openingStock)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "库存容量不能为负。");
            }

            Capacity = capacity;
            if (openingStock == null)
            {
                throw new ArgumentNullException(nameof(openingStock));
            }

            var seen = new HashSet<Good>();
            long total = 0;
            for (int i = 0; i < openingStock.Length; i++)
            {
                (Good good, int quantity) = openingStock[i];
                if (!IsKnown(good))
                {
                    throw new ArgumentOutOfRangeException(nameof(openingStock), good, "未知商品。");
                }

                if (quantity < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(openingStock), quantity, "开局库存不能为负。");
                }

                if (!seen.Add(good))
                {
                    throw new ArgumentException($"开局库存重复：{good}。", nameof(openingStock));
                }

                total += quantity;
                _quantities[good] = quantity;
            }

            if (total > capacity)
            {
                throw new ArgumentException("开局库存超过容量。", nameof(openingStock));
            }
        }

        public int Capacity { get; }

        public int TotalQuantity
        {
            get
            {
                long total = 0;
                foreach (KeyValuePair<Good, int> pair in _quantities)
                {
                    total += pair.Value;
                }

                return checked((int)total);
            }
        }

        public int GetQuantity(Good good)
        {
            if (!IsKnown(good))
            {
                throw new ArgumentOutOfRangeException(nameof(good), good, "未知商品。");
            }

            return _quantities[good];
        }

        public InventoryResult TryDeposit(Good good, int quantity)
        {
            if (!IsKnown(good))
            {
                return InventoryResult.Fail(InventoryRejection.UnknownGood);
            }

            if (quantity <= 0)
            {
                return InventoryResult.Fail(InventoryRejection.InvalidQuantity);
            }

            long nextGood = (long)_quantities[good] + quantity;
            long nextTotal = (long)TotalQuantity + quantity;
            if (nextGood > int.MaxValue || nextTotal > Capacity)
            {
                return InventoryResult.Fail(InventoryRejection.ExceedsCapacity);
            }

            _quantities[good] = (int)nextGood;
            return InventoryResult.Ok(quantity);
        }

        public InventoryResult TryWithdraw(Good good, int quantity)
        {
            if (!IsKnown(good))
            {
                return InventoryResult.Fail(InventoryRejection.UnknownGood);
            }

            if (quantity <= 0)
            {
                return InventoryResult.Fail(InventoryRejection.InvalidQuantity);
            }

            if (_quantities[good] < quantity)
            {
                return InventoryResult.Fail(InventoryRejection.InsufficientStock);
            }

            _quantities[good] -= quantity;
            return InventoryResult.Ok(quantity);
        }

        /// <summary>
        /// 把指定数量从 <paramref name="from"/> 整笔调到 <paramref name="to"/>。
        /// 任一步失败则双方数量都保持调用前的值。
        /// </summary>
        public static InventoryResult TryTransfer(IInventory from, IInventory to, Good good, int quantity)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            if (to == null)
            {
                throw new ArgumentNullException(nameof(to));
            }

            InventoryResult withdrawn = from.TryWithdraw(good, quantity);
            if (!withdrawn.Succeeded)
            {
                return withdrawn;
            }

            InventoryResult deposited = to.TryDeposit(good, quantity);
            if (deposited.Succeeded)
            {
                return deposited;
            }

            InventoryResult restored = from.TryDeposit(good, quantity);
            if (!restored.Succeeded)
            {
                throw new InvalidOperationException("调拨回滚失败，库存可能已不一致。");
            }

            return deposited;
        }

        internal static bool IsKnown(Good good) => good is Good.Food or Good.Wood or Good.Tool;
    }
}
