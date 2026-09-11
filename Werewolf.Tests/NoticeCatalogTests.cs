using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class NoticeCatalogTests
    {
        [Fact]
        public void NamedNotices_EmbedTheActorName()
        {
            var texts = new[]
            {
                NoticeCatalog.Format(SessionNotice.ForConveneStarted("Alice")),
                NoticeCatalog.Format(SessionNotice.ForExecuted("Alice")),
                NoticeCatalog.Format(SessionNotice.ForBlackCatRevealed("Alice")),
                NoticeCatalog.Format(SessionNotice.ForCurseVictim("Alice")),
                NoticeCatalog.Format(SessionNotice.ForPlayerDisconnected("Alice")),
            };

            foreach (var text in texts)
            {
                Assert.False(string.IsNullOrEmpty(text));
                Assert.Contains("Alice", text);
            }

            Assert.Equal(texts.Length, texts.Distinct().Count());
        }

        [Fact]
        public void FixedNotices_AreNonEmptyAndCarryNoNameSlot()
        {
            var texts = new[]
            {
                NoticeCatalog.Format(SessionNotice.ForNoExecution()),
                NoticeCatalog.Format(SessionNotice.ForCatAwakened()),
                NoticeCatalog.Format(SessionNotice.ForConveneHoldHint()),
            };

            foreach (var text in texts)
            {
                Assert.False(string.IsNullOrEmpty(text));
                Assert.DoesNotContain("{0}", text);
            }

            Assert.Equal(texts.Length, texts.Distinct().Count());
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
