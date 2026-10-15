using System;
using SocialEvolution.Bootstrap;
using SocialEvolution.Core;
using SocialEvolution.Domain;
using Xunit;

namespace SocialEvolution.Tests
{
    public class TreasuryTests
    {
        [Fact]
        public void BalanceMayGoNegativeWithoutStoppingTheClock()
        {
            GameSession session = GameSession.CreateNew();
            session.World.Treasury.Debit(OpeningSeed.InitialTreasury + 1);

            Assert.Equal(-1L, session.World.Treasury.Balance);
            Assert.Equal(24, session.Time.StepHours(GameDateTime.HoursPerDay));
            Assert.Equal(2, session.Time.Day);
            Assert.Equal(0, session.Time.Hour);
            Assert.Equal(-1L, session.World.Treasury.Balance);
        }

        [Fact]
        public void CreditAndDebit_RejectNegativeAmounts()
        {
            var treasury = new Treasury(-20);
            Assert.Equal(-20L, treasury.Balance);

            Assert.Throws<ArgumentOutOfRangeException>(() => treasury.Credit(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => treasury.Debit(-1));
            Assert.Equal(-20L, treasury.Balance);

            treasury.Credit(5);
            treasury.Debit(2);
            Assert.Equal(-17L, treasury.Balance);
        }
    }
}
