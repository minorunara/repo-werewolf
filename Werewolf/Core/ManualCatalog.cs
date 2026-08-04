namespace Werewolf.Core
{
    public sealed class ManualBlock
    {
        public string IconKey;

        public TextId BodyId;

        public static ManualBlock Text(TextId bodyId)
            => new ManualBlock { IconKey = null, BodyId = bodyId };

        public static ManualBlock Icon(string iconKey, TextId bodyId)
            => new ManualBlock { IconKey = iconKey, BodyId = bodyId };
    }

    public sealed class ManualPage
    {
        public TextId TitleId;

        public ManualBlock[] Blocks;

        public string ImageKey;

        public float ImageHeight;

        public float ImageWidth;

        public ManualPage(TextId titleId, TextId bodyId, string imageKey = null, float imageHeight = 0f, float imageWidth = 0f)
            : this(titleId, new[] { ManualBlock.Text(bodyId) }, imageKey, imageHeight, imageWidth)
        {
        }

        public ManualPage(TextId titleId, ManualBlock[] blocks, string imageKey = null, float imageHeight = 0f, float imageWidth = 0f)
        {
            TitleId = titleId;
            Blocks = blocks;
            ImageKey = imageKey;
            ImageHeight = imageHeight;
            ImageWidth = imageWidth;
        }
    }

    public sealed class ManualSection
    {
        public TextId TitleId;
        public int StartIndex;

        public ManualSection(TextId titleId, int startIndex)
        {
            TitleId = titleId;
            StartIndex = startIndex;
        }
    }

    public static class ManualCatalog
    {
        private const float RoleIconHeight = 320f;

        private const float GaugeImageWidth = 1000f;
        private const float GaugeImageHeight = 128f;

        private const float MeetingButtonImageWidth = 426f;
        private const float MeetingButtonImageHeight = 229f;

        private const float VotePanelImageWidth = 772f;
        private const float VotePanelImageHeight = 264f;

        public static readonly ManualPage[] Pages =
        {
            new ManualPage(TextId.ManualWelcomeTitle, TextId.ManualWelcomeBody),
            new ManualPage(TextId.ManualGameFlowTitle, TextId.ManualGameFlowBody),
            new ManualPage(TextId.ManualVillagerWinTitle, TextId.ManualVillagerWinBody),
            new ManualPage(TextId.ManualWerewolfWinTitle, TextId.ManualWerewolfWinBody),
            new ManualPage(TextId.ManualValuablesMapTitle, TextId.ManualValuablesMapBody, "manual_map_markers"),
            new ManualPage(TextId.ManualValuableRecordTitle, new[]
            {
                ManualBlock.Icon("icon_valuable_record", TextId.ManualValuableRecordBody),
                ManualBlock.Text(TextId.ManualValuableRecordToggle),
            }),
            new ManualPage(TextId.ManualCombatTitle, TextId.ManualCombatBody),
            new ManualPage(TextId.ManualEndgamePrepTitle, TextId.ManualEndgamePrepBody),
            new ManualPage(TextId.ManualCorpseTitle, new[]
            {
                ManualBlock.Icon("icon_host_megaphone", TextId.ManualCorpseBody),
            }),
            new ManualPage(TextId.ManualConveneTitle, TextId.ManualConveneBody, "manual_meeting_button",
                MeetingButtonImageHeight, MeetingButtonImageWidth),
            new ManualPage(TextId.ManualMeetingFlowTitle, TextId.ManualMeetingFlowBody, "manual_death_reveal"),
            new ManualPage(TextId.ManualVotingTitle, TextId.ManualVotingBody, "manual_vote_panel",
                VotePanelImageHeight, VotePanelImageWidth),
            new ManualPage(TextId.ManualGaugeBasicsTitle, new[]
            {
                ManualBlock.Text(TextId.ManualGaugeIntro),
                ManualBlock.Text(TextId.ManualGaugeLoss),
                ManualBlock.Text(TextId.ManualGaugeDelivery),
                ManualBlock.Text(TextId.ManualGaugeLines),
            }, "manual_gauge_overview", GaugeImageHeight, GaugeImageWidth),
            new ManualPage(TextId.ManualRoleVillagerTitle, TextId.ManualRoleVillagerBody, "role_villager", RoleIconHeight),
            new ManualPage(TextId.ManualRoleShamanTitle, TextId.ManualRoleShamanIntro, "role_shaman", RoleIconHeight),
            new ManualPage(TextId.ManualShamanSenseTitle, new[]
            {
                ManualBlock.Text(TextId.ManualShamanGhost),
                ManualBlock.Text(TextId.ManualShamanStorm),
            }),
            new ManualPage(TextId.ManualRoleWerewolfTitle, new[]
            {
                ManualBlock.Text(TextId.ManualRoleWerewolfIntro),
                ManualBlock.Text(TextId.ManualRoleWerewolfEnemyMap),
            }, "role_werewolf", RoleIconHeight),
            new ManualPage(TextId.ManualRoleWerewolfPerksTitle, new[]
            {
                ManualBlock.Icon("perk_stamina", TextId.ManualRoleWerewolfPerkStamina),
                ManualBlock.Icon("perk_jump", TextId.ManualRoleWerewolfPerkJump),
            }),
            new ManualPage(TextId.ManualRoleWerewolfPerksTitle2, new[]
            {
                ManualBlock.Icon("perk_enemy_ignore", TextId.ManualRoleWerewolfPerkEnemyIgnore),
                ManualBlock.Icon("perk_heal", TextId.ManualRoleWerewolfPerkHeal),
                ManualBlock.Text(TextId.ManualRoleWerewolfPerkToggle),
            }),
            new ManualPage(TextId.ManualRoleWerewolfBeaconTitle, TextId.ManualRoleWerewolfBeaconBody, "perk_beacon"),
            new ManualPage(TextId.ManualRoleBlackCatTitle, TextId.ManualRoleBlackCatIntro, "role_blackcat", RoleIconHeight),
            new ManualPage(TextId.ManualBlackCatInformantTitle, new[]
            {
                ManualBlock.Text(TextId.ManualRoleBlackCatInformant),
                ManualBlock.Text(TextId.ManualRoleBlackCatGaugeNote),
            }, "perk_informant"),
            new ManualPage(TextId.ManualBlackCatCounterTitle, TextId.ManualBlackCatCounterBody),
            new ManualPage(TextId.ManualRoleBomberTitle, TextId.ManualRoleBomberIntro, "role_bomber", RoleIconHeight),
            new ManualPage(TextId.ManualBomberPlantTitle, TextId.ManualRoleBomberPlant, "perk_bomb_plant"),
            new ManualPage(TextId.ManualRoleBomberDetonateTitle, TextId.ManualRoleBomberDetonateBody, "perk_bomb_detonate"),
            new ManualPage(TextId.ManualAfterDeathTitle, TextId.ManualAfterDeathBody),
        };

        public static readonly ManualSection[] Sections =
        {
            new ManualSection(TextId.ManualSectionBasics, 0),
            new ManualSection(TextId.ManualSectionExploration, 4),
            new ManualSection(TextId.ManualSectionMeeting, 8),
            new ManualSection(TextId.ManualSectionGauge, 12),
            new ManualSection(TextId.ManualSectionVillager, 13),
            new ManualSection(TextId.ManualSectionShaman, 14),
            new ManualSection(TextId.ManualSectionWerewolf, 16),
            new ManualSection(TextId.ManualSectionBlackCat, 20),
            new ManualSection(TextId.ManualSectionBomber, 23),
            new ManualSection(TextId.ManualSectionAfterDeath, 26),
        };

        public static int PageCount => Pages.Length;

        public static int SectionIndexForPage(int pageIndex)
        {
            int page = ClampIndex(pageIndex);
            for (int i = Sections.Length - 1; i >= 0; i--)
            {
                if (page >= Sections[i].StartIndex) return i;
            }
            return 0;
        }

        public static int PreviousSectionStart(int pageIndex)
        {
            int section = SectionIndexForPage(pageIndex);
            return Sections[section > 0 ? section - 1 : 0].StartIndex;
        }

        public static int NextSectionStart(int pageIndex)
        {
            int section = SectionIndexForPage(pageIndex);
            return Sections[section + 1 < Sections.Length ? section + 1 : section].StartIndex;
        }

        public static int ClampIndex(int index)
        {
            if (index < 0) return 0;
            if (index >= Pages.Length) return Pages.Length - 1;
            return index;
        }
    }
}
