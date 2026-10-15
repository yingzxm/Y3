#nullable enable
using System;
using System.Collections.Generic;
using SocialEvolution.Core;
using SocialEvolution.Domain;
using SocialEvolution.World;

namespace SocialEvolution.Economy
{
    /// <summary>
    /// 简化市场：按中间价即时买卖，不撮合订单簿。
    /// 成交把中间价改成这笔成交价。每游戏时若无成交，中间价向库存压力价移动一步。
    /// 集市库存高则压力价偏低，库存低则偏高，并夹在上下限里。
    /// </summary>
    public sealed class CityMarket : ICityMarket
    {
        public const long InitialClearingCash = 2_000_000L;

        public const int MaxTradePriceStep = 5;

        public const int DriftStep = 1;

        /// <summary>与开局农场食物储备一致。超出的部分才卖进集市。</summary>
        public const int FarmFoodReserve = 100;

        public const int FarmWoodReserve = 0;

        public const int FactoryToolReserve = 0;

        public const long FoodMidPrice = 10L;

        public const long WoodMidPrice = 8L;

        public const long ToolMidPrice = 40L;

        private readonly WorldState _world;
        private readonly CashLedger _clearing;
        private readonly Dictionary<Good, GoodMarketState> _states;
        private readonly Good[] _order;
        private readonly Dictionary<int, CashLedger> _producerCash = new Dictionary<int, CashLedger>();

        public CityMarket(WorldState world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _clearing = new CashLedger(InitialClearingCash, allowNegative: true);
            _order = new[] { Good.Food, Good.Wood, Good.Tool };
            _states = new Dictionary<Good, GoodMarketState>
            {
                { Good.Food, new GoodMarketState(Good.Food, FoodMidPrice, 1L, 100L, 200) },
                { Good.Wood, new GoodMarketState(Good.Wood, WoodMidPrice, 1L, 80L, 80) },
                { Good.Tool, new GoodMarketState(Good.Tool, ToolMidPrice, 2L, 400L, 40) },
            };
        }

        public WorldState World => _world;

        public long ClearingCash => _clearing.Balance;

        public GameDateTime LastSettled { get; private set; }

        public IReadOnlyList<GoodMarketState> Goods
        {
            get
            {
                var copy = new GoodMarketState[_order.Length];
                for (int i = 0; i < _order.Length; i++)
                {
                    copy[i] = GetState(_order[i]);
                }

                return copy;
            }
        }

        public GoodMarketState GetState(Good good)
        {
            if (!_states.TryGetValue(good, out GoodMarketState? state) || state == null)
            {
                throw new ArgumentOutOfRangeException(nameof(good), good, "未知商品。");
            }

            state.InventoryPressure = state.TargetStock - PitQuantity(good);
            return state;
        }

        public MarketResult TryBuy(ICashLedger payer, IInventory destination, Good good, int quantity)
        {
            if (payer == null)
            {
                throw new ArgumentNullException(nameof(payer));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (!TryGetTradable(good, quantity, out GoodMarketState? state, out MarketResult rejected))
            {
                return rejected;
            }

            BuildingSite? pit = Find(BuildingKind.Market);
            if (pit == null)
            {
                return MarketResult.Fail(MarketRejection.NoMarket, good);
            }

            long unit = Quote(state!, buying: true, quantity);
            if (!TryCost(unit, quantity, out long cost))
            {
                return MarketResult.Fail(MarketRejection.InvalidQuantity, good);
            }

            if (!payer.TryDebit(cost))
            {
                return MarketResult.Fail(MarketRejection.InsufficientCash, good);
            }

            InventoryResult withdrawn = pit.Inventory.TryWithdraw(good, quantity);
            if (!withdrawn.Succeeded)
            {
                payer.Credit(cost);
                return MarketResult.Fail(Map(withdrawn.Rejection), good);
            }

            InventoryResult deposited = destination.TryDeposit(good, quantity);
            if (!deposited.Succeeded)
            {
                RestorePit(pit.Inventory, good, quantity);
                payer.Credit(cost);
                return MarketResult.Fail(Map(deposited.Rejection), good);
            }

            _clearing.Credit(cost);
            RecordTrade(state!, unit, quantity);
            return MarketResult.Ok(good, quantity, unit, cost);
        }

        public MarketResult TrySell(ICashLedger payee, IInventory source, Good good, int quantity)
        {
            if (payee == null)
            {
                throw new ArgumentNullException(nameof(payee));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (!TryGetTradable(good, quantity, out GoodMarketState? state, out MarketResult rejected))
            {
                return rejected;
            }

            BuildingSite? pit = Find(BuildingKind.Market);
            if (pit == null)
            {
                return MarketResult.Fail(MarketRejection.NoMarket, good);
            }

            long unit = Quote(state!, buying: false, quantity);
            if (!TryCost(unit, quantity, out long proceeds))
            {
                return MarketResult.Fail(MarketRejection.InvalidQuantity, good);
            }

            InventoryResult withdrawn = source.TryWithdraw(good, quantity);
            if (!withdrawn.Succeeded)
            {
                return MarketResult.Fail(Map(withdrawn.Rejection), good);
            }

            InventoryResult deposited = pit.Inventory.TryDeposit(good, quantity);
            if (!deposited.Succeeded)
            {
                RestoreSource(source, good, quantity);
                return MarketResult.Fail(Map(deposited.Rejection), good);
            }

            if (!_clearing.TryDebit(proceeds))
            {
                InventoryResult takeBack = pit.Inventory.TryWithdraw(good, quantity);
                if (!takeBack.Succeeded)
                {
                    throw new InvalidOperationException("市场回滚失败，库存可能已不一致。");
                }

                RestoreSource(source, good, quantity);
                return MarketResult.Fail(MarketRejection.InsufficientCash, good);
            }

            payee.Credit(proceeds);
            RecordTrade(state!, unit, quantity);
            return MarketResult.Ok(good, quantity, unit, proceeds);
        }

        public MarketResult TryGovernmentBuy(Good good, int quantity)
        {
            BuildingSite? government = Find(BuildingKind.Government);
            if (government == null)
            {
                return MarketResult.Fail(MarketRejection.NoGovernment, good);
            }

            return TryBuy(new TreasuryLedger(_world.Treasury), government.Inventory, good, quantity);
        }

        public MarketResult TryGovernmentSell(Good good, int quantity)
        {
            BuildingSite? government = Find(BuildingKind.Government);
            if (government == null)
            {
                return MarketResult.Fail(MarketRejection.NoGovernment, good);
            }

            return TrySell(new TreasuryLedger(_world.Treasury), government.Inventory, good, quantity);
        }

        public void RunProduction() => StateProduction.Run(_world);

        public void SettlePrices()
        {
            for (int i = 0; i < _order.Length; i++)
            {
                Good good = _order[i];
                GoodMarketState state = _states[good];
                int stock = PitQuantity(good);
                if (state.HourVolume > 0)
                {
                    state.MidPrice = Clamp(state.LastTradePrice, state);
                }
                else
                {
                    Drift(state, stock);
                }

                state.RecentVolume = state.HourVolume;
                state.HourVolume = 0;
                state.InventoryPressure = state.TargetStock - stock;
            }
        }

        /// <summary>
        /// 小时钩子。顺序：生产入库、国企卖出超出储备的产出、结算中间价。
        /// 由 <c>GameSession.CreateNew</c> 注册到 <see cref="ITimeService"/>。
        /// </summary>
        public void OnGameHour(GameDateTime now)
        {
            LastSettled = now;
            RunProduction();
            OfferSurplus();
            SettlePrices();
        }

        private void OfferSurplus()
        {
            IReadOnlyList<BuildingSite> sites = _world.Sites;
            for (int i = 0; i < sites.Count; i++)
            {
                BuildingSite site = sites[i];
                if (site.Building.Kind == BuildingKind.Farm)
                {
                    Offer(site, Good.Food, FarmFoodReserve);
                    Offer(site, Good.Wood, FarmWoodReserve);
                }
                else if (site.Building.Kind == BuildingKind.Factory)
                {
                    Offer(site, Good.Tool, FactoryToolReserve);
                }
            }
        }

        private void Offer(BuildingSite site, Good good, int reserve)
        {
            int quantity = site.Inventory.GetQuantity(good);
            int surplus = quantity - reserve;
            if (surplus <= 0)
            {
                return;
            }

            TrySell(ProducerCash(site.Id), site.Inventory, good, surplus);
        }

        private CashLedger ProducerCash(BuildingId id)
        {
            if (!_producerCash.TryGetValue(id.Value, out CashLedger? ledger) || ledger == null)
            {
                ledger = new CashLedger(0);
                _producerCash[id.Value] = ledger;
            }

            return ledger;
        }

        private bool TryGetTradable(Good good, int quantity, out GoodMarketState? state, out MarketResult rejected)
        {
            if (!_states.TryGetValue(good, out state) || state == null)
            {
                rejected = MarketResult.Fail(MarketRejection.UnknownGood, good);
                return false;
            }

            if (quantity <= 0)
            {
                rejected = MarketResult.Fail(MarketRejection.InvalidQuantity, good);
                return false;
            }

            rejected = default;
            return true;
        }

        private static bool TryCost(long unit, int quantity, out long cost)
        {
            try
            {
                cost = checked(unit * quantity);
                return cost > 0;
            }
            catch (OverflowException)
            {
                cost = 0;
                return false;
            }
        }

        private static long Quote(GoodMarketState state, bool buying, int quantity)
        {
            int step = quantity;
            if (step > MaxTradePriceStep)
            {
                step = MaxTradePriceStep;
            }

            if (step < 1)
            {
                step = 1;
            }

            long price = buying ? state.MidPrice + step : state.MidPrice - step;
            return Clamp(price, state);
        }

        private static long Clamp(long price, GoodMarketState state)
        {
            if (price < state.FloorPrice)
            {
                return state.FloorPrice;
            }

            if (price > state.CeilingPrice)
            {
                return state.CeilingPrice;
            }

            return price;
        }

        private void RecordTrade(GoodMarketState state, long unitPrice, int quantity)
        {
            state.LastTradePrice = unitPrice;
            state.MidPrice = unitPrice;
            state.HourVolume += quantity;
            state.RecentVolume = state.HourVolume;
            state.InventoryPressure = state.TargetStock - PitQuantity(state.Good);
        }

        private void Drift(GoodMarketState state, int stock)
        {
            int divisor = stock > 0 ? stock : 1;
            long raw;
            try
            {
                raw = checked(state.AnchorPrice * state.TargetStock / divisor);
            }
            catch (OverflowException)
            {
                raw = state.CeilingPrice;
            }

            raw = Clamp(raw, state);
            if (state.MidPrice < raw)
            {
                long next = state.MidPrice + DriftStep;
                state.MidPrice = next > raw ? raw : next;
            }
            else if (state.MidPrice > raw)
            {
                long next = state.MidPrice - DriftStep;
                state.MidPrice = next < raw ? raw : next;
            }
        }

        private int PitQuantity(Good good)
        {
            BuildingSite? pit = Find(BuildingKind.Market);
            if (pit == null)
            {
                return 0;
            }

            return pit.Inventory.GetQuantity(good);
        }

        private BuildingSite? Find(BuildingKind kind)
        {
            IReadOnlyList<BuildingSite> sites = _world.Sites;
            for (int i = 0; i < sites.Count; i++)
            {
                if (sites[i].Building.Kind == kind)
                {
                    return sites[i];
                }
            }

            return null;
        }

        private static void RestorePit(IInventory pit, Good good, int quantity)
        {
            InventoryResult restored = pit.TryDeposit(good, quantity);
            if (!restored.Succeeded)
            {
                throw new InvalidOperationException("市场回滚失败，库存可能已不一致。");
            }
        }

        private static void RestoreSource(IInventory source, Good good, int quantity)
        {
            InventoryResult restored = source.TryDeposit(good, quantity);
            if (!restored.Succeeded)
            {
                throw new InvalidOperationException("市场回滚失败，库存可能已不一致。");
            }
        }

        private static MarketRejection Map(InventoryRejection rejection)
        {
            switch (rejection)
            {
                case InventoryRejection.InvalidQuantity:
                    return MarketRejection.InvalidQuantity;
                case InventoryRejection.ExceedsCapacity:
                    return MarketRejection.ExceedsCapacity;
                case InventoryRejection.InsufficientStock:
                    return MarketRejection.InsufficientStock;
                case InventoryRejection.UnknownGood:
                    return MarketRejection.UnknownGood;
                default:
                    return MarketRejection.InvalidQuantity;
            }
        }
    }
}
