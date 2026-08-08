using System.Collections.Generic;
using System.Text;

namespace Werewolf.Core
{
    public readonly struct MeetingRecapData
    {
        public MeetingRecapData(IReadOnlyList<string> deathLabels,
                                int lostDollars, int extractedDollars, int haulGoalDollars, int beaconUses)
        {
            DeathLabels = deathLabels;
            LostDollars = lostDollars;
            ExtractedDollars = extractedDollars;
            HaulGoalDollars = haulGoalDollars;
            BeaconUses = beaconUses;
        }

        public IReadOnlyList<string> DeathLabels { get; }

        public int LostDollars { get; }

        public int ExtractedDollars { get; }

        public int HaulGoalDollars { get; }

        public int BeaconUses { get; }
    }

    public static class MeetingRecap
    {
        public const int Unknown = -1;

        public static int LostSince(int totalDollars, int baselineDollars)
        {
            if (totalDollars < 0) return Unknown;
            int delta = totalDollars - baselineDollars;
            return delta > 0 ? delta : 0;
        }

        public static List<string> BuildLines(MeetingRecapData data, bool emoji = false)
        {
            var lines = new List<string>(4);

            lines.Add(data.DeathLabels != null && data.DeathLabels.Count > 0
                ? ChatEmoji.Format(TextId.RecapDeathsFormat, emoji, JoinLabels(data.DeathLabels))
                : ChatEmoji.Get(TextId.RecapDeathsNone, emoji));

            if (data.LostDollars >= 0)
            {
                lines.Add(ChatEmoji.Format(TextId.RecapLostFormat, emoji, data.LostDollars));
            }

            if (data.ExtractedDollars >= 0 && data.HaulGoalDollars >= 0)
            {
                lines.Add(ChatEmoji.Format(TextId.RecapHaulFormat, emoji,
                    data.ExtractedDollars, data.HaulGoalDollars));
            }

            if (data.BeaconUses >= 0)
            {
                lines.Add(data.BeaconUses > 0
                    ? ChatEmoji.Format(TextId.RecapBeaconFormat, emoji, data.BeaconUses)
                    : ChatEmoji.Get(TextId.RecapBeaconNone, emoji));
            }

            return lines;
        }

        private static string JoinLabels(IReadOnlyList<string> labels)
        {
            string separator = Texts.Get(TextId.RecapNameSeparator);
            var sb = new StringBuilder();
            for (int i = 0; i < labels.Count; i++)
            {
                if (string.IsNullOrEmpty(labels[i])) continue;
                if (sb.Length > 0) sb.Append(separator);
                sb.Append(MeetingChatLog.Sanitize(labels[i], ParticipantLabel.MaxLength));
            }
            return sb.ToString();
        }
    }
}
