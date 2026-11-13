#nullable enable
using System;
using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class TextRefreshTests : IDisposable
    {
        private readonly Language _before = Texts.Current;
        private readonly List<string> _logged = new List<string>();
        private readonly Action<string, bool>? _savedSink;

        public TextRefreshTests()
        {
            _savedSink = WLog.Sink;
            WLog.Sink = (line, _) => _logged.Add(line);
        }

        public void Dispose()
        {
            WLog.Sink = _savedSink;
            Texts.Apply(_before, null);
        }

        private static void RaiseChanged() => Texts.Apply(Texts.Current, null);

        [Fact]
        public void Subscribe_Twice_KeepsSingleSubscriptionAndUsesLatestHandlers()
        {
            var refresh = new TextRefresh("test");
            int first = 0, second = 0;
            try
            {
                refresh.Subscribe(() => true, () => first++);
                refresh.Subscribe(() => true, () => second++);

                RaiseChanged();

                Assert.Equal(0, first);
                Assert.Equal(1, second);
                Assert.True(refresh.IsSubscribed);
            }
            finally
            {
                refresh.Unsubscribe();
            }
        }

        [Fact]
        public void Subscribe_WithoutAliveCheck_AppliesOnEveryChange()
        {
            var refresh = new TextRefresh("test");
            int applied = 0;
            try
            {
                refresh.Subscribe(() => applied++);
                RaiseChanged();
                RaiseChanged();
                Assert.Equal(2, applied);
            }
            finally
            {
                refresh.Unsubscribe();
            }
        }

        [Fact]
        public void Changed_WhenNotAlive_SkipsApply()
        {
            var refresh = new TextRefresh("test");
            int applied = 0;
            try
            {
                refresh.Subscribe(() => false, () => applied++);
                RaiseChanged();
                Assert.Equal(0, applied);
            }
            finally
            {
                refresh.Unsubscribe();
            }
        }

        [Fact]
        public void Unsubscribe_StopsNotifications_AndIsIdempotent()
        {
            var refresh = new TextRefresh("test");
            int applied = 0;
            refresh.Subscribe(() => true, () => applied++);
            refresh.Unsubscribe();
            refresh.Unsubscribe();

            RaiseChanged();

            Assert.Equal(0, applied);
            Assert.False(refresh.IsSubscribed);
        }

        [Fact]
        public void ApplyThrows_IsLoggedWithOwner_AndDoesNotStopOtherSubscribers()
        {
            var failing = new TextRefresh("FailingPanel");
            var healthy = new TextRefresh("HealthyPanel");
            int healthyApplied = 0;
            try
            {
                failing.Subscribe(() => true, () => throw new InvalidOperationException("boom"));
                healthy.Subscribe(() => true, () => healthyApplied++);

                RaiseChanged();

                Assert.Equal(1, healthyApplied);
                Assert.Contains(_logged, line => line.Contains("panel_text_refresh_error") && line.Contains("FailingPanel"));
            }
            finally
            {
                failing.Unsubscribe();
                healthy.Unsubscribe();
            }
        }
    }
}
