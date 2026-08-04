using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class WolfStatusModelTests
    {
        private const long Now = 1_700_000_000_000L;

        private static RolesClientState NewGaugeReceivedState(byte unlockedFlags,
            byte beaconCharges = 0, long beaconReadyUnixMs = 0)
        {
            var state = new RolesClientState();
            state.ApplyGaugeSync(0, unlockedFlags, beaconCharges, 0, beaconReadyUnixMs);
            return state;
        }

        private static WolfStatusState ComputePlay(WolfStatusModel model, RolesClientState roles,
            long now = Now)
            => model.Compute(roles, Role.Werewolf, GamePhase.Play, false, now);

        [Fact]
        public void Compute_人狼以外は非表示()
        {
            var model = new WolfStatusModel();
            RolesClientState roles = NewGaugeReceivedState((byte)PerkFlags.InfiniteStamina);
            Assert.False(model.Compute(roles, Role.Villager, GamePhase.Play, false, Now).Visible);
            Assert.False(model.Compute(roles, Role.BlackCat, GamePhase.Play, false, Now).Visible);
            Assert.False(model.Compute(roles, null, GamePhase.Play, false, Now).Visible);
        }

        [Fact]
        public void Compute_死亡ゲート済みロールnullは非表示()
        {
            var model = new WolfStatusModel();
            RolesClientState roles = NewGaugeReceivedState((byte)PerkFlags.InfiniteStamina);
            roles.TryToggleWolfMode(Role.Werewolf);
            Assert.False(model.Compute(roles, null, GamePhase.Play, false, Now).Visible);
        }

        [Fact]
        public void Compute_171未受信は非表示()
        {
            var model = new WolfStatusModel();
            Assert.False(ComputePlay(model, new RolesClientState()).Visible);
        }

        [Fact]
        public void Compute_セッション外フェーズは非表示()
        {
            var model = new WolfStatusModel();
            RolesClientState roles = NewGaugeReceivedState((byte)PerkFlags.InfiniteStamina);
            Assert.False(model.Compute(roles, Role.Werewolf, GamePhase.Lobby, false, Now).Visible);
            Assert.False(model.Compute(roles, Role.Werewolf, GamePhase.GameOver, false, Now).Visible);
        }

        [Fact]
        public void Compute_ワープ後会議中は非表示_ワープ前は表示()
        {
            var model = new WolfStatusModel();
            RolesClientState roles = NewGaugeReceivedState((byte)PerkFlags.InfiniteStamina);
            Assert.False(model.Compute(roles, Role.Werewolf, GamePhase.Meeting, true, Now).Visible);
            Assert.True(model.Compute(roles, Role.Werewolf, GamePhase.Meeting, false, Now).Visible);
        }

        [Fact]
        public void Compute_nullステートは非表示()
        {
            var model = new WolfStatusModel();
            Assert.False(model.Compute(null, Role.Werewolf, GamePhase.Play, false, Now).Visible);
        }

        [Fact]
        public void Compute_全特典未解禁は本体ごとLocked()
        {
            var model = new WolfStatusModel();
            RolesClientState roles = NewGaugeReceivedState((byte)PerkFlags.None);
            WolfStatusState s = ComputePlay(model, roles);

            Assert.True(s.Visible);
            Assert.Equal(WolfPerkVisual.Locked, s.Toggle);
            Assert.Equal(WolfPerkVisual.Locked, s.Stamina);
            Assert.Equal(WolfPerkVisual.Locked, s.Jump);
            Assert.Equal(WolfPerkVisual.Locked, s.EnemyIgnore);
        }

        [Fact]
        public void Compute_解禁済み狼化OFFはReady_未解禁はLockedのまま()
        {
            var model = new WolfStatusModel();
            RolesClientState roles = NewGaugeReceivedState((byte)PerkFlags.InfiniteStamina);
            WolfStatusState s = ComputePlay(model, roles);

            Assert.Equal(WolfPerkVisual.Ready, s.Toggle);
            Assert.Equal(WolfPerkVisual.Ready, s.Stamina);
            Assert.Equal(WolfPerkVisual.Locked, s.Jump);
            Assert.Equal(WolfPerkVisual.Locked, s.EnemyIgnore);
        }

        [Fact]
        public void Compute_狼化ONは解禁済みスロットのみActive()
        {
            var model = new WolfStatusModel();
            RolesClientState roles = NewGaugeReceivedState(
                (byte)(PerkFlags.InfiniteStamina | PerkFlags.InfiniteJump));
            Assert.True(roles.TryToggleWolfMode(Role.Werewolf));

            WolfStatusState s = ComputePlay(model, roles);
            Assert.Equal(WolfPerkVisual.Active, s.Toggle);
            Assert.Equal(WolfPerkVisual.Active, s.Stamina);
            Assert.Equal(WolfPerkVisual.Active, s.Jump);
            Assert.Equal(WolfPerkVisual.Locked, s.EnemyIgnore);
        }

        [Fact]
        public void Compute_狼化ON中の新規解禁は即時Active()
        {
            var model = new WolfStatusModel();
            RolesClientState roles = NewGaugeReceivedState((byte)PerkFlags.InfiniteStamina);
            Assert.True(roles.TryToggleWolfMode(Role.Werewolf));
            roles.ApplyGaugeSync(0,
                (byte)(PerkFlags.InfiniteStamina | PerkFlags.EnemyIgnore), 0, 0, 0);

            WolfStatusState s = ComputePlay(model, roles);
            Assert.Equal(WolfPerkVisual.Active, s.EnemyIgnore);
        }

        [Fact]
        public void Compute_リセットで非表示へ戻る()
        {
            var model = new WolfStatusModel();
            RolesClientState roles = NewGaugeReceivedState((byte)PerkFlags.InfiniteStamina);
            roles.TryToggleWolfMode(Role.Werewolf);
            roles.Reset();

            Assert.False(ComputePlay(model, roles).Visible);
        }

        [Fact]
        public void Compute_ジャンプ未解禁は残回数非表示()
        {
            var model = new WolfStatusModel();
            RolesClientState roles = NewGaugeReceivedState((byte)PerkFlags.InfiniteStamina);
            WolfStatusState s = model.Compute(roles, Role.Werewolf, GamePhase.Play, false, Now,
                extraJumpLimit: 3, jumpRefillsUsed: 0);

            Assert.Equal(-1, s.JumpCharges);
        }

        [Fact]
        public void Compute_ジャンプ解禁済みは残回数を表示()
        {
            var model = new WolfStatusModel();
            RolesClientState roles = NewGaugeReceivedState((byte)PerkFlags.InfiniteJump);

            Assert.Equal(3, model.Compute(roles, Role.Werewolf, GamePhase.Play, false, Now,
                extraJumpLimit: 3, jumpRefillsUsed: 0).JumpCharges);
            Assert.Equal(2, model.Compute(roles, Role.Werewolf, GamePhase.Play, false, Now,
                extraJumpLimit: 3, jumpRefillsUsed: 2, injectedJumpAvailable: true).JumpCharges);
        }

        [Fact]
        public void Compute_補充直後は未使用の1回分も残回数に含める()
        {
            var model = new WolfStatusModel();
            RolesClientState roles = NewGaugeReceivedState((byte)PerkFlags.InfiniteJump);

            Assert.Equal(3, model.Compute(roles, Role.Werewolf, GamePhase.Play, false, Now,
                extraJumpLimit: 3, jumpRefillsUsed: 1, injectedJumpAvailable: true).JumpCharges);

            Assert.Equal(1, model.Compute(roles, Role.Werewolf, GamePhase.Play, false, Now,
                extraJumpLimit: 3, jumpRefillsUsed: 3, injectedJumpAvailable: true).JumpCharges);
            Assert.Equal(0, model.Compute(roles, Role.Werewolf, GamePhase.Play, false, Now,
                extraJumpLimit: 3, jumpRefillsUsed: 3, injectedJumpAvailable: false).JumpCharges);
        }

        [Fact]
        public void Compute_ジャンプ消費しきりは0で下げ止め()
        {
            var model = new WolfStatusModel();
            RolesClientState roles = NewGaugeReceivedState((byte)PerkFlags.InfiniteJump);

            Assert.Equal(0, model.Compute(roles, Role.Werewolf, GamePhase.Play, false, Now,
                extraJumpLimit: 3, jumpRefillsUsed: 3).JumpCharges);
            Assert.Equal(0, model.Compute(roles, Role.Werewolf, GamePhase.Play, false, Now,
                extraJumpLimit: 2, jumpRefillsUsed: 3).JumpCharges);
        }

        [Fact]
        public void Compute_ジャンプ上限マイナス1は実質無限で非表示()
        {
            var model = new WolfStatusModel();
            RolesClientState roles = NewGaugeReceivedState((byte)PerkFlags.InfiniteJump);
            WolfStatusState s = model.Compute(roles, Role.Werewolf, GamePhase.Play, false, Now,
                extraJumpLimit: -1, jumpRefillsUsed: 5);

            Assert.Equal(-1, s.JumpCharges);
        }

        [Fact]
        public void Compute_非表示状態のジャンプ残回数は非表示値()
        {
            var model = new WolfStatusModel();
            Assert.Equal(-1, model.Compute(null, Role.Werewolf, GamePhase.Play, false, Now,
                extraJumpLimit: 3, jumpRefillsUsed: 0).JumpCharges);
        }

        [Fact]
        public void Compute_ビーコン残数ありで待ちなしはグレーなし()
        {
            var model = new WolfStatusModel();
            RolesClientState roles = NewGaugeReceivedState(0, beaconCharges: 2);
            WolfStatusState s = ComputePlay(model, roles);

            Assert.Equal(2, s.BeaconCharges);
            Assert.Equal(0, s.BeaconCooldownSec);
            Assert.Equal(0f, s.BeaconGrayFraction);
        }

        [Fact]
        public void Compute_ビーコン残数ゼロで待ちなしは全面グレー()
        {
            var model = new WolfStatusModel();
            RolesClientState roles = NewGaugeReceivedState(0, beaconCharges: 0);
            WolfStatusState s = ComputePlay(model, roles);

            Assert.Equal(0, s.BeaconCharges);
            Assert.Equal(0, s.BeaconCooldownSec);
            Assert.Equal(1f, s.BeaconGrayFraction);
        }

        [Fact]
        public void Compute_クールダウンは受信時刻アンカーで残割合が減っていく()
        {
            var model = new WolfStatusModel();
            RolesClientState roles = NewGaugeReceivedState(
                0, beaconCharges: 1, beaconReadyUnixMs: Now + 10_000);

            WolfStatusState atStart = ComputePlay(model, roles, Now);
            Assert.Equal(10, atStart.BeaconCooldownSec);
            Assert.Equal(1f, atStart.BeaconGrayFraction, 3);

            WolfStatusState atHalf = ComputePlay(model, roles, Now + 5_000);
            Assert.Equal(5, atHalf.BeaconCooldownSec);
            Assert.Equal(0.5f, atHalf.BeaconGrayFraction, 3);

            WolfStatusState atEnd = ComputePlay(model, roles, Now + 10_000);
            Assert.Equal(0, atEnd.BeaconCooldownSec);
            Assert.Equal(0f, atEnd.BeaconGrayFraction);
        }

        [Fact]
        public void Compute_残秒は切り上げ表示()
        {
            var model = new WolfStatusModel();
            RolesClientState roles = NewGaugeReceivedState(
                0, beaconCharges: 1, beaconReadyUnixMs: Now + 10_000);

            Assert.Equal(10, ComputePlay(model, roles, Now + 1).BeaconCooldownSec);
            Assert.Equal(1, ComputePlay(model, roles, Now + 9_500).BeaconCooldownSec);
        }

        [Fact]
        public void Compute_過去のready値は待ちなし扱い()
        {
            var model = new WolfStatusModel();
            RolesClientState roles = NewGaugeReceivedState(
                0, beaconCharges: 1, beaconReadyUnixMs: Now - 1);
            WolfStatusState s = ComputePlay(model, roles);

            Assert.Equal(0, s.BeaconCooldownSec);
            Assert.Equal(0f, s.BeaconGrayFraction);
        }

        [Fact]
        public void Compute_同一ready値の再受信でアンカーは動かない()
        {
            var model = new WolfStatusModel();
            RolesClientState roles = NewGaugeReceivedState(
                0, beaconCharges: 1, beaconReadyUnixMs: Now + 10_000);
            ComputePlay(model, roles, Now);

            roles.ApplyGaugeSync(0, 0, 1, 0, Now + 10_000);
            WolfStatusState s = ComputePlay(model, roles, Now + 7_500);
            Assert.Equal(0.25f, s.BeaconGrayFraction, 3);
        }

        [Fact]
        public void Compute_モデルResetで次のreadyを新規アンカー扱い()
        {
            var model = new WolfStatusModel();
            RolesClientState roles = NewGaugeReceivedState(
                0, beaconCharges: 1, beaconReadyUnixMs: Now + 10_000);
            ComputePlay(model, roles, Now);

            model.Reset();
            WolfStatusState s = ComputePlay(model, roles, Now + 5_000);
            Assert.Equal(1f, s.BeaconGrayFraction, 3);
        }
    }
}
