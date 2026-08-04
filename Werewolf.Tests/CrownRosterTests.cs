using System;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public sealed class CrownRosterTests : IDisposable
    {
        public CrownRosterTests()
        {
            CrownRoster.Clear();
        }

        public void Dispose()
        {
            CrownRoster.Clear();
        }

        [Fact]
        public void SetWinners_登録したactorだけが勝者判定される()
        {
            CrownRoster.SetWinners(new[] { 1, 3 });

            Assert.True(CrownRoster.HasWinners);
            Assert.Equal(2, CrownRoster.Count);
            Assert.True(CrownRoster.IsWinner(1));
            Assert.True(CrownRoster.IsWinner(3));
            Assert.False(CrownRoster.IsWinner(2));
        }

        [Fact]
        public void SetWinners_再登録は置換になり前回分が残留しない()
        {
            CrownRoster.SetWinners(new[] { 1, 2 });
            CrownRoster.SetWinners(new[] { 3 });

            Assert.Equal(1, CrownRoster.Count);
            Assert.False(CrownRoster.IsWinner(1));
            Assert.False(CrownRoster.IsWinner(2));
            Assert.True(CrownRoster.IsWinner(3));
        }

        [Fact]
        public void SetWinners_nullは空名簿への置換として扱う()
        {
            CrownRoster.SetWinners(new[] { 1 });
            CrownRoster.SetWinners(null);

            Assert.False(CrownRoster.HasWinners);
            Assert.False(CrownRoster.IsWinner(1));
        }

        [Fact]
        public void SetWinners_ボットの負数actorも登録自体は受け付ける()
        {
            CrownRoster.SetWinners(new[] { 1, -101 });

            Assert.True(CrownRoster.IsWinner(-101));
        }

        [Fact]
        public void Clear_全消去でHasWinnersが倒れる()
        {
            CrownRoster.SetWinners(new[] { 1, 2 });
            CrownRoster.Clear();

            Assert.False(CrownRoster.HasWinners);
            Assert.Equal(0, CrownRoster.Count);
            Assert.False(CrownRoster.IsWinner(1));
        }

        [Fact]
        public void Version_SetWinnersとClearのたびに増加する()
        {
            int before = CrownRoster.Version;
            CrownRoster.SetWinners(new[] { 1 });
            Assert.Equal(before + 1, CrownRoster.Version);
            CrownRoster.Clear();
            Assert.Equal(before + 2, CrownRoster.Version);
        }

        [Fact]
        public void 空名簿では誰も勝者判定されない()
        {
            Assert.False(CrownRoster.HasWinners);
            Assert.False(CrownRoster.IsWinner(0));
            Assert.False(CrownRoster.IsWinner(1));
        }
    }
}
