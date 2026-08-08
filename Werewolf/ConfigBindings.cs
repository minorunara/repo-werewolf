using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf
{
    internal sealed class ConfigBindings
    {
        private const string SecGeneral = "01. General";
        private const string SecMeeting = "02. Meeting";
        private const string SecRoleAssignment = "03. Role Assignment";
        private const string SecShaman = "04. Shaman";
        private const string SecWerewolf = "05. Werewolf";
        private const string SecBlackCat = "06. Black Cat";
        private const string SecBomber = "07. Bomber";
        internal const string SecStage = "08. Stage";
        private const string SecPlayerUpgrades = "09. Player Upgrades";
        internal const string SecLoadoutItems = "10. Loadout Items";
        private const string SecClientKeybinds = "Client Keybinds";
        private const string SecClientUi = "Client UI";
        private const string SecStreamer = "Streamer";
        private const string SecTutorial = "Tutorial";
        private const string SecVoice = "Voice";
        private const string SecDebug = "Debug";

        internal ConfigEntry<int> WerewolfCount;
        internal ConfigEntry<int> BlackCatChancePercent;
        internal ConfigEntry<int> BomberChancePercent;
        internal ConfigEntry<int> RoundSeconds;
        internal ConfigEntry<int> BlackCatRevealDelaySec;
        internal ConfigEntry<bool> BlackCatCurseEnabled;
        internal ConfigEntry<bool> DebugMode;

        internal ConfigEntry<int> ShamanChancePercent;
        internal ConfigEntry<int> ShamanGazeFullSec;
        internal ConfigEntry<int> ShamanGhostCooldownSec;
        internal ConfigEntry<int> ShamanStormWeakMeters;
        internal ConfigEntry<int> ShamanStormMediumMeters;
        internal ConfigEntry<int> ShamanStormStrongMeters;

        internal ConfigEntry<int> BomberProximityMeters;
        internal ConfigEntry<int> BomberGaugeFullSec;
        internal ConfigEntry<int> BomberInitialCooldownSec;
        internal ConfigEntry<int> BomberCooldownSec;
        internal ConfigEntry<int> BomberBlastRadiusMeters;
        internal ConfigEntry<int> BomberBlastPlayerDamage;
        internal ConfigEntry<int> BomberBlastEnemyDamage;
        internal ConfigEntry<int> BomberAmmoRefillPct;
        internal ConfigEntry<KeyCode> BomberPlantKey;
        internal ConfigEntry<KeyCode> BomberDetonateKey;

        internal ConfigEntry<string> Language;

        internal ConfigEntry<int> MeetingRightsPerPlayer;
        internal ConfigEntry<int> ConveneSuppressStartSec;
        internal ConfigEntry<int> ConveneSuppressAfterSec;
        internal ConfigEntry<int> MeetingCountdownSec;
        internal ConfigEntry<int> MeetingDurationSec;
        internal ConfigEntry<bool> VoteTimeCutEnabled;
        internal ConfigEntry<int> ResultDisplaySec;
        internal ConfigEntry<bool> MeetingScatterEnabled;
        internal ConfigEntry<int> ScatterGuardSec;
        internal ConfigEntry<float> ButtonOffsetX;
        internal ConfigEntry<float> ButtonOffsetY;
        internal ConfigEntry<float> ButtonOffsetZ;
        internal ConfigEntry<float> ButtonYaw;
        internal ConfigEntry<float> ButtonPitch;

        internal ConfigEntry<int> StaminaUnlockPct;
        internal ConfigEntry<int> JumpUnlockPct;
        internal ConfigEntry<int> EnemyIgnoreUnlockPct;
        internal ConfigEntry<int> HealUnlockPct;
        internal ConfigEntry<int> HealIntervalSec;
        internal ConfigEntry<int> BeaconChargePct;
        internal ConfigEntry<int> InformantThresholdPct;
        internal ConfigEntry<int> ExtraJumpCount;
        internal ConfigEntry<int> BeaconCooldownSec;
        internal ConfigEntry<int> BeaconSuppressStartSec;
        internal ConfigEntry<int> BeaconSuppressAfterMeetingSec;
        internal ConfigEntry<int> CatGaugeSyncIntervalSec;
        internal ConfigEntry<bool> OrbGaugeEnabled;
        internal ConfigEntry<KeyCode> WolfModeKey;
        internal ConfigEntry<KeyCode> BeaconKey;

        internal ConfigEntry<bool> WerewolfModeEnabled;
        internal ConfigEntry<bool> MinimapHideEnabled;
        internal ConfigEntry<bool> OutfitChangeAllowed;
        internal ConfigEntry<int> ValuableMapMode;
        internal ConfigEntry<int> GameOverAutoReturnSec;
        internal ConfigEntry<int> ToastDurationSec;

        internal ConfigEntry<KeyCode> LobbySettingsPanelKey;

        internal ConfigEntry<KeyCode> ManualKey;

        internal ConfigEntry<KeyCode> CorpseReportKey;

        internal ConfigEntry<KeyCode> ResultReturnKey;

        internal ConfigEntry<KeyCode> VoidMatchKey;

        internal ConfigEntry<KeyCode> MeetingMapKey;
        internal ConfigEntry<KeyCode> MeetingChatLogKey;
        internal ConfigEntry<float> MeetingMapOrthoSize;
        internal ConfigEntry<int> MeetingMapResolution;
        internal ConfigEntry<bool> MeetingMapGrid;
        internal ConfigEntry<bool> MeetingChatLog;

        internal ConfigEntry<int> NecroVoiceMode;

        internal ConfigEntry<float> NecroVoiceVolume;
        internal ConfigEntry<float> NecroVoiceLowPassCutoffHz;
        internal ConfigEntry<float> NecroVoiceEchoDelayMs;
        internal ConfigEntry<float> NecroVoiceEchoDecay;
        internal ConfigEntry<float> NecroVoiceReverbRoom;
        internal ConfigEntry<float> NecroVoiceReverbRoomHF;
        internal ConfigEntry<float> NecroVoiceReverbDecayTime;
        internal ConfigEntry<float> NecroVoiceReverbDecayHFRatio;
        internal ConfigEntry<float> NecroVoiceReverbReflections;
        internal ConfigEntry<float> NecroVoiceReverbReflectionsDelay;
        internal ConfigEntry<float> NecroVoiceReverbLevel;
        internal ConfigEntry<float> NecroVoiceReverbDelay;
        internal ConfigEntry<float> NecroVoiceReverbDiffusion;
        internal ConfigEntry<float> NecroVoiceReverbDensity;
        internal ConfigEntry<float> NecroVoiceReverbHFReference;

        private Dictionary<TutorialId, ConfigEntry<bool>> _tutorialSeen;
        internal ConfigEntry<bool> ResetTutorials;
        internal ConfigEntry<float> TutorialFontScale;

        internal ConfigEntry<bool> StreamerSafeMode;

        internal ConfigEntry<float> CursorMirrorScale;

        internal ConfigEntry<int> HudOffsetX;
        internal ConfigEntry<int> HudOffsetY;

        internal ConfigEntry<int> StartLevelNumber;
        internal ConfigEntry<string> StartMapName;
        internal ConfigEntry<int> StartEnergyPct;
        internal ConfigEntry<int> OrbDropMax;

        private Dictionary<string, ConfigEntry<int>> _startUpgrades;

        internal Func<string> ItemsSpecProvider { get; set; }

        private readonly Werewolf.Core.ValuableMapMode _valuableMapModeDefault;

        private readonly Werewolf.Core.NecroVoiceMode _necroVoiceModeDefault;

        internal ConfigBindings(ConfigFile config)
        {
            var defaults = new GameConfig();
            _valuableMapModeDefault = defaults.ValuableMapMode;
            _necroVoiceModeDefault = defaults.NecroVoiceMode;

            BindGeneral(config, defaults);
            BindMeeting(config, defaults);
            BindRoleAssignment(config, defaults);
            BindShaman(config, defaults);
            BindWerewolf(config, defaults);
            BindBlackCat(config, defaults);
            BindBomber(config, defaults);
            BindStage(config, defaults);
            BindPlayerUpgrades(config, defaults);
            BindClientKeybinds(config, defaults);
            BindClientUi(config, defaults);
            BindStreamer(config, defaults);
            BindTutorial(config, defaults);
            BindVoice(config, defaults);
            BindDebug(config, defaults);
        }

        private void BindGeneral(ConfigFile config, GameConfig defaults)
        {

            WerewolfModeEnabled = config.Bind(
                SecGeneral, "WerewolfModeEnabled", true,
                "Werewolf mode ON/OFF. While true, rounds are played as werewolf sessions. " +
                "Host authoritative: only the host's value applies to the room / " +
                "人狼モードのON/OFF。true の間はラウンドが人狼セッションとして進行する。" +
                "ホスト権威（部屋に効くのはホストの値のみ。参加者側の値は自分がホストのときにだけ効く）");

            Language = config.Bind(
                SecGeneral, "Language (restart required)", "English",
                new ConfigDescription(
                    "Display language (restart required). 日本語 = built-in Japanese, English = built-in English, " +
                    "custom = load Lang/custom.txt next to the DLL. A matching Lang file (Lang/en.txt for English, " +
                    "Lang/custom.txt for custom), if present, overrides the built-in text per key " +
                    "(missing keys fall back to the built-in table) / " +
                    "表示言語（反映には再起動が必要）。日本語=埋め込み日本語, English=埋め込み英語, custom=DLL同階層の Lang/custom.txt。" +
                    "対応する Lang ファイル（English は Lang/en.txt）があればキー単位で埋め込み文言を上書きする（欠落キーは埋め込みへフォールバック）",
                    new AcceptableValueList<string>("日本語", "English", "custom")));

            RoundSeconds = config.Bind(
                SecGeneral, "RoundSeconds", defaults.RoundSeconds,
                new ConfigDescription(
                    "Round time limit (seconds) / ラウンド制限時間（秒）",
                    new AcceptableValueRange<int>(180, 3600)));

            MinimapHideEnabled = config.Bind(
                SecGeneral, "MinimapHideEnabled", defaults.MinimapHideEnabled,
                "Hide valuables and corpses from the minimap / 貴重品・死体のミニマップ非表示設定");

            OutfitChangeAllowed = config.Bind(
                SecGeneral, "OutfitChangeAllowed", defaults.OutfitChangeAllowed,
                "Allow appearance (cosmetic) changes during a match (impersonation play) / " +
                "試合中の見た目（コスメ）変更を許可する（着替えなりすましを遊びとして認める）");

            ValuableMapMode = config.Bind(
                SecGeneral, "ValuableMapMode", (int)defaults.ValuableMapMode,
                new ConfigDescription(
                    "Valuable map mode (0 = realtime, 1 = meeting sync (recommended; positions freeze at discovery and snapshot at meeting start / extraction complete), 2 = hidden) / " +
                    "貴重品マップ表示モード（0=リアルタイム, 1=会議同期（推奨・発見時の位置で静止表示し会議開始/抽出完了時にスナップショット更新）, 2=非表示）",
                    new AcceptableValueRange<int>(0, 2)));

            OrbGaugeEnabled = config.Bind(
                SecGeneral, "OrbGaugeEnabled", defaults.OrbGaugeEnabled,
                "Whether value lost as enemy-dropped orbs counts toward the perk gauge / " +
                "敵ドロップ品（オーブ）の減額を特典ゲージへ算入するか");

            NecroVoiceMode = config.Bind(
                SecGeneral, "NecroVoiceMode", (int)_necroVoiceModeDefault,
                new ConfigDescription(
                    "Netherworld voice eavesdrop mode (0 = OFF, 1 = non-werewolf dead, 2 = all dead). Host setting, synced to all clients / " +
                    "冥界の声の傍聴モード（0=OFF, 1=人狼以外の死者, 2=全死者）。ホスト設定で全クライアントに同期される",
                    new AcceptableValueRange<int>(0, 2)));

            GameOverAutoReturnSec = config.Bind(
                SecGeneral, "GameOverAutoReturnSec", defaults.GameOverAutoReturnSec,
                new ConfigDescription(
                    "Fallback: seconds until the result screen auto-returns to the lobby when the host does not press the return key (0 = never) / " +
                    "ホストが帰還キーを押さない場合に結果画面から自動でロビーへ戻るまでの保険秒数（0=自動で戻らない）",
                    new AcceptableValueRange<int>(0, 60)));
        }

        private void BindMeeting(ConfigFile config, GameConfig defaults)
        {

            MeetingRightsPerPlayer = config.Bind(
                SecMeeting, "MeetingRightsPerPlayer", defaults.MeetingRightsPerPlayer,
                new ConfigDescription(
                    "Emergency meeting calls per player. Consumed only when a call is accepted / " +
                    "1人あたりの会議開催権の回数。召集が受理された時点でのみ消費する",
                    new AcceptableValueRange<int>(0, 10)));

            MeetingCountdownSec = config.Bind(
                SecMeeting, "MeetingCountdownSec", defaults.MeetingCountdownSec,
                new ConfigDescription(
                    "Lead time from the meeting announcement until everyone warps (seconds) / " +
                    "会議開始通知から全員ワープまでの予告時間（秒）",
                    new AcceptableValueRange<int>(0, 30)));

            MeetingDurationSec = config.Bind(
                SecMeeting, "MeetingDurationSec", defaults.MeetingDurationSec,
                new ConfigDescription(
                    "Meeting time limit (seconds). End time = warp time + this value / " +
                    "会議の制限時間（秒）。終了時刻 = ワープ時刻 + この秒数",
                    new AcceptableValueRange<int>(10, 600)));

            VoteTimeCutEnabled = config.Bind(
                SecMeeting, "VoteTimeCutEnabled", defaults.VoteTimeCutEnabled,
                "Whether each accepted vote shortens the remaining meeting time by a share based on the number of living players / " +
                "投票が受理されるたびに会議残り時間を生存人数に応じた割合で短縮するか");

            ResultDisplaySec = config.Bind(
                SecMeeting, "ResultDisplaySec", defaults.ResultDisplaySec,
                new ConfigDescription(
                    "How long the vote results stay on screen before play resumes (seconds) / " +
                    "開票結果の表示保持時間（秒）。この経過後に通常プレイへ復帰する",
                    new AcceptableValueRange<int>(0, 60)));

            MeetingScatterEnabled = config.Bind(
                SecMeeting, "MeetingScatterEnabled", defaults.MeetingScatterEnabled,
                "Reshuffle surviving players into random groups of 3+ and warp each group to its own destination (truck / completed extraction points) when a meeting ends. Groups are announced to everyone, destinations stay hidden. No scatter below 6 survivors or before the first completed extraction (prevents fixed patrol groups) / " +
                "会議終了時に生存者を3人以上の組へ無作為に組み替え、組ごとにトラック＋納品済み抽出地点の別行き先へ分散ワープさせるか（固定グループ巡回の防止。組分けは全員へ発表・行き先は非公開。生存6人未満・最初の納品完了前は分散なし）");

            ScatterGuardSec = config.Bind(
                SecMeeting, "ScatterGuardSec", defaults.ScatterGuardSec,
                new ConfigDescription(
                    "Guard window after a scatter warp (seconds). If anyone dies within this window right after " +
                    "groups scattered, the host auto-convenes an immediate meeting (no warning countdown, no meeting " +
                    "right consumed) shown as a \"handover incident\" — punishes spawn-kills right after warp-in. " +
                    "0 = off. Not triggered during the last run / " +
                    "散開ワープ後の監視時間（秒）。分散直後のこの時間内に死亡が発生するとホストが即時会議" +
                    "（予告なし＝即時ワープ・開催権消費なし・「引き継ぎのトラブル」表示）を自動召集する" +
                    "＝着地直後の即キルの逃げ得防止。0=無効。ラストラン中は発火しない",
                    new AcceptableValueRange<int>(0, 60)));

            ConveneSuppressStartSec = config.Bind(
                SecMeeting, "ConveneSuppressStartSec", defaults.ConveneSuppressStartSec,
                new ConfigDescription(
                    "Time right after the game starts during which meetings cannot be called (seconds) / " +
                    "ゲーム開始直後に会議召集を抑止する時間（秒）",
                    new AcceptableValueRange<int>(0, 60)));

            ConveneSuppressAfterSec = config.Bind(
                SecMeeting, "ConveneSuppressAfterSec", defaults.ConveneSuppressAfterSec,
                new ConfigDescription(
                    "Time right after a meeting ends during which the next meeting cannot be called (seconds) / " +
                    "会議終了直後に次の会議召集を抑止する時間（秒）",
                    new AcceptableValueRange<int>(0, 60)));
        }

        private void BindRoleAssignment(ConfigFile config, GameConfig defaults)
        {

            ShamanChancePercent = config.Bind(
                SecRoleAssignment, "ShamanChancePercent", defaults.ShamanChancePercent,
                new ConfigDescription(
                    "Shaman appearance chance P_shaman (0..100%). Villager-side variant: one villager is converted, werewolf count N is unaffected / " +
                    "祈祷師の出現確率 P_shaman（0..100%）。村人側変種＝当選時に村人1名が変換され、人狼の人数 N には影響しない",
                    new AcceptableValueRange<int>(0, 100)));

            WerewolfCount = config.Bind(
                SecRoleAssignment, "WerewolfCount", defaults.WerewolfCount,
                new ConfigDescription(
                    "Number of werewolves N (Werewolf slots; a winning Bomber roll converts one slot, always leaving at least one pure Werewolf). The Black Cat mutates from the villager side and is not counted here / " +
                    "人狼の人数 N（人狼枠。爆弾魔当選時はこの枠の1人が爆弾魔へ変異する。純人狼は最低1人残る）。黒猫は村人陣営から変異するためこの人数には含まれない",
                    new AcceptableValueRange<int>(1, 10)));

            BlackCatChancePercent = config.Bind(
                SecRoleAssignment, "BlackCatChancePercent", defaults.BlackCatChancePercent,
                new ConfigDescription(
                    "Black Cat appearance chance P_cat (0..100%). The Black Cat mutates from one villager and joins the werewolf team; rolls happen only while at least one villager would remain / " +
                    "黒猫の出現確率 P_cat（0..100%）。黒猫は村人陣営から変異して人狼陣営に加わる（変異後も村人が最低1人残る構成でのみ抽選）",
                    new AcceptableValueRange<int>(0, 100)));

            BomberChancePercent = config.Bind(
                SecRoleAssignment, "BomberChancePercent", defaults.BomberChancePercent,
                new ConfigDescription(
                    "Bomber appearance chance P_bomber (0..100%). The Bomber mutates from one werewolf slot (needs werewolves >= 2) / " +
                    "爆弾魔の出現確率 P_bomber（0..100%）。爆弾魔は人狼枠から変異する（人狼の人数2以上で抽選）",
                    new AcceptableValueRange<int>(0, 100)));
        }

        private void BindShaman(ConfigFile config, GameConfig defaults)
        {

            ShamanGazeFullSec = config.Bind(
                SecShaman, "ShamanGazeFullSec", defaults.ShamanGazeFullSec,
                new ConfigDescription(
                    "Seconds of keeping the nearest unannounced corpse in view to trigger the spirit vision (decays at double speed while out of view) / " +
                    "霊表示に必要な注視秒数（最寄りの未周知死体が視界内で蓄積・視界外は倍速減衰）",
                    new AcceptableValueRange<int>(1, 30)));

            ShamanGhostCooldownSec = config.Bind(
                SecShaman, "ShamanGhostCooldownSec", defaults.ShamanGhostCooldownSec,
                new ConfigDescription(
                    "Cooldown after the spirit vision ends before it can charge again (seconds) / 霊表示終了から再蓄積可能になるまでのクールダウン（秒）",
                    new AcceptableValueRange<int>(0, 120)));

            ShamanStormWeakMeters = config.Bind(
                SecShaman, "ShamanStormWeakMeters", defaults.ShamanStormWeakMeters,
                new ConfigDescription(
                    "Radius of the weak haunting ring around an unannounced corpse (m) / 霊障（弱）の発動半径（m）",
                    new AcceptableValueRange<int>(1, 100)));

            ShamanStormMediumMeters = config.Bind(
                SecShaman, "ShamanStormMediumMeters", defaults.ShamanStormMediumMeters,
                new ConfigDescription(
                    "Radius of the medium haunting ring (m) / 霊障（中）の発動半径（m）",
                    new AcceptableValueRange<int>(1, 100)));

            ShamanStormStrongMeters = config.Bind(
                SecShaman, "ShamanStormStrongMeters", defaults.ShamanStormStrongMeters,
                new ConfigDescription(
                    "Radius of the strong haunting ring (m) / 霊障（強）の発動半径（m）",
                    new AcceptableValueRange<int>(1, 100)));
        }

        private void BindWerewolf(ConfigFile config, GameConfig defaults)
        {

            StaminaUnlockPct = config.Bind(
                SecWerewolf, "StaminaUnlockPct", defaults.StaminaUnlockPct,
                new ConfigDescription(
                    "Unlock threshold for infinite stamina (% of the map's total valuable value destroyed) / " +
                    "無限スタミナの解禁閾値（マップ総貴重品額に対する減額%）",
                    new AcceptableValueRange<int>(0, 100)));

            JumpUnlockPct = config.Bind(
                SecWerewolf, "JumpUnlockPct", defaults.JumpUnlockPct,
                new ConfigDescription(
                    "Unlock threshold for extra jumps (%) / 追加ジャンプの解禁閾値（%）",
                    new AcceptableValueRange<int>(0, 100)));

            ExtraJumpCount = config.Bind(
                SecWerewolf, "ExtraJumpCount", defaults.ExtraJumpCount,
                new ConfigDescription(
                    "Extra mid-air jumps (-1 = effectively unlimited) / 滞空中の追加ジャンプ回数（-1=実質無限）",
                    new AcceptableValueRange<int>(-1, 20)));

            EnemyIgnoreUnlockPct = config.Bind(
                SecWerewolf, "EnemyIgnoreUnlockPct", defaults.EnemyIgnoreUnlockPct,
                new ConfigDescription(
                    "Unlock threshold for enemy ignore (%) / 敵認識無効の解禁閾値（%）",
                    new AcceptableValueRange<int>(0, 100)));

            HealUnlockPct = config.Bind(
                SecWerewolf, "HealUnlockPct", defaults.HealUnlockPct,
                new ConfigDescription(
                    "Unlock threshold for regeneration (%) / 自然治癒の解禁閾値（%）",
                    new AcceptableValueRange<int>(0, 100)));

            HealIntervalSec = config.Bind(
                SecWerewolf, "HealIntervalSec", defaults.HealIntervalSec,
                new ConfigDescription(
                    "Regeneration interval while Wolf Mode is on (seconds per 1 HP) / " +
                    "狼化中の自然治癒の回復間隔（1HPあたりの秒数）",
                    new AcceptableValueRange<int>(1, 60)));

            BeaconChargePct = config.Bind(
                SecWerewolf, "BeaconChargePct", defaults.BeaconChargePct,
                new ConfigDescription(
                    "Threshold per beacon charge (%; each multiple reached grants every werewolf one use) / " +
                    "ビーコン1チャージあたりの閾値（%。この倍数の到達ごとに各人狼へ1回分付与）",
                    new AcceptableValueRange<int>(0, 100)));

            BeaconCooldownSec = config.Bind(
                SecWerewolf, "BeaconCooldownSec", defaults.BeaconCooldownSec,
                new ConfigDescription(
                    "Beacon cooldown (seconds) / ビーコンのクールダウン時間（秒）",
                    new AcceptableValueRange<int>(0, 600)));

            BeaconSuppressStartSec = config.Bind(
                SecWerewolf, "BeaconSuppressStartSec", defaults.BeaconSuppressStartSec,
                new ConfigDescription(
                    "Time right after the game starts during which the beacon cannot be used (seconds) / " +
                    "ゲーム開始直後のビーコン使用抑止時間（秒）",
                    new AcceptableValueRange<int>(0, 180)));

            BeaconSuppressAfterMeetingSec = config.Bind(
                SecWerewolf, "BeaconSuppressAfterMeetingSec", defaults.BeaconSuppressAfterMeetingSec,
                new ConfigDescription(
                    "Time right after a meeting ends during which the beacon cannot be used (seconds) / " +
                    "会議終了直後のビーコン使用抑止時間（秒）",
                    new AcceptableValueRange<int>(0, 180)));
        }

        private void BindBlackCat(ConfigFile config, GameConfig defaults)
        {

            BlackCatRevealDelaySec = config.Bind(
                SecBlackCat, "BlackCatRevealDelaySec", defaults.BlackCatRevealDelaySec,
                new ConfigDescription(
                    "Delay from game start until the Black Cat is privately notified of its role (seconds) / " +
                    "ゲーム開始から黒猫本人へ自覚通知するまでの遅延（秒）",
                    new AcceptableValueRange<int>(0, 300)));

            BlackCatCurseEnabled = config.Bind(
                SecBlackCat, "BlackCatCurseEnabled", defaults.BlackCatCurseEnabled,
                "Enable the Black Cat's drag-down ability when executed / " +
                "黒猫が処刑されたときの道連れ能力を有効にする");

            InformantThresholdPct = config.Bind(
                SecBlackCat, "InformantThresholdPct", defaults.InformantThresholdPct,
                new ConfigDescription(
                    "Informant threshold: reveals the werewolf list to the Black Cat (%; a high value not reached early is recommended) / " +
                    "内通（黒猫への人狼一覧開示）の閾値（%。序盤に到達しない高めを推奨）",
                    new AcceptableValueRange<int>(0, 100)));

            CatGaugeSyncIntervalSec = config.Bind(
                SecBlackCat, "CatGaugeSyncIntervalSec", defaults.CatGaugeSyncIntervalSec,
                new ConfigDescription(
                    "Gauge sync interval to the Black Cat (seconds; 0 = realtime. Werewolves always get realtime. " +
                    "Meeting start/end always refreshes regardless of the interval) / " +
                    "黒猫へのゲージ配信間隔（秒。0=リアルタイム配信。人狼へは常にリアルタイム。会議開始・終了時は間隔に関係なく更新される）",
                    new AcceptableValueRange<int>(0, 600)));
        }

        private void BindBomber(ConfigFile config, GameConfig defaults)
        {

            BomberProximityMeters = config.Bind(
                SecBomber, "BomberProximityMeters", defaults.BomberProximityMeters,
                new ConfigDescription(
                    "Distance within which the Bomber counts a plant target as close contact (m) / 爆弾魔がプラント対象を至近と見なす距離（m）",
                    new AcceptableValueRange<int>(1, 20)));

            BomberGaugeFullSec = config.Bind(
                SecBomber, "BomberGaugeFullSec", defaults.BomberGaugeFullSec,
                new ConfigDescription(
                    "Total seconds of close contact needed to fill the proximity gauge / 近接ゲージが満タンになるまでの累計滞在秒",
                    new AcceptableValueRange<int>(1, 60)));

            BomberInitialCooldownSec = config.Bind(
                SecBomber, "BomberInitialCooldownSec", defaults.BomberInitialCooldownSec,
                new ConfigDescription(
                    "Initial cooldown after the match starts; proximity gauges are paused during it (seconds) / " +
                    "試合開始直後の専用クールダウン。この間は近接ゲージも停止（秒）",
                    new AcceptableValueRange<int>(0, 180)));

            BomberCooldownSec = config.Bind(
                SecBomber, "BomberCooldownSec", defaults.BomberCooldownSec,
                new ConfigDescription(
                    "Cooldown after meetings, planting, re-planting, and detonation (seconds) / " +
                    "会議後・プラント・付替・爆破後クールダウン（秒）",
                    new AcceptableValueRange<int>(0, 60)));

            BomberAmmoRefillPct = config.Bind(
                SecBomber, "BomberAmmoRefillPct", defaults.BomberAmmoRefillPct,
                new ConfigDescription(
                    "Perk-gauge share required to refill one bomb (%) / 弾1発の補充に要する特典ゲージ割合％",
                    new AcceptableValueRange<int>(1, 100)));

            BomberBlastRadiusMeters = config.Bind(
                SecBomber, "BomberBlastRadiusMeters",
                Mathf.RoundToInt(defaults.BomberBlastRadiusMeters),
                new ConfigDescription(
                    "Blast radius / warning-sound audible radius (m, integer) / 爆風半径・警告音可聴半径（m・整数）",
                    new AcceptableValueRange<int>(1, 20)));

            BomberBlastPlayerDamage = config.Bind(
                SecBomber, "BomberBlastPlayerDamage", defaults.BomberBlastPlayerDamage,
                new ConfigDescription(
                    "Damage to nearby players / 周囲プレイヤーへのダメージ",
                    new AcceptableValueRange<int>(1, 999)));

            BomberBlastEnemyDamage = config.Bind(
                SecBomber, "BomberBlastEnemyDamage", defaults.BomberBlastEnemyDamage,
                new ConfigDescription(
                    "Damage to enemies / 敵ダメージ",
                    new AcceptableValueRange<int>(1, 999)));
        }

        private void BindStage(ConfigFile config, GameConfig defaults)
        {

            StartLevelNumber = config.Bind(
                SecStage, "StartLevelNumber", defaults.StartLevelNumber,
                new ConfigDescription(
                    "Level of the werewolf session (the N in the on-screen \"Level N\") / 人狼セッションのレベル（画面表示「Level N」の N）",
                    new AcceptableValueRange<int>(1, 30)));

            StartMapName = config.Bind(
                SecStage, "StartMapName", "",
                "Map type name for the werewolf session. Empty = random (vanilla default selection). Unknown names also mean random / " +
                "人狼セッションの開始マップ種別名。空 = ランダム（バニラ既定の選択）。実在しない名前もランダム扱い");

            StartEnergyPct = config.Bind(
                SecStage, "StartEnergyPct", defaults.StartEnergyPct,
                new ConfigDescription(
                    "Truck charger level at session start (%; 0 = empty, 100 = full) / トラック充電器の開始時充電量（%。0 = 空, 100 = 満充電）",
                    new AcceptableValueRange<int>(0, 100)));

            OrbDropMax = config.Bind(
                SecStage, "OrbDropMax", defaults.OrbDropMax,
                new ConfigDescription(
                    "Number of valuable orbs dropped when an enemy dies (vanilla = 3) / 敵撃破時にドロップする貴重品オーブの数（バニラ = 3）",
                    new AcceptableValueRange<int>(0, 20)));
        }

        private void BindPlayerUpgrades(ConfigFile config, GameConfig defaults)
        {
            var startUpgradeDefaults = new Dictionary<string, int> { ["Health"] = 1, ["Strength"] = 2 };
            _startUpgrades = new Dictionary<string, ConfigEntry<int>>(WorldgenUpgrades.Names.Count);
            foreach (var name in WorldgenUpgrades.Names)
            {
                var defaultLevel = startUpgradeDefaults.TryGetValue(name, out var lvl) ? lvl : 0;
                _startUpgrades[name] = config.Bind(
                    SecPlayerUpgrades, "StartUpgrade" + name, defaultLevel,
                    new ConfigDescription(
                        $"Starting upgrade level shared by all players ({name}). 0 = no upgrade / 全プレイヤー共通の開始時強化段階（{name}）。0 = 強化なし",
                        new AcceptableValueRange<int>(0, 20)));
            }
        }

        private void BindClientKeybinds(ConfigFile config, GameConfig defaults)
        {

            WolfModeKey = config.Bind(
                SecClientKeybinds, "WolfModeKey", KeyCode.F,
                new ConfigDescription(
                    "Key binding for the Wolf Mode toggle (bulk ON/OFF of unlocked perks) / " +
                    "狼化トグル（解禁済み特典の一括ON/OFF）のキーバインド",
                    null, "HideFromREPOConfig"));

            BeaconKey = config.Bind(
                SecClientKeybinds, "BeaconKey", KeyCode.G,
                new ConfigDescription(
                    "Key binding for using the beacon / ビーコン使用のキーバインド",
                    null, "HideFromREPOConfig"));

            BomberPlantKey = config.Bind(
                SecClientKeybinds, "BomberPlantKey", KeyCode.F,
                new ConfigDescription(
                    "Key binding for the Bomber's plant (turn a player into a bomb) / 爆弾魔のプラント（爆弾化）キーバインド",
                    null, "HideFromREPOConfig"));

            BomberDetonateKey = config.Bind(
                SecClientKeybinds, "BomberDetonateKey", KeyCode.G,
                new ConfigDescription(
                    "Key binding for the Bomber's detonation / 爆弾魔の起爆キーバインド",
                    null, "HideFromREPOConfig"));

            CorpseReportKey = config.Bind(
                SecClientKeybinds, "CorpseReportKey", KeyCode.R,
                new ConfigDescription(
                    "Key binding to report an emergency meeting near an undiscovered body (dead player's head) / " +
                    "未発見の死体（死者頭部）の近くで押すと緊急会議を通報するキーバインド",
                    null, "HideFromREPOConfig"));

            MeetingMapKey = config.Bind(
                SecClientKeybinds, "MeetingMapKey", KeyCode.M,
                new ConfigDescription(
                    "Key binding to toggle the full-map overlay during meetings / 会議中の全体マップオーバーレイのトグルキー",
                    null, "HideFromREPOConfig"));

            MeetingChatLogKey = config.Bind(
                SecClientKeybinds, "MeetingChatLogKey", KeyCode.L,
                new ConfigDescription(
                    "Key binding to toggle the chat log panel during meetings / 会議チャットログのトグルキー",
                    null, "HideFromREPOConfig"));

            LobbySettingsPanelKey = config.Bind(
                SecClientKeybinds, "LobbySettingsPanelKey", KeyCode.F7,
                new ConfigDescription(
                    "Key binding to toggle the lobby settings panel / ロビー設定確認パネルの表示/非表示トグルのキーバインド",
                    null, "HideFromREPOConfig"));

            ManualKey = config.Bind(
                SecClientKeybinds, "ManualKey", KeyCode.F1,
                new ConfigDescription(
                    "Key binding to toggle the in-game manual / ゲーム内説明書の表示/非表示トグルのキーバインド",
                    null, "HideFromREPOConfig"));

            ResultReturnKey = config.Bind(
                SecClientKeybinds, "ResultReturnKey", KeyCode.F5,
                new ConfigDescription(
                    "Host only: key binding to return to the lobby from the match result screen / " +
                    "ホスト専用: 試合結果画面からロビーへ戻るキーバインド",
                    null, "HideFromREPOConfig"));

            VoidMatchKey = config.Bind(
                SecClientKeybinds, "VoidMatchKey", KeyCode.F5,
                new ConfigDescription(
                    "Host only: hold this key to declare the match a no contest / " +
                    "ホスト専用: 長押しで試合を無効試合にするキーバインド",
                    null, "HideFromREPOConfig"));
        }

        private void BindClientUi(ConfigFile config, GameConfig defaults)
        {

            ButtonOffsetX = config.Bind(
                SecClientUi, "ButtonOffsetX", defaults.ButtonOffsetX,
                new ConfigDescription(
                    "Meeting button offset X relative to the truck (fine-tuned on Stage 1) / 会議召集ボタンのトラック基準相対オフセットX",
                    new AcceptableValueRange<float>(-10f, 10f), "HideFromREPOConfig"));

            ButtonOffsetY = config.Bind(
                SecClientUi, "ButtonOffsetY", defaults.ButtonOffsetY,
                new ConfigDescription(
                    "Meeting button offset Y relative to the truck (fine-tuned on Stage 1) / 会議召集ボタンのトラック基準相対オフセットY",
                    new AcceptableValueRange<float>(-10f, 10f), "HideFromREPOConfig"));

            ButtonOffsetZ = config.Bind(
                SecClientUi, "ButtonOffsetZ", defaults.ButtonOffsetZ,
                new ConfigDescription(
                    "Meeting button offset Z relative to the truck (fine-tuned on Stage 1) / 会議召集ボタンのトラック基準相対オフセットZ",
                    new AcceptableValueRange<float>(-10f, 10f), "HideFromREPOConfig"));

            ButtonYaw = config.Bind(
                SecClientUi, "ButtonYaw", defaults.ButtonYaw,
                new ConfigDescription(
                    "Meeting button yaw fine-tune (degrees; tuned on Stage 1) / 会議召集ボタンの向き（Yaw, 度）の微調整",
                    new AcceptableValueRange<float>(-180f, 180f), "HideFromREPOConfig"));

            ButtonPitch = config.Bind(
                SecClientUi, "ButtonPitch", defaults.ButtonPitch,
                new ConfigDescription(
                    "Meeting button pitch (degrees). 0 = flat facing up, 90 = wall mount (vertical, facing forward) / " +
                    "会議召集ボタンのピッチ（度）。0=水平面上向き、90=壁面マウント（垂直面前向き）",
                    new AcceptableValueRange<float>(-180f, 180f), "HideFromREPOConfig"));

            ToastDurationSec = config.Bind(
                SecClientUi, "ToastDurationSec", defaults.ToastDurationSec,
                new ConfigDescription(
                    "Toast notification duration (seconds) / トースト通知の表示秒数",
                    new AcceptableValueRange<int>(0, 60), "HideFromREPOConfig"));

            MeetingMapOrthoSize = config.Bind(
                SecClientUi, "MeetingMapOrthoSize", 60f,
                new ConfigDescription(
                    "Camera orthographicSize while the full-map overlay is open (larger = wider view). " +
                    "MiniMap MOD defaults to 2.25 / max 10; a larger default is used here to show the whole level. " +
                    "Needs vary per level, so tune in-game (recommended range 5-50) / " +
                    "全体マップオーバーレイ表示時のカメラ orthographicSize（大きいほど広範囲）。レベル毎に必要な広さが異なるため実機で調整可（推奨レンジ 5〜50）",
                    new AcceptableValueRange<float>(1.5f, 200f), "HideFromREPOConfig"));

            MeetingMapResolution = config.Bind(
                SecClientUi, "MeetingMapResolution", 1,
                new ConfigDescription(
                    "Full-map overlay render resolution preset. " +
                    "0=1280x768 / 1=1600x960 / 2=1920x1152 (all 5:3, same aspect as the 1200x720 panel). " +
                    "The vanilla handheld-map RT is low-res and blurry when magnified, so a dedicated RT replaces it while the overlay is open / " +
                    "全体マップオーバーレイのレンダリング解像度プリセット（0=1280x768 / 1=1600x960 / 2=1920x1152）",
                    new AcceptableValueRange<int>(0, 2), "HideFromREPOConfig"));

            MeetingMapGrid = config.Bind(
                SecClientUi, "MeetingMapGrid", true,
                new ConfigDescription(
                    "Show an Excel-style coordinate grid on the meeting full-map overlay " +
                    "(columns A,B,C... left to right / rows 1,2,3... top to bottom; one cell = one room module), " +
                    "so locations can be called out like \"the room at C5\" / " +
                    "会議全体マップにエクセル風の座標グリッド（列=左からA,B,C…・行=上から1,2,3…・" +
                    "1マス=1部屋モジュール）を重ねて表示する（「C5の部屋」のような位置伝達用）"));

            MeetingChatLog = config.Bind(
                SecClientUi, "MeetingChatLog", true,
                new ConfigDescription(
                    "Show the chat log panel on the right side of the screen during meetings " +
                    "(speaker avatar and message bubbles, plus a recap of deaths, destroyed valuables, " +
                    "delivery progress and beacon uses). Messages from the dead are shown only to the dead / " +
                    "会議中に画面右端へ会議チャットログを表示する（発言者アバターと吹き出し＋死亡者・破壊された貴重品・" +
                    "納品状況・ビーコン使用の要約）。死者の発言は死者にだけ表示される"));

            CursorMirrorScale = config.Bind(
                SecClientUi, "CursorMirrorScale", 1.0f,
                new ConfigDescription(
                    "Scale of the mirror cursor drawn over meeting panels (1.0 = vanilla cursor's measured size) / " +
                    "会議パネル等の上に重ねるミラーカーソルのサイズ倍率（1.0=バニラカーソルの実測換算そのまま）",
                    new AcceptableValueRange<float>(0.5f, 3.0f)));

            HudOffsetX = config.Bind(
                SecClientUi, "HudOffsetX (+right)", 0,
                new ConfigDescription(
                    "Horizontal offset of the top-left role badge in 1920x1080-reference px (+ = right). " +
                    "Adjust if it overlaps the vanilla health/stamina display on your resolution / " +
                    "左上役職バッジの横オフセット（1920x1080基準px・＋で右）。バニラの体力/スタミナ表示と重なる環境で調整する",
                    new AcceptableValueRange<int>(-1000, 1000)));

            HudOffsetY = config.Bind(
                SecClientUi, "HudOffsetY (+down)", 0,
                new ConfigDescription(
                    "Vertical offset of the top-left role badge in 1920x1080-reference px (+ = down). " +
                    "Adjust if it overlaps the vanilla health/stamina display on your resolution / " +
                    "左上役職バッジの縦オフセット（1920x1080基準px・＋で下）。バニラの体力/スタミナ表示と重なる環境で調整する",
                    new AcceptableValueRange<int>(-1000, 1000)));
        }

        private void BindStreamer(ConfigFile config, GameConfig defaults)
        {

            StreamerSafeMode = config.Bind(
                SecStreamer, "StreamerSafeMode", false,
                new ConfigDescription(
                    "Streamer-safe mode: replaces some parody visuals and distinctive sound effects " +
                    "(the Bomber's two ability icons, the meeting-convene chime, and the two execution " +
                    "sounds) with generic assets or silence, to avoid automated content detection on " +
                    "streaming platforms and viewer misunderstandings. Local setting (affects only " +
                    "your own screen and audio); applies after a game restart / " +
                    "配信者向けセーフモード: 配信プラットフォームの自動判定や視聴者の誤解を避けるため、" +
                    "一部のパロディ表現や特徴的な効果音（爆弾魔の能力アイコン2種・会議招集チャイム・" +
                    "処刑演出の合の手2種）を汎用素材・無音に置き換える。ローカル設定（自分の画面と音にだけ効く）・反映はゲーム再起動",
                    null, "HideFromREPOConfig"));
        }

        private void BindTutorial(ConfigFile config, GameConfig defaults)
        {

            ResetTutorials = config.Bind(
                SecTutorial, "ResetTutorials", false,
                "Set to true to reset all tutorial seen-flags (flips back to false automatically on the next poll; " +
                "acts as a reset button to show the tutorials again) / " +
                "true にするとチュートリアルの既読状態を全てリセットする（次のポーリングで自動的にfalseへ戻る。" +
                "チュートリアルをもう一度表示させたい時のリセットボタン代わり）");

            TutorialFontScale = config.Bind(
                SecTutorial, "FontScale", 0.7f,
                new ConfigDescription(
                    "Tutorial text font scale (ratio against the vanilla tutorial notification's maximum size) / " +
                    "チュートリアル文言のフォントサイズ倍率（バニラのチュートリアル通知の上限サイズに対する比率）",
                    new AcceptableValueRange<float>(0.4f, 1.5f)));

            _tutorialSeen = new Dictionary<TutorialId, ConfigEntry<bool>>();
            foreach (TutorialId id in Enum.GetValues(typeof(TutorialId)))
            {
                _tutorialSeen[id] = config.Bind(
                    SecTutorial, "Seen" + id, false,
                    new ConfigDescription(
                        $"Whether tutorial \"{id}\" has been shown (internal bookkeeping) / チュートリアル「{id}」を表示済みか（内部管理用）",
                        null, "HideFromREPOConfig"));
            }
        }

        private void BindVoice(ConfigFile config, GameConfig defaults)
        {

            NecroVoiceVolume = config.Bind(
                SecVoice, "NecroVoiceVolume", defaults.NecroVoiceVolume,
                new ConfigDescription(
                    "Eavesdrop voice volume (0..1; vanilla normal voice is roughly 0.5) / 傍聴音声の再生音量（0..1。バニラ通常声≒0.5相当）",
                    new AcceptableValueRange<float>(0f, 1f), "HideFromREPOConfig"));

            NecroVoiceLowPassCutoffHz = config.Bind(
                SecVoice, "NecroVoiceLowPassCutoffHz", defaults.NecroVoiceLowPassCutoffHz,
                new ConfigDescription(
                    "Eavesdrop voice muffling (LowPass cutoff frequency, Hz) / 傍聴音声のくすみ（LowPass カットオフ周波数 Hz）",
                    new AcceptableValueRange<float>(100f, 20000f), "HideFromREPOConfig"));

            NecroVoiceEchoDelayMs = config.Bind(
                SecVoice, "NecroVoiceEchoDelayMs", defaults.NecroVoiceEchoDelayMs,
                new ConfigDescription(
                    "Eavesdrop voice echo interval (ms) / 傍聴音声の反響1回の間隔（ms）",
                    new AcceptableValueRange<float>(10f, 2000f), "HideFromREPOConfig"));

            NecroVoiceEchoDecay = config.Bind(
                SecVoice, "NecroVoiceEchoDecay", defaults.NecroVoiceEchoDecay,
                new ConfigDescription(
                    "Eavesdrop voice echo decay (0..1) / 傍聴音声の反響の減衰率（0..1）",
                    new AcceptableValueRange<float>(0f, 1f), "HideFromREPOConfig"));

            NecroVoiceReverbRoom = config.Bind(
                SecVoice, "NecroVoiceReverbRoom", defaults.NecroVoiceReverbRoom,
                new ConfigDescription(
                    "Eavesdrop voice reverb room level (1/100 dB) / 傍聴音声の Reverb ルーム全体レベル（100分の1dB）",
                    new AcceptableValueRange<float>(-10000f, 0f), "HideFromREPOConfig"));

            NecroVoiceReverbRoomHF = config.Bind(
                SecVoice, "NecroVoiceReverbRoomHF", defaults.NecroVoiceReverbRoomHF,
                new ConfigDescription(
                    "Eavesdrop voice reverb room HF level (1/100 dB) / 傍聴音声の Reverb ルーム高域レベル（100分の1dB）",
                    new AcceptableValueRange<float>(-10000f, 0f), "HideFromREPOConfig"));

            NecroVoiceReverbDecayTime = config.Bind(
                SecVoice, "NecroVoiceReverbDecayTime", defaults.NecroVoiceReverbDecayTime,
                new ConfigDescription(
                    "Eavesdrop voice reverb decay time (seconds) / 傍聴音声の Reverb 残響減衰時間（秒）",
                    new AcceptableValueRange<float>(0.1f, 20f), "HideFromREPOConfig"));

            NecroVoiceReverbDecayHFRatio = config.Bind(
                SecVoice, "NecroVoiceReverbDecayHFRatio", defaults.NecroVoiceReverbDecayHFRatio,
                new ConfigDescription(
                    "Eavesdrop voice reverb HF decay ratio / 傍聴音声の Reverb 高域減衰比",
                    new AcceptableValueRange<float>(0.1f, 2f), "HideFromREPOConfig"));

            NecroVoiceReverbReflections = config.Bind(
                SecVoice, "NecroVoiceReverbReflections", defaults.NecroVoiceReverbReflections,
                new ConfigDescription(
                    "Eavesdrop voice reverb early reflection level (1/100 dB) / 傍聴音声の Reverb 初期反射レベル（100分の1dB）",
                    new AcceptableValueRange<float>(-10000f, 1000f), "HideFromREPOConfig"));

            NecroVoiceReverbReflectionsDelay = config.Bind(
                SecVoice, "NecroVoiceReverbReflectionsDelay", defaults.NecroVoiceReverbReflectionsDelay,
                new ConfigDescription(
                    "Eavesdrop voice reverb early reflection delay (seconds) / 傍聴音声の Reverb 初期反射遅延（秒）",
                    new AcceptableValueRange<float>(0f, 0.3f), "HideFromREPOConfig"));

            NecroVoiceReverbLevel = config.Bind(
                SecVoice, "NecroVoiceReverbLevel", defaults.NecroVoiceReverbLevel,
                new ConfigDescription(
                    "Eavesdrop voice reverb late reflection level (1/100 dB) / 傍聴音声の Reverb 後期反射レベル（100分の1dB）",
                    new AcceptableValueRange<float>(-10000f, 2000f), "HideFromREPOConfig"));

            NecroVoiceReverbDelay = config.Bind(
                SecVoice, "NecroVoiceReverbDelay", defaults.NecroVoiceReverbDelay,
                new ConfigDescription(
                    "Eavesdrop voice reverb late reflection delay (seconds) / 傍聴音声の Reverb 後期反射遅延（秒）",
                    new AcceptableValueRange<float>(0f, 0.1f), "HideFromREPOConfig"));

            NecroVoiceReverbDiffusion = config.Bind(
                SecVoice, "NecroVoiceReverbDiffusion", defaults.NecroVoiceReverbDiffusion,
                new ConfigDescription(
                    "Eavesdrop voice reverb diffusion (0..100) / 傍聴音声の Reverb 拡散度（0..100）",
                    new AcceptableValueRange<float>(0f, 100f), "HideFromREPOConfig"));

            NecroVoiceReverbDensity = config.Bind(
                SecVoice, "NecroVoiceReverbDensity", defaults.NecroVoiceReverbDensity,
                new ConfigDescription(
                    "Eavesdrop voice reverb density (0..100) / 傍聴音声の Reverb 密度（0..100）",
                    new AcceptableValueRange<float>(0f, 100f), "HideFromREPOConfig"));

            NecroVoiceReverbHFReference = config.Bind(
                SecVoice, "NecroVoiceReverbHFReference", defaults.NecroVoiceReverbHFReference,
                new ConfigDescription(
                    "Eavesdrop voice reverb HF reference frequency (Hz) / 傍聴音声の Reverb 高域基準周波数（Hz）",
                    new AcceptableValueRange<float>(20f, 20000f), "HideFromREPOConfig"));
        }

        private void BindDebug(ConfigFile config, GameConfig defaults)
        {

            DebugMode = config.Bind(
                SecDebug, "DebugMode", defaults.DebugMode,
                new ConfigDescription(
                    "Debug mode. true allows cheat commands (/ww ...), writes secret log lines immediately " +
                    "(false batches them until the match ends), and shows a persistent TEST PLAY banner " +
                    "to every participant when the host enables it / " +
                    "デバッグモード。true でチートコマンド（/ww ...）を許可し、secret ログ行を即時出力する" +
                    "（false は試合終了時に一括出力）。ホストが有効にすると全参加者に" +
                    "テストプレイ常設バナーが表示される",
                    null, "HideFromREPOConfig"));
        }

        internal GameConfig Snapshot()
        {
            var v = ValuableMapMode.Value;
            var valuableMapMode = (v >= 0 && v <= 2)
                ? (Werewolf.Core.ValuableMapMode)v
                : _valuableMapModeDefault;

            var nvm = NecroVoiceMode.Value;
            var necroVoiceMode = (nvm >= 0 && nvm <= 2)
                ? (Werewolf.Core.NecroVoiceMode)nvm
                : _necroVoiceModeDefault;

            var upgradeLevels = new Dictionary<string, int>(_startUpgrades.Count);
            foreach (var pair in _startUpgrades) upgradeLevels[pair.Key] = pair.Value.Value;

            return new GameConfig
            {
                WerewolfCount = WerewolfCount.Value,
                BlackCatChancePercent = BlackCatChancePercent.Value,
                BomberChancePercent = BomberChancePercent.Value,
                RoundSeconds = RoundSeconds.Value,
                BlackCatRevealDelaySec = BlackCatRevealDelaySec.Value,
                BlackCatCurseEnabled = BlackCatCurseEnabled.Value,
                DebugMode = DebugMode.Value,
                MeetingRightsPerPlayer = MeetingRightsPerPlayer.Value,
                ConveneSuppressStartSec = ConveneSuppressStartSec.Value,
                ConveneSuppressAfterSec = ConveneSuppressAfterSec.Value,
                MeetingCountdownSec = MeetingCountdownSec.Value,
                MeetingDurationSec = MeetingDurationSec.Value,
                VoteTimeCutEnabled = VoteTimeCutEnabled.Value,
                ResultDisplaySec = ResultDisplaySec.Value,
                MeetingScatterEnabled = MeetingScatterEnabled.Value,
                ScatterGuardSec = ScatterGuardSec.Value,
                ButtonOffsetX = ButtonOffsetX.Value,
                ButtonOffsetY = ButtonOffsetY.Value,
                ButtonOffsetZ = ButtonOffsetZ.Value,
                ButtonYaw = ButtonYaw.Value,
                ButtonPitch = ButtonPitch.Value,
                StaminaUnlockPct = StaminaUnlockPct.Value,
                JumpUnlockPct = JumpUnlockPct.Value,
                EnemyIgnoreUnlockPct = EnemyIgnoreUnlockPct.Value,
                HealUnlockPct = HealUnlockPct.Value,
                HealIntervalSec = HealIntervalSec.Value,
                BeaconChargePct = BeaconChargePct.Value,
                InformantThresholdPct = InformantThresholdPct.Value,
                ExtraJumpCount = ExtraJumpCount.Value,
                BeaconCooldownSec = BeaconCooldownSec.Value,
                BeaconSuppressStartSec = BeaconSuppressStartSec.Value,
                BeaconSuppressAfterMeetingSec = BeaconSuppressAfterMeetingSec.Value,
                CatGaugeSyncIntervalSec = CatGaugeSyncIntervalSec.Value,
                OrbGaugeEnabled = OrbGaugeEnabled.Value,
                WerewolfModeEnabled = WerewolfModeEnabled.Value,
                MinimapHideEnabled = MinimapHideEnabled.Value,
                OutfitChangeAllowed = OutfitChangeAllowed.Value,
                ValuableMapMode = valuableMapMode,
                GameOverAutoReturnSec = GameOverAutoReturnSec.Value,
                ToastDurationSec = ToastDurationSec.Value,
                StartLevelNumber = StartLevelNumber.Value,
                StartMapName = StartMapName.Value ?? "",
                StartItemsSpec = ItemsSpecProvider?.Invoke() ?? "",
                StartEnergyPct = StartEnergyPct.Value,
                StartUpgradesSpec = WorldgenSpec.Encode(upgradeLevels),
                OrbDropMax = OrbDropMax.Value,
                NecroVoiceMode = necroVoiceMode,
                NecroVoiceVolume = NecroVoiceVolume.Value,
                NecroVoiceLowPassCutoffHz = NecroVoiceLowPassCutoffHz.Value,
                NecroVoiceEchoDelayMs = NecroVoiceEchoDelayMs.Value,
                NecroVoiceEchoDecay = NecroVoiceEchoDecay.Value,
                NecroVoiceReverbRoom = NecroVoiceReverbRoom.Value,
                NecroVoiceReverbRoomHF = NecroVoiceReverbRoomHF.Value,
                NecroVoiceReverbDecayTime = NecroVoiceReverbDecayTime.Value,
                NecroVoiceReverbDecayHFRatio = NecroVoiceReverbDecayHFRatio.Value,
                NecroVoiceReverbReflections = NecroVoiceReverbReflections.Value,
                NecroVoiceReverbReflectionsDelay = NecroVoiceReverbReflectionsDelay.Value,
                NecroVoiceReverbLevel = NecroVoiceReverbLevel.Value,
                NecroVoiceReverbDelay = NecroVoiceReverbDelay.Value,
                NecroVoiceReverbDiffusion = NecroVoiceReverbDiffusion.Value,
                NecroVoiceReverbDensity = NecroVoiceReverbDensity.Value,
                NecroVoiceReverbHFReference = NecroVoiceReverbHFReference.Value,
                ShamanChancePercent = ShamanChancePercent.Value,
                ShamanGazeFullSec = ShamanGazeFullSec.Value,
                ShamanGhostCooldownSec = ShamanGhostCooldownSec.Value,
                ShamanStormWeakMeters = ShamanStormWeakMeters.Value,
                ShamanStormMediumMeters = ShamanStormMediumMeters.Value,
                ShamanStormStrongMeters = ShamanStormStrongMeters.Value,
                BomberProximityMeters = BomberProximityMeters.Value,
                BomberGaugeFullSec = BomberGaugeFullSec.Value,
                BomberInitialCooldownSec = BomberInitialCooldownSec.Value,
                BomberCooldownSec = BomberCooldownSec.Value,
                BomberBlastRadiusMeters = BomberBlastRadiusMeters.Value,
                BomberBlastPlayerDamage = BomberBlastPlayerDamage.Value,
                BomberBlastEnemyDamage = BomberBlastEnemyDamage.Value,
                BomberAmmoRefillPct = BomberAmmoRefillPct.Value,
            };
        }

        internal bool IsTutorialSeen(TutorialId id)
            => !_tutorialSeen.TryGetValue(id, out var entry) || entry.Value;

        internal void MarkTutorialSeen(TutorialId id)
        {
            if (_tutorialSeen.TryGetValue(id, out var entry)) entry.Value = true;
        }

        internal void TickTutorialReset()
        {
            if (!ResetTutorials.Value) return;
            foreach (var entry in _tutorialSeen.Values) entry.Value = false;
            ResetTutorials.Value = false;
        }
    }
}
