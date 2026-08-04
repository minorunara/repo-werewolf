using System;
using System.IO;
using System.Text;
using Werewolf.Core;
using Werewolf.Game;

namespace Werewolf.Debugging
{
    internal static class CheatCommands
    {
        private const string Usage =
            "/ww <start|bot [n]|role <actor> <villager|werewolf|blackcat|bomber|shaman>|skiptimer|" +
            "checkmate|reveal <selfcat|mates>|kill <actor> [vote]|phase <play|meeting|gameover>|" +
            "meeting|vote <actor|skip> [asActor]|leave <actor>|meetingstatus|status|selftest|" +
            "gauge <pct>|beacon <charge [n]|use>|perk <stamina|jump|ghost|heal|all>|informant|curse [actor]|" +
            "body [clear]|spawnbag [dollars]|bomb <gauge|plant <actor>|detonate|ammo [n]>|" +
            "fx <reveal [villager|werewolf|blackcat]|toast [message]|countdown [sec]|" +
            "result|sfx [countdown|howl|toast]|clear>|" +
            "cfg <inject <id=value;...>|clear>|echo|lang export|" +
            "chat <say <text>|as <actor> <text>|name <actor> <name> <text>|spam [n]|vote [actor]|" +
            "baseline|late [ms] [text]|gate|avatar|state|clear>|res <w> <h>>";
        internal static void Execute(string message)
        {
            if (!CommandGate.TryParse(message, out string command, out string[] args))
            {
                return;
            }

            bool isHost = SemiFunc.IsMasterClientOrSingleplayer();
            bool debugMode = Plugin.GameConfig != null && Plugin.GameConfig.DebugMode;

            var verdict = CommandGate.Decide(command, isHost, debugMode);
            if (verdict != CommandGateVerdict.Allowed)
            {
                WLog.Line("cmd_rejected", secret: false,
                    ("name", command),
                    ("reason", verdict == CommandGateVerdict.RejectedNotHost
                        ? "not_host" : "debug_mode_disabled"));
                return;
            }

            WLog.Line("cmd", secret: false,
                ("name", command),
                ("args", command == "role" || command == "vote"
                    || command == "gauge" || command == "beacon"
                    || command == "perk" || command == "curse"
                    ? new[] { "<redacted>" } : args));

            var director = WerewolfDirector.Instance;
            if (director == null)
            {
                WLog.Line("cmd_rejected", secret: false, ("name", command), ("reason", "no_director"));
                return;
            }

            switch (command)
            {
                case "start":
                    HandleStart(director);
                    break;

                case "bot":
                    HandleBot(director, args);
                    break;

                case "role":
                    HandleRole(director, args);
                    break;

                case "skiptimer":
                    director.HostForceExpireTimer();
                    break;

                case "checkmate":
                    if (!director.DebugForceCheckmate())
                    {
                        WLog.Line("cmd_rejected", secret: false,
                            ("name", "checkmate"), ("reason", "no_session_or_not_host"));
                    }
                    break;

                case "reveal":
                    HandleReveal(director, args);
                    break;

                case "kill":
                    HandleKill(director, args);
                    break;

                case "phase":
                    HandlePhase(director, args);
                    break;

                case "meeting":
                    director.SendConveneRequest();
                    break;

                case "report":
                    director.SendConveneRequest(Core.ConveneKind.CorpseReport);
                    break;

                case "body":
                    if (args.Length >= 1 && args[0].ToLowerInvariant() == "clear")
                    {
                        director.DebugClearFakeBodies();
                    }
                    else
                    {
                        director.DebugSpawnFakeBodies();
                    }
                    break;

                case "spawnbag":
                    {
                        int dollars = 5000;
                        if (args.Length >= 1 && (!int.TryParse(args[0], out dollars) || dollars < 1))
                        {
                            WLog.Line("cmd_error", secret: false,
                                ("name", "spawnbag"), ("reason", "bad_value"), ("arg", args[0]));
                            break;
                        }
                        director.DebugSpawnMoneyBag(dollars);
                    }
                    break;

                case "vote":
                    HandleVote(director, args);
                    break;

                case "leave":
                    HandleLeave(director, args);
                    break;

                case "meetingstatus":
                    director.DumpMeetingStatus();
                    break;

                case "status":
                    director.DumpStatus();
                    break;

                case "selftest":
                    SelfTest.RunAll();
                    break;

                case "gauge":
                    HandleGauge(director, args);
                    break;

                case "beacon":
                    HandleBeacon(director, args);
                    break;

                case "perk":
                    HandlePerk(director, args);
                    break;

                case "informant":
                    director.DebugRolesInformant();
                    break;

                case "curse":
                    HandleCurse(director, args);
                    break;

                case "bomb":
                    HandleBomb(director, args);
                    break;

                case "fx":
                    HandleFx(director, args);
                    break;

                case "cfg":
                    HandleCfg(director, args);
                    break;

                case "echo":
                    director.DebugToggleSelfEcho();
                    break;

                case "lang":
                    HandleLang(args);
                    break;

                case "chat":
                    HandleChat(director, args);
                    break;

                case "res":
                    HandleRes(args);
                    break;

                case "":
                    WLog.Line("cmd_usage", secret: false, ("usage", Usage));
                    break;

                default:
                    WLog.Line("cmd_rejected", secret: false,
                        ("name", command), ("reason", "unknown_command"));
                    break;
            }
        }

        private static void HandleBot(WerewolfDirector director, string[] args)
        {
            const int firstBotActor = -101;

            int count = 1;
            if (args.Length >= 1 && (!int.TryParse(args[0], out count) || count < 1))
            {
                WLog.Line("cmd_error", secret: false, ("name", "bot"), ("reason", "bad_count"), ("arg", args[0]));
                return;
            }

            int first = director.PendingBotCount;
            for (int i = 0; i < count; i++)
            {
                director.AddPendingBot(new WPlayer
                {
                    ActorNumber = firstBotActor - (first + i),
                    Name = "Bot" + (first + i + 1),
                    IsBot = true,
                });
            }
            WLog.Line("cmd_bot", secret: false,
                ("added", count), ("pendingBots", director.PendingBotCount));
        }

        private static void HandleRole(WerewolfDirector director, string[] args)
        {
            if (args.Length < 2 || !int.TryParse(args[0], out int actor))
            {
                WLog.Line("cmd_error", secret: false, ("name", "role"), ("reason", "bad_args"), ("usage", Usage));
                return;
            }

            Role role;
            switch (args[1].ToLowerInvariant())
            {
                case "villager": role = Role.Villager; break;
                case "werewolf": role = Role.Werewolf; break;
                case "blackcat": role = Role.BlackCat; break;
                case "bomber": role = Role.Bomber; break;
                case "shaman": role = Role.Shaman; break;
                default:
                    WLog.Line("cmd_error", secret: false, ("name", "role"), ("reason", "bad_role"), ("arg", args[1]));
                    return;
            }

            director.ReserveForcedRole(actor, role);
            WLog.Line("cmd_role", secret: true, ("actor", actor), ("role", role));
        }

        private static void HandleReveal(WerewolfDirector director, string[] args)
        {
            if (args.Length < 1)
            {
                WLog.Line("cmd_error", secret: false, ("name", "reveal"), ("reason", "bad_args"), ("usage", Usage));
                return;
            }

            switch (args[0].ToLowerInvariant())
            {
                case "selfcat":
                    director.HostNotifyDisclosure(DisclosureKind.BlackCatSelfAwareness);
                    break;
                case "mates":
                    director.HostNotifyDisclosure(DisclosureKind.BlackCatSeesWerewolves);
                    break;
                default:
                    WLog.Line("cmd_error", secret: false, ("name", "reveal"), ("reason", "bad_kind"), ("arg", args[0]));
                    break;
            }
        }

        private static void HandleKill(WerewolfDirector director, string[] args)
        {
            if (args.Length < 1 || !int.TryParse(args[0], out int actor))
            {
                WLog.Line("cmd_error", secret: false, ("name", "kill"), ("reason", "bad_args"), ("usage", Usage));
                return;
            }

            bool asVote = args.Length >= 2 &&
                string.Equals(args[1], "vote", StringComparison.OrdinalIgnoreCase);
            director.HostRecordDeathByActor(actor, asVote);
        }

        private static void HandleVote(WerewolfDirector director, string[] args)
        {
            if (args.Length < 1)
            {
                WLog.Line("cmd_error", secret: false, ("name", "vote"), ("reason", "bad_args"), ("usage", Usage));
                return;
            }

            int target;
            if (string.Equals(args[0], "skip", StringComparison.OrdinalIgnoreCase))
            {
                target = -1;
            }
            else if (!int.TryParse(args[0], out target))
            {
                WLog.Line("cmd_error", secret: false, ("name", "vote"), ("reason", "bad_target"));
                return;
            }
            else if (target == -1)
            {
                WLog.Line("cmd_vote_note", secret: true,
                    ("note", "target -1 is the skip sentinel; treated as skip"));
            }

            if (args.Length >= 2)
            {
                if (!int.TryParse(args[1], out int asActor))
                {
                    WLog.Line("cmd_error", secret: false, ("name", "vote"), ("reason", "bad_as_actor"));
                    return;
                }
                director.HostCastVoteAsActor(asActor, target);
            }
            else
            {
                director.SendVote(target);
            }
        }

        private static void HandleLeave(WerewolfDirector director, string[] args)
        {
            if (args.Length < 1 || !int.TryParse(args[0], out int actor))
            {
                WLog.Line("cmd_error", secret: false, ("name", "leave"), ("reason", "bad_args"), ("usage", Usage));
                return;
            }
            director.SimulatePlayerLeave(actor);
        }

        private static void HandlePhase(WerewolfDirector director, string[] args)
        {
            if (args.Length < 1)
            {
                WLog.Line("cmd_error", secret: false, ("name", "phase"), ("reason", "bad_args"), ("usage", Usage));
                return;
            }

            GamePhase target;
            switch (args[0].ToLowerInvariant())
            {
                case "play": target = GamePhase.Play; break;
                case "meeting": target = GamePhase.Meeting; break;
                case "gameover": target = GamePhase.GameOver; break;
                default:
                    WLog.Line("cmd_error", secret: false, ("name", "phase"), ("reason", "bad_phase"), ("arg", args[0]));
                    return;
            }

            PhaseChangeResult result = director.HostRequestPhaseChange(target);
            WLog.Line("cmd_phase", secret: false,
                ("target", target), ("result", result.Success ? "ok" : "rejected"),
                ("reason", result.Reason));
        }

        private static void HandleGauge(WerewolfDirector director, string[] args)
        {
            if (args.Length < 1 || !int.TryParse(args[0], out int pct) || pct < 1)
            {
                WLog.Line("cmd_error", secret: false, ("name", "gauge"), ("reason", "bad_pct"), ("usage", Usage));
                return;
            }
            director.DebugRolesGauge(pct);
        }

        private static void HandleBeacon(WerewolfDirector director, string[] args)
        {
            string op = args.Length >= 1 ? args[0].ToLowerInvariant() : "";
            switch (op)
            {
                case "charge":
                    int count = 1;
                    if (args.Length >= 2 && (!int.TryParse(args[1], out count) || count < 1))
                    {
                        WLog.Line("cmd_error", secret: false, ("name", "beacon"), ("reason", "bad_count"));
                        return;
                    }
                    director.DebugRolesBeaconCharge(count);
                    break;

                case "use":
                    director.DebugRolesBeaconUse();
                    break;

                default:
                    WLog.Line("cmd_error", secret: false, ("name", "beacon"), ("reason", "bad_op"), ("usage", Usage));
                    break;
            }
        }

        private static void HandlePerk(WerewolfDirector director, string[] args)
        {
            int i = args.Length >= 1 &&
                string.Equals(args[0], "unlock", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            string kind = args.Length > i ? args[i].ToLowerInvariant() : "";
            switch (kind)
            {
                case "stamina":
                    director.DebugRolesPerkUnlock(PerkId.InfiniteStamina);
                    break;
                case "jump":
                    director.DebugRolesPerkUnlock(PerkId.InfiniteJump);
                    break;
                case "ghost":
                    director.DebugRolesPerkUnlock(PerkId.EnemyIgnore);
                    break;
                case "heal":
                    director.DebugRolesPerkUnlock(PerkId.NaturalHeal);
                    break;
                case "all":
                    director.DebugRolesPerkUnlock(PerkId.InfiniteStamina);
                    director.DebugRolesPerkUnlock(PerkId.InfiniteJump);
                    director.DebugRolesPerkUnlock(PerkId.EnemyIgnore);
                    director.DebugRolesPerkUnlock(PerkId.NaturalHeal);
                    break;
                default:
                    WLog.Line("cmd_error", secret: false, ("name", "perk"), ("reason", "bad_perk"), ("usage", Usage));
                    break;
            }
        }

        private static void HandleCurse(WerewolfDirector director, string[] args)
        {
            int? target = null;
            if (args.Length >= 1)
            {
                if (!int.TryParse(args[0], out int actor))
                {
                    WLog.Line("cmd_error", secret: false, ("name", "curse"), ("reason", "bad_actor"));
                    return;
                }
                target = actor;
            }
            director.DebugRolesCurse(target);
        }

        private static void HandleBomb(WerewolfDirector director, string[] args)
        {
            string sub = args.Length >= 1 ? args[0].ToLowerInvariant() : "";
            switch (sub)
            {
                case "gauge":
                    director.DebugBomberFillGauge();
                    break;

                case "plant":
                    if (args.Length < 2 || !int.TryParse(args[1], out int target))
                    {
                        WLog.Line("cmd_error", secret: false,
                            ("name", "bomb"), ("sub", "plant"), ("reason", "bad_args"), ("usage", Usage));
                        return;
                    }
                    director.DebugBomberPlant(target);
                    break;

                case "detonate":
                    director.DebugBomberDetonate();
                    break;

                case "ammo":
                    int count = 1;
                    if (args.Length >= 2 && (!int.TryParse(args[1], out count) || count < 1))
                    {
                        WLog.Line("cmd_error", secret: false,
                            ("name", "bomb"), ("sub", "ammo"), ("reason", "bad_count"));
                        return;
                    }
                    director.DebugBomberGrantAmmo(count);
                    break;

                default:
                    WLog.Line("cmd_error", secret: false,
                        ("name", "bomb"), ("reason", "bad_op"), ("usage", Usage));
                    break;
            }
        }

        private static void HandleFx(WerewolfDirector director, string[] args)
        {
            string sub = args.Length >= 1 ? args[0].ToLowerInvariant() : "";
            string[] rest = args.Length >= 2 ? args[1..] : Array.Empty<string>();

            switch (sub)
            {
                case "reveal":
                    HandleFxReveal(director, rest);
                    break;

                case "toast":
                    director.DebugPlayToast(rest.Length >= 1 ? string.Join(" ", rest) : "テスト通知");
                    break;

                case "countdown":
                    HandleFxCountdown(director, rest);
                    break;

                case "result":
                    director.DebugPlayResult();
                    break;

                case "sfx":
                    HandleFxSfx(director, rest);
                    break;

                case "clear":
                    director.DebugClearFx();
                    break;

                default:
                    WLog.Line("cmd_error", secret: false, ("name", "fx"), ("reason", "bad_kind"), ("usage", Usage));
                    break;
            }
        }

        private static void HandleFxReveal(WerewolfDirector director, string[] rest)
        {
            Role role = Role.Werewolf;
            if (rest.Length >= 1)
            {
                switch (rest[0].ToLowerInvariant())
                {
                    case "villager": role = Role.Villager; break;
                    case "werewolf": role = Role.Werewolf; break;
                    case "blackcat": role = Role.BlackCat; break;
                    case "bomber": role = Role.Bomber; break;
                    case "shaman": role = Role.Shaman; break;
                    default:
                        WLog.Line("cmd_error", secret: false,
                            ("name", "fx"), ("reason", "bad_reveal_role"), ("arg", rest[0]));
                        return;
                }
            }
            director.DebugPlayReveal(role);
        }

        private static void HandleFxCountdown(WerewolfDirector director, string[] rest)
        {
            int seconds = 10;
            if (rest.Length >= 1 && (!int.TryParse(rest[0], out seconds) || seconds < 0))
            {
                WLog.Line("cmd_error", secret: false, ("name", "fx"), ("reason", "bad_countdown_sec"));
                return;
            }
            director.DebugPlayConveneCountdown("デバッグ", seconds);
        }

        private static void HandleFxSfx(WerewolfDirector director, string[] rest)
        {
            string kind = rest.Length >= 1 ? rest[0].ToLowerInvariant() : "toast";
            string clipKey;
            switch (kind)
            {
                case "countdown": clipKey = "sfx_countdown"; break;
                case "howl": clipKey = "sfx_howl"; break;
                case "toast": clipKey = NoticeSfx.DefaultClipKey; break;
                case "notice_convene": clipKey = NoticeSfx.ConveneStartedClipKey; break;
                case "bell": clipKey = "sfx_bell"; break;
                default:
                    WLog.Line("cmd_error", secret: false, ("name", "fx"), ("reason", "bad_sfx_kind"), ("arg", kind));
                    return;
            }
            director.DebugPlaySfx(clipKey);
        }

        private static void HandleCfg(WerewolfDirector director, string[] args)
        {
            string sub = args.Length >= 1 ? args[0].ToLowerInvariant() : "";
            switch (sub)
            {
                case "inject":
                    if (args.Length < 2)
                    {
                        WLog.Line("cmd_error", secret: false,
                            ("name", "cfg"), ("reason", "bad_args"), ("usage", Usage));
                        return;
                    }
                    string payload = string.Join(" ", args[1..]);
                    string blob;
                    if (LooksLikeVersionedBlob(payload))
                    {
                        blob = payload;
                    }
                    else
                    {
                        blob = SettingsCatalog.BlobVersion + "|" + payload;
                    }
                    director.DebugInjectCfgBlob(blob);
                    break;

                case "clear":
                    director.DebugClearCfgBlob();
                    break;

                default:
                    WLog.Line("cmd_error", secret: false,
                        ("name", "cfg"), ("reason", "bad_op"), ("usage", Usage));
                    break;
            }
        }

        private static void HandleLang(string[] args)
        {
            string sub = args.Length >= 1 ? args[0].ToLowerInvariant() : "";
            if (sub != "export")
            {
                WLog.Line("cmd_error", secret: false, ("name", "lang"), ("reason", "bad_op"), ("usage", Usage));
                return;
            }

            try
            {
                var dllPath = typeof(CheatCommands).Assembly.Location;
                var dir = string.IsNullOrEmpty(dllPath) ? null : Path.GetDirectoryName(dllPath);
                if (string.IsNullOrEmpty(dir))
                {
                    WLog.Line("cmd_error", secret: false, ("name", "lang"), ("reason", "no_dll_dir"));
                    return;
                }

                var langDir = Path.Combine(dir, "Lang");
                Directory.CreateDirectory(langDir);
                var path = Path.Combine(langDir, "template.txt");
                File.WriteAllText(path, Texts.ExportTemplate(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                WLog.Line("cmd_lang_export", secret: false, ("path", path));
            }
            catch (Exception e)
            {
                WLog.Line("cmd_error", secret: false,
                    ("name", "lang"), ("reason", "export_failed"), ("detail", e.GetType().Name));
            }
        }

        private static void HandleChat(WerewolfDirector director, string[] args)
        {
            string sub = args.Length >= 1 ? args[0].ToLowerInvariant() : "state";
            switch (sub)
            {
                case "say":
                    director.DebugInjectChat(director.DebugLocalActor, null, JoinFrom(args, 1, "テスト発言"));
                    break;

                case "as":
                    if (args.Length >= 2 && int.TryParse(args[1], out int asActor))
                    {
                        director.DebugInjectChat(asActor, null, JoinFrom(args, 2, "テスト発言"));
                    }
                    else
                    {
                        WLog.Line("cmd_error", secret: false, ("name", "chat as"), ("reason", "bad_actor"));
                    }
                    break;

                case "name":
                    if (args.Length >= 3 && int.TryParse(args[1], out int nameActor))
                    {
                        director.DebugInjectChat(nameActor, args[2], JoinFrom(args, 3, "テスト発言"));
                    }
                    else
                    {
                        WLog.Line("cmd_error", secret: false,
                            ("name", "chat name"), ("reason", "usage: /ww chat name <actor> <name> <text>"));
                    }
                    break;

                case "spam":
                    director.DebugSpamChat(args.Length >= 2 && int.TryParse(args[1], out int n) ? n : 20);
                    break;

                case "vote":
                    director.DebugInjectVoteLine(
                        args.Length >= 2 && int.TryParse(args[1], out int vActor)
                            ? vActor : director.DebugLocalActor);
                    break;

                case "baseline":
                    director.DebugArmVoteBaseline();
                    break;

                case "late":
                    director.DebugScheduleChat(
                        args.Length >= 2 && long.TryParse(args[1], out long ms) ? ms : 1000L,
                        JoinFrom(args, 2, "終了間際の発言"));
                    break;

                case "gate":
                    director.DebugToggleInputGateProbe();
                    break;

                case "avatar":
                    director.DebugToggleChatAvatarFallback();
                    break;

                case "clear":
                    director.DebugClearChat();
                    break;

                case "state":
                    director.DebugDumpChatState();
                    break;

                default:
                    WLog.Line("cmd_error", secret: false, ("name", "chat"), ("reason", "unknown_sub"), ("sub", sub));
                    break;
            }
        }

        private static string JoinFrom(string[] args, int from, string fallback)
        {
            if (args.Length <= from) return fallback;
            return string.Join(" ", args, from, args.Length - from);
        }

        private static void HandleRes(string[] args)
        {
            if (args.Length < 2
                || !int.TryParse(args[0], out int w) || !int.TryParse(args[1], out int h)
                || w < 640 || h < 360)
            {
                WLog.Line("cmd_error", secret: false,
                    ("name", "res"), ("reason", "usage: /ww res <width>=640.. <height>=360.."));
                return;
            }

            UnityEngine.Screen.SetResolution(w, h, UnityEngine.Screen.fullScreen);
            WLog.Line("cmd_res", secret: false,
                ("w", w), ("h", h), ("fullScreen", UnityEngine.Screen.fullScreen));
        }

        private static bool LooksLikeVersionedBlob(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            int bar = s.IndexOf('|');
            if (bar <= 0) return false;
            for (int i = 0; i < bar; i++)
            {
                if (s[i] < '0' || s[i] > '9') return false;
            }
            return true;
        }

        private static void HandleStart(WerewolfDirector director)
        {
            if (!SemiFunc.RunIsLevel() ||
                GameDirector.instance == null ||
                GameDirector.instance.currentState != GameDirector.gameState.Main)
            {
                WLog.Line("cmd_start", secret: false, ("result", "rejected"), ("reason", "not_in_level"));
                return;
            }

            StartResult result = director.StartHosted();
            if (result.Success)
            {
                WLog.Line("cmd_start", secret: false, ("result", "ok"));
            }
            else
            {
                WLog.Line("cmd_start", secret: false,
                    ("result", "rejected"), ("reason", result.Reason));
            }
        }
    }
}
