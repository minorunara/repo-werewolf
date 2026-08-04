using System.Collections.Generic;
using System.Text;

namespace Werewolf.Core
{
    public readonly struct MeetingRecapData
    {
        public MeetingRecapData(IReadOnlyList<string> deathNames,
                                int lostDollars, int extractedDollars, int haulGoalDollars, int beaconUses)
        {
            DeathNames = deathNames;
            LostDollars = lostDollars;
            ExtractedDollars = extractedDollars;
            HaulGoalDollars = haulGoalDollars;
            BeaconUses = beaconUses;
        }

        public IReadOnlyList<string> DeathNames { get; }

        public int LostDollars { get; }

        public int ExtractedDollars { get; }

        public int HaulGoalDollars { get; }

        public int BeaconUses { get; }

        public long Signature
        {
            get
            {
                unchecked
                {
                    long h = 17;
                    h = h * 31 + (DeathNames?.Count ?? 0);
                    h = h * 31 + LostDollars;
                    h = h * 31 + ExtractedDollars;
                    h = h * 31 + HaulGoalDollars;
                    h = h * 31 + BeaconUses;
                    return h;
                }
            }
        }
    }

    public static class MeetingRecap
    {
        public const int Unknown = -1;

        public static List<string> BuildLines(MeetingRecapData data)
        {
            var lines = new List<string>(4);

            lines.Add(data.DeathNames != null && data.DeathNames.Count > 0
                ? Texts.Format(TextId.RecapDeathsFormat, JoinNames(data.DeathNames))
                : Texts.Get(TextId.RecapDeathsNone));

            if (data.LostDollars >= 0)
            {
                lines.Add(Texts.Format(TextId.RecapLostFormat, data.LostDollars));
            }

            if (data.ExtractedDollars >= 0 && data.HaulGoalDollars >= 0)
            {
                lines.Add(Texts.Format(TextId.RecapHaulFormat, data.ExtractedDollars, data.HaulGoalDollars));
            }

            if (data.BeaconUses >= 0)
            {
                lines.Add(data.BeaconUses > 0
                    ? Texts.Format(TextId.RecapBeaconFormat, data.BeaconUses)
                    : Texts.Get(TextId.RecapBeaconNone));
            }

            return lines;
        }

        private static string JoinNames(IReadOnlyList<string> names)
        {
            string separator = Texts.Get(TextId.RecapNameSeparator);
            var sb = new StringBuilder();
            for (int i = 0; i < names.Count; i++)
            {
                if (string.IsNullOrEmpty(names[i])) continue;
                if (sb.Length > 0) sb.Append(separator);
                sb.Append(MeetingChatLog.Sanitize(names[i], MeetingChatLog.MaxNameLength));
            }
            return sb.ToString();
        }
    }
}
