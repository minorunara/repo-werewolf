using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class NoticeCatalogTests
    {
        [Fact]
        public void ConveneStarted_IncludesCallerName()
        {
            var notice = SessionNotice.ForConveneStarted("Alice");
            Assert.Equal("Aliceが緊急会議を招集しました", NoticeCatalog.Format(notice));
        }

        [Fact]
        public void NoExecution_ShowsFixedText()
        {
            var notice = SessionNotice.ForNoExecution();
            Assert.Equal("誰も処刑されませんでした", NoticeCatalog.Format(notice));
        }

        [Fact]
        public void Executed_IncludesActorName()
        {
            var notice = SessionNotice.ForExecuted("Bob");
            Assert.Equal("Bobが処刑されました", NoticeCatalog.Format(notice));
        }

        [Fact]
        public void BlackCatRevealed_IncludesActorName()
        {
            var notice = SessionNotice.ForBlackCatRevealed("Carol");
            Assert.Equal("Carolは黒猫でした", NoticeCatalog.Format(notice));
        }

        [Fact]
        public void CurseVictim_IncludesActorName()
        {
            var notice = SessionNotice.ForCurseVictim("Dave");
            Assert.Equal("Daveは道連れにされました", NoticeCatalog.Format(notice));
        }

        [Fact]
        public void CatAwakened_IsAnonymousFixedTemplate()
        {
            var notice = SessionNotice.ForCatAwakened();
            Assert.Equal("もし黒猫がいるなら、目覚めている頃です…", NoticeCatalog.Format(notice));
        }

        [Fact]
        public void PlayerDisconnected_IncludesActorName()
        {
            var notice = SessionNotice.ForPlayerDisconnected("Eve");
            Assert.Equal("Eveがゲームから切断されました", NoticeCatalog.Format(notice));
        }

        [Fact]
        public void ConveneHoldHint_ExplainsHoldOperation()
        {
            var notice = SessionNotice.ForConveneHoldHint();
            Assert.Equal("会議を招集するには、ボタンを長押ししてください",
                NoticeCatalog.Format(notice));
        }

        [Theory]
        [InlineData(ConveneRejectReason.NoRight)]
        [InlineData(ConveneRejectReason.Suppressed)]
        [InlineData(ConveneRejectReason.WrongPhase)]
        [InlineData(ConveneRejectReason.CallerDead)]
        [InlineData(ConveneRejectReason.AlreadyMeeting)]
        [InlineData(ConveneRejectReason.UnknownCaller)]
        public void ConveneDenied_AllReasons_ProduceNonEmptyDistinctFromNone(ConveneRejectReason reason)
        {
            var notice = SessionNotice.ForConveneDenied(reason);
            var text = NoticeCatalog.Format(notice);

            Assert.False(string.IsNullOrEmpty(text));
        }

        [Fact]
        public void ConveneDenied_None_IsNotATarget()
        {
            var notice = SessionNotice.ForConveneDenied(ConveneRejectReason.None);
            Assert.Null(NoticeCatalog.Format(notice));
        }

        [Fact]
        public void ConveneDenied_DistinctReasons_ProduceDistinctWording()
        {
            var noRight = NoticeCatalog.Format(SessionNotice.ForConveneDenied(ConveneRejectReason.NoRight));
            var suppressed = NoticeCatalog.Format(SessionNotice.ForConveneDenied(ConveneRejectReason.Suppressed));
            var wrongPhase = NoticeCatalog.Format(SessionNotice.ForConveneDenied(ConveneRejectReason.WrongPhase));

            Assert.NotEqual(noRight, suppressed);
            Assert.NotEqual(suppressed, wrongPhase);
            Assert.NotEqual(noRight, wrongPhase);
        }

        [Fact]
        public void Format_Null_ReturnsNull()
        {
            Assert.Null(NoticeCatalog.Format(null));
        }

        [Fact]
        public void AllTemplates_NeverContainRoleLabels()
        {
            var notices = new[]
            {
                SessionNotice.ForConveneStarted("Alice"),
                SessionNotice.ForNoExecution(),
                SessionNotice.ForExecuted("Bob"),
                SessionNotice.ForBlackCatRevealed("Carol"),
                SessionNotice.ForCurseVictim("Dave"),
                SessionNotice.ForConveneDenied(ConveneRejectReason.NoRight),
                SessionNotice.ForConveneDenied(ConveneRejectReason.Suppressed),
                SessionNotice.ForConveneDenied(ConveneRejectReason.WrongPhase),
                SessionNotice.ForCatAwakened(),
                SessionNotice.ForConveneHoldHint(),
            };

            foreach (var notice in notices)
            {
                var text = NoticeCatalog.Format(notice);
                Assert.NotNull(text);
                Assert.DoesNotContain("人狼です", text);
                Assert.DoesNotContain("村人です", text);
                Assert.DoesNotContain("票", text);
            }
        }
    }
}
