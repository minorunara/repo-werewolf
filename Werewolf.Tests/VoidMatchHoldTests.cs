using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class VoidMatchHoldTests
    {
        private const float Step = 0.1f;

        private static VoidMatchHoldEvent Drive(VoidMatchHold hold, float seconds,
            bool held = true, bool available = true, bool cancel = false)
        {
            var first = VoidMatchHoldEvent.None;
            int steps = (int)(seconds / Step + 0.5f);
            for (int i = 0; i < steps; i++)
            {
                var e = hold.Tick(held, available, cancel, Step);
                if (e != VoidMatchHoldEvent.None && first == VoidMatchHoldEvent.None) first = e;
            }
            return first;
        }

        [Fact]
        public void 第1段の長押しで確認画面が開く()
        {
            var hold = new VoidMatchHold();

            Assert.Equal(VoidMatchHoldEvent.None, Drive(hold, VoidMatchHold.ArmSeconds - 0.2f));
            Assert.False(hold.Armed);

            Assert.Equal(VoidMatchHoldEvent.Armed, Drive(hold, 0.3f));
            Assert.True(hold.Armed);
            Assert.False(hold.Confirmed);
        }

        [Fact]
        public void 第1段の途中で離すと蓄積が消える()
        {
            var hold = new VoidMatchHold();

            Drive(hold, VoidMatchHold.ArmSeconds - 0.2f);
            hold.Tick(held: false, available: true, cancelRequested: false, deltaSeconds: Step);
            Assert.Equal(0f, hold.Ratio);
            Assert.False(hold.IsCharging);

            Assert.Equal(VoidMatchHoldEvent.None, Drive(hold, VoidMatchHold.ArmSeconds - 0.2f));
            Assert.False(hold.Armed);
        }

        [Fact]
        public void 押しっぱなしでは第2段へ進まない()
        {
            var hold = new VoidMatchHold();
            Assert.Equal(VoidMatchHoldEvent.Armed, Drive(hold, VoidMatchHold.ArmSeconds + 0.1f));

            Assert.Equal(VoidMatchHoldEvent.None,
                Drive(hold, VoidMatchHold.ConfirmSeconds + 2f));
            Assert.False(hold.Confirmed);
            Assert.True(hold.Armed);
        }

        [Fact]
        public void 一度離してから第2段の長押しで確定する()
        {
            var hold = new VoidMatchHold();
            Drive(hold, VoidMatchHold.ArmSeconds + 0.1f);

            hold.Tick(held: false, available: true, cancelRequested: false, deltaSeconds: Step);

            Assert.Equal(VoidMatchHoldEvent.None, Drive(hold, VoidMatchHold.ConfirmSeconds - 0.3f));
            Assert.False(hold.Confirmed);

            Assert.Equal(VoidMatchHoldEvent.Confirmed, Drive(hold, 0.4f));
            Assert.True(hold.Confirmed);
            Assert.Equal(1f, hold.Ratio);
        }

        [Fact]
        public void 確定後は再発火しない()
        {
            var hold = new VoidMatchHold();
            Drive(hold, VoidMatchHold.ArmSeconds + 0.1f);
            hold.Tick(false, true, false, Step);
            Assert.Equal(VoidMatchHoldEvent.Confirmed, Drive(hold, VoidMatchHold.ConfirmSeconds + 0.1f));

            hold.Tick(false, true, false, Step);
            Assert.Equal(VoidMatchHoldEvent.None, Drive(hold, VoidMatchHold.ConfirmSeconds + 1f));
        }

        [Fact]
        public void 取消入力で確認画面が閉じる()
        {
            var hold = new VoidMatchHold();
            Drive(hold, VoidMatchHold.ArmSeconds + 0.1f);

            Assert.Equal(VoidMatchHoldEvent.Cancelled,
                hold.Tick(held: false, available: true, cancelRequested: true, deltaSeconds: Step));
            Assert.False(hold.Armed);
        }

        [Fact]
        public void 放置すると確認画面が自動で閉じる()
        {
            var hold = new VoidMatchHold();
            Drive(hold, VoidMatchHold.ArmSeconds + 0.1f);

            Assert.Equal(VoidMatchHoldEvent.None,
                Drive(hold, VoidMatchHold.ArmedTimeoutSeconds - 0.3f, held: false));
            Assert.True(hold.Armed);

            Assert.Equal(VoidMatchHoldEvent.Cancelled, Drive(hold, 0.4f, held: false));
            Assert.False(hold.Armed);
        }

        [Fact]
        public void 第2段の長押し中は放置タイマーが進まない()
        {
            var hold = new VoidMatchHold();
            Drive(hold, VoidMatchHold.ArmSeconds + 0.1f);
            hold.Tick(false, true, false, Step);

            for (int i = 0; i < 6; i++)
            {
                Assert.Equal(VoidMatchHoldEvent.None, Drive(hold, 2f));
                hold.Tick(false, true, false, Step);
            }
            Assert.True(hold.Armed);
        }

        [Fact]
        public void 利用不可になると確認画面が閉じる()
        {
            var hold = new VoidMatchHold();
            Drive(hold, VoidMatchHold.ArmSeconds + 0.1f);

            Assert.Equal(VoidMatchHoldEvent.Cancelled,
                hold.Tick(held: true, available: false, cancelRequested: false, deltaSeconds: Step));
            Assert.False(hold.Armed);
        }

        [Fact]
        public void 未開封のまま利用不可になっても取消イベントは出ない()
        {
            var hold = new VoidMatchHold();
            Drive(hold, VoidMatchHold.ArmSeconds - 0.3f);

            Assert.Equal(VoidMatchHoldEvent.None,
                hold.Tick(held: true, available: false, cancelRequested: false, deltaSeconds: Step));
        }

        [Fact]
        public void Ratioは現在段の満了秒に対する比を返す()
        {
            var hold = new VoidMatchHold();

            Drive(hold, VoidMatchHold.ArmSeconds * 0.5f);
            Assert.InRange(hold.Ratio, 0.4f, 0.6f);

            Drive(hold, VoidMatchHold.ArmSeconds);
            hold.Tick(false, true, false, Step);
            Drive(hold, VoidMatchHold.ConfirmSeconds * 0.5f);
            Assert.InRange(hold.Ratio, 0.4f, 0.6f);
        }

        [Fact]
        public void Resetで初期状態へ戻る()
        {
            var hold = new VoidMatchHold();
            Drive(hold, VoidMatchHold.ArmSeconds + 0.1f);
            hold.Reset();

            Assert.False(hold.Armed);
            Assert.False(hold.Confirmed);
            Assert.False(hold.IsCharging);
            Assert.Equal(0f, hold.Ratio);
        }
    }
}
