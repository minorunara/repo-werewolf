using System;
using System.Collections.Generic;
using System.Text;

namespace Werewolf.Core
{
    public enum RoleIcon : byte
    {
        Villager = 0,
        Werewolf = 1,
        BlackCat = 2,
        Bomber = 3,
        Shaman = 4,
    }

    public sealed class RevealPage
    {
        public string[] BodyLines;

        public float HoldSec;
    }

    public sealed class RevealContent
    {
        public string Title;

        public string SelfIdLine;

        public RoleIcon Icon;

        public RevealPage[] Pages;

        public float FadeInSec;
        public float FadeOutSec;
    }

    public static class RevealScript
    {
        private const int MaxLineLength = 26;

        private static string TeammatePrefix => Texts.Get(TextId.RevealTeammatePrefix);
        private const string TeammateSeparator = ",";

        private const string Bullet = "・";

        private const float DefaultFadeInSec = 1.0f;
        private const float DefaultFadeOutSec = 1.0f;

        private const float TeamPageHoldSec = 5.0f;

        private const float AbilityPageHoldSec = 5.0f;

        private const float SinglePageHoldSec = 5.0f;

        public static RevealContent Build(Role selfRole, IReadOnlyList<string> teammateNames, bool blackCatPossible,
            ValuableMapMode valuableMapMode, bool blackCatCurseEnabled = true, int selfParticipantId = 0)
        {
            RevealContent content;
            switch (selfRole)
            {
                case Role.Villager:
                    content = BuildVillager(blackCatPossible, valuableMapMode);
                    break;
                case Role.Werewolf:
                    content = BuildWolfTeam(
                        Texts.Get(TextId.RevealWerewolfTitle), RoleIcon.Werewolf, teammateNames,
                        TextId.RevealWolfAbility1, TextId.RevealWolfAbility2, TextId.RevealWolfAbility3);
                    break;
                case Role.BlackCat:
                    content = BuildBlackCat(blackCatCurseEnabled);
                    break;
                case Role.Bomber:
                    content = BuildWolfTeam(
                        Texts.Get(TextId.RevealBomberTitle), RoleIcon.Bomber, teammateNames,
                        TextId.RevealBomberAbility1, TextId.RevealBomberAbility2, TextId.RevealBomberAbility3);
                    break;
                case Role.Shaman:
                    content = BuildShaman();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(selfRole), selfRole, "未知の役職です。");
            }

            content.SelfIdLine = selfParticipantId > 0
                ? Texts.Format(TextId.RevealSelfIdFormat, selfParticipantId)
                : null;
            return content;
        }

        private static List<string> WolfWinConditionLines()
        {
            return new List<string>
            {
                Texts.Get(TextId.RevealHeadingWinCondition),
                Bullet + Texts.Get(TextId.RevealWolfTeamWinCondition1),
                Bullet + Texts.Get(TextId.RevealWolfTeamWinCondition2),
                Bullet + Texts.Get(TextId.RevealWolfTeamWinCondition3),
            };
        }

        private static RevealContent BuildVillager(bool blackCatPossible, ValuableMapMode valuableMapMode)
        {
            TextId tip3 = valuableMapMode == ValuableMapMode.MeetingSync
                ? TextId.RevealVillagerTipValuableMap
                : TextId.RevealVillagerTipAliveCheck;

            return new RevealContent
            {
                Title = blackCatPossible
                    ? Texts.Get(TextId.RevealVillagerTitleMaybeCat)
                    : Texts.Get(TextId.RevealVillagerTitle),
                Icon = RoleIcon.Villager,
                Pages = new[]
                {
                    new RevealPage
                    {
                        BodyLines = new[]
                        {
                            Texts.Get(TextId.RevealHeadingWinCondition),
                            Bullet + Texts.Get(TextId.RevealVillagerWinCondition1),
                            Bullet + Texts.Get(TextId.RevealVillagerWinCondition2),
                        },
                        HoldSec = TeamPageHoldSec,
                    },
                    new RevealPage
                    {
                        BodyLines = new[]
                        {
                            Texts.Get(TextId.RevealHeadingTips),
                            Bullet + Texts.Get(TextId.RevealVillagerTipConvene),
                            Bullet + Texts.Get(TextId.RevealVillagerTipReport),
                            Bullet + Texts.Get(tip3),
                            string.Empty,
                            Texts.Get(TextId.RevealVillagerFlavor),
                        },
                        HoldSec = AbilityPageHoldSec,
                    },
                },
                FadeInSec = DefaultFadeInSec,
                FadeOutSec = DefaultFadeOutSec,
            };
        }

        private static RevealContent BuildWolfTeam(
            string title, RoleIcon icon, IReadOnlyList<string> teammateNames,
            TextId ability1, TextId ability2, TextId ability3)
        {
            var teamLines = new List<string>();
            teamLines.AddRange(WrapTeammateLines(teammateNames));
            teamLines.AddRange(WolfWinConditionLines());

            return new RevealContent
            {
                Title = title,
                Icon = icon,
                Pages = new[]
                {
                    new RevealPage { BodyLines = teamLines.ToArray(), HoldSec = TeamPageHoldSec },
                    new RevealPage
                    {
                        BodyLines = new[]
                        {
                            Texts.Get(TextId.RevealHeadingAbility),
                            Bullet + Texts.Get(ability1),
                            Bullet + Texts.Get(ability2),
                            Bullet + Texts.Get(ability3),
                        },
                        HoldSec = AbilityPageHoldSec,
                    },
                },
                FadeInSec = DefaultFadeInSec,
                FadeOutSec = DefaultFadeOutSec,
            };
        }

        private static RevealContent BuildShaman()
        {
            return new RevealContent
            {
                Title = Texts.Get(TextId.RevealShamanTitle),
                Icon = RoleIcon.Shaman,
                Pages = new[]
                {
                    new RevealPage
                    {
                        BodyLines = new[]
                        {
                            Texts.Get(TextId.RevealHeadingWinCondition),
                            Bullet + Texts.Get(TextId.RevealVillagerWinCondition1),
                            Bullet + Texts.Get(TextId.RevealVillagerWinCondition2),
                        },
                        HoldSec = TeamPageHoldSec,
                    },
                    new RevealPage
                    {
                        BodyLines = new[]
                        {
                            Texts.Get(TextId.RevealHeadingAbility),
                            Bullet + Texts.Get(TextId.RevealShamanAbility1),
                            Bullet + Texts.Get(TextId.RevealShamanAbility2),
                        },
                        HoldSec = AbilityPageHoldSec,
                    },
                },
                FadeInSec = DefaultFadeInSec,
                FadeOutSec = DefaultFadeOutSec,
            };
        }

        public static RevealContent BuildBlackCatAwakening(bool blackCatCurseEnabled = true)
        {
            return new RevealContent
            {
                Title = Texts.Get(TextId.RevealBlackCatAwakeningTitle),
                Icon = RoleIcon.BlackCat,
                Pages = new[]
                {
                    new RevealPage
                    {
                        BodyLines = new[]
                        {
                            Texts.Get(blackCatCurseEnabled
                                ? TextId.RevealBlackCatAbility
                                : TextId.RevealBlackCatNoCurse),
                        },
                        HoldSec = SinglePageHoldSec,
                    },
                },
                FadeInSec = DefaultFadeInSec,
                FadeOutSec = DefaultFadeOutSec,
            };
        }

        private static RevealContent BuildBlackCat(bool blackCatCurseEnabled)
        {
            return new RevealContent
            {
                Title = Texts.Get(TextId.RevealBlackCatTitle),
                Icon = RoleIcon.BlackCat,
                Pages = new[]
                {
                    new RevealPage { BodyLines = WolfWinConditionLines().ToArray(), HoldSec = TeamPageHoldSec },
                    new RevealPage
                    {
                        BodyLines = new[]
                        {
                            Texts.Get(TextId.RevealHeadingAbility),
                            Bullet + Texts.Get(blackCatCurseEnabled
                                ? TextId.RevealBlackCatAbility
                                : TextId.RevealBlackCatNoCurse),
                        },
                        HoldSec = AbilityPageHoldSec,
                    },
                },
                FadeInSec = DefaultFadeInSec,
                FadeOutSec = DefaultFadeOutSec,
            };
        }

        private static List<string> WrapTeammateLines(IReadOnlyList<string> teammateNames)
        {
            var lines = new List<string>();
            var current = new StringBuilder(TeammatePrefix);
            bool currentHasName = false;

            if (teammateNames != null)
            {
                foreach (var name in teammateNames)
                {
                    if (string.IsNullOrEmpty(name)) continue;

                    string segment = currentHasName ? TeammateSeparator + name : name;

                    if (currentHasName && current.Length + segment.Length > MaxLineLength)
                    {
                        lines.Add(current.ToString());
                        current = new StringBuilder(name);
                        currentHasName = true;
                    }
                    else
                    {
                        current.Append(segment);
                        currentHasName = true;
                    }
                }
            }

            lines.Add(current.ToString());
            return lines;
        }
    }
}
