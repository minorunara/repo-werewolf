using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ResultReturnButtonFlowTests
    {
        private const long T0 = 1_000_000L;

        private static ResultReturnButtonFlow BeginFlow()
        {
            var flow = new ResultReturnButtonFlow();
            flow.Begin(T0);
            return flow;
        }

        private const long Ready = T0 + ResultReturnButtonFlow.ArmDelayMs;

        [Fact]
        public void フェード開始前はアルファ0で受付もしない()
        {
            var flow = BeginFlow();
            long justBeforeFade = T0 + ResultReturnButtonFlow.FadeStartMs - 1;

            Assert.Equal(0f, flow.AlphaAt(justBeforeFade));
            Assert.False(flow.ReadyAt(justBeforeFade));
            Assert.Equal(ResultReturnButtonEvent.None,
                flow.Tick(justBeforeFade, clicked: true, pointerOnButton: true));
            Assert.False(flow.Armed);
        }

        [Fact]
        public void フェード中はアルファが進むが受付はしない()
        {
            var flow = BeginFlow();
            long midFade = T0 + ResultReturnButtonFlow.FadeStartMs
                + ResultReturnButtonFlow.FadeDurationMs / 2;

            Assert.Equal(0.5f, flow.AlphaAt(midFade), 2);
            Assert.False(flow.ReadyAt(midFade));
            Assert.Equal(ResultReturnButtonEvent.None,
                flow.Tick(midFade, clicked: true, pointerOnButton: true));
        }

        [Fact]
        public void フェード完了でアルファ1かつ受付開始()
        {
            var flow = BeginFlow();

            Assert.False(flow.ReadyAt(Ready - 1));
            Assert.True(flow.ReadyAt(Ready));
            Assert.Equal(1f, flow.AlphaAt(Ready));
        }

        [Fact]
        public void 二度押しで確定する()
        {
            var flow = BeginFlow();

            Assert.Equal(ResultReturnButtonEvent.Armed,
                flow.Tick(Ready, clicked: true, pointerOnButton: true));
            Assert.True(flow.Armed);
            Assert.False(flow.Confirmed);

            Assert.Equal(ResultReturnButtonEvent.Confirmed,
                flow.Tick(Ready + 500, clicked: true, pointerOnButton: true));
            Assert.True(flow.Confirmed);
        }

        [Fact]
        public void 確認待ち中のボタン外クリックで取り消す()
        {
            var flow = BeginFlow();
            flow.Tick(Ready, clicked: true, pointerOnButton: true);

            Assert.Equal(ResultReturnButtonEvent.Disarmed,
                flow.Tick(Ready + 500, clicked: true, pointerOnButton: false));
            Assert.False(flow.Armed);

            Assert.Equal(ResultReturnButtonEvent.Armed,
                flow.Tick(Ready + 1000, clicked: true, pointerOnButton: true));
        }

        [Fact]
        public void 確認待ちでないボタン外クリックは何も起こさない()
        {
            var flow = BeginFlow();

            Assert.Equal(ResultReturnButtonEvent.None,
                flow.Tick(Ready, clicked: true, pointerOnButton: false));
            Assert.False(flow.Armed);
        }

        [Fact]
        public void クリックの無いフレームは状態を変えない()
        {
            var flow = BeginFlow();
            flow.Tick(Ready, clicked: true, pointerOnButton: true);

            Assert.Equal(ResultReturnButtonEvent.None,
                flow.Tick(Ready + 500, clicked: false, pointerOnButton: false));
            Assert.True(flow.Armed);
        }

        [Fact]
        public void 確定後は再クリックしても発火しない()
        {
            var flow = BeginFlow();
            flow.Tick(Ready, clicked: true, pointerOnButton: true);
            flow.Tick(Ready + 500, clicked: true, pointerOnButton: true);

            Assert.Equal(ResultReturnButtonEvent.None,
                flow.Tick(Ready + 1000, clicked: true, pointerOnButton: true));
            Assert.True(flow.Confirmed);
        }

        [Fact]
        public void 未開始とリセット後は不可視で受け付けない()
        {
            var flow = new ResultReturnButtonFlow();
            Assert.Equal(0f, flow.AlphaAt(Ready));
            Assert.False(flow.ReadyAt(Ready));
            Assert.Equal(ResultReturnButtonEvent.None,
                flow.Tick(Ready, clicked: true, pointerOnButton: true));

            flow.Begin(T0);
            flow.Tick(Ready, clicked: true, pointerOnButton: true);
            flow.Reset();
            Assert.False(flow.Armed);
            Assert.Equal(0f, flow.AlphaAt(Ready));
            Assert.Equal(ResultReturnButtonEvent.None,
                flow.Tick(Ready, clicked: true, pointerOnButton: true));
        }

        [Fact]
        public void 再Beginで確定状態が白紙化されタイムラインが引き直される()
        {
            var flow = BeginFlow();
            flow.Tick(Ready, clicked: true, pointerOnButton: true);
            flow.Tick(Ready + 500, clicked: true, pointerOnButton: true);
            Assert.True(flow.Confirmed);

            long t1 = Ready + 10_000;
            flow.Begin(t1);
            Assert.False(flow.Confirmed);
            Assert.Equal(0f, flow.AlphaAt(t1 + ResultReturnButtonFlow.FadeStartMs - 1));
            Assert.False(flow.ReadyAt(t1 + ResultReturnButtonFlow.ArmDelayMs - 1));
            Assert.Equal(ResultReturnButtonEvent.Armed,
                flow.Tick(t1 + ResultReturnButtonFlow.ArmDelayMs, clicked: true, pointerOnButton: true));
        }
    }
}
