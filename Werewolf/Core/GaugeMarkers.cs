using System.Collections.Generic;

namespace Werewolf.Core
{
    public enum GaugeMarkerKind : byte
    {
        Perk = 0,

        Scale = 1,
    }

    public readonly struct GaugeMarker
    {
        public int Pct { get; }

        public GaugeMarkerKind Kind { get; }

        public string IconKey { get; }

        public string Label { get; }

        public int Tier { get; }

        public bool Unlocked { get; }

        public GaugeMarker(int pct, GaugeMarkerKind kind, string iconKey, string label, int tier, bool unlocked)
        {
            Pct = pct;
            Kind = kind;
            IconKey = iconKey;
            Label = label;
            Tier = tier;
            Unlocked = unlocked;
        }
    }

    public static class GaugeMarkerLayout
    {
        public static List<GaugeMarker> Build(MeetingGaugeSnapshot s, int permille, int minGapPct)
        {
            var markers = new List<GaugeMarker>();
            if (s == null) return markers;

            var perks = new List<KeyValuePair<int, string[]>>();
            InsertSorted(perks, s.StaminaPct, "perk_stamina", Texts.Get(TextId.GaugePerkStaminaLabel));
            InsertSorted(perks, s.JumpPct, "perk_jump", Texts.Get(TextId.GaugePerkJumpLabel));
            InsertSorted(perks, s.EnemyIgnorePct, "perk_enemy_ignore", Texts.Get(TextId.GaugePerkEnemyIgnoreLabel));
            InsertSorted(perks, s.HealPct, "perk_heal", Texts.Get(TextId.GaugePerkHealLabel));
            InsertSorted(perks, s.InformantPct, "perk_informant", Texts.Get(TextId.GaugePerkInformantLabel));

            int lastPctTier0 = -1000;
            int lastPctTier1 = -1000;
            foreach (KeyValuePair<int, string[]> p in perks)
            {
                int tier;
                if (p.Key - lastPctTier0 >= minGapPct)
                {
                    tier = 0;
                    lastPctTier0 = p.Key;
                }
                else if (p.Key - lastPctTier1 >= minGapPct)
                {
                    tier = 1;
                    lastPctTier1 = p.Key;
                }
                else
                {
                    tier = 0;
                    lastPctTier0 = p.Key;
                }
                markers.Add(new GaugeMarker(p.Key, GaugeMarkerKind.Perk,
                    p.Value[0], p.Value[1], tier, unlocked: permille >= p.Key * 10));
            }

            for (int pct = 10; pct <= 90; pct += 10)
            {
                markers.Add(new GaugeMarker(pct, GaugeMarkerKind.Scale,
                    iconKey: null, label: null, tier: 0, unlocked: false));
            }
            return markers;
        }

        public static int UnlockedCount(MeetingGaugeSnapshot s, int permille)
        {
            if (s == null) return 0;
            return UnlockedIf(s.StaminaPct, permille)
                 + UnlockedIf(s.JumpPct, permille)
                 + UnlockedIf(s.EnemyIgnorePct, permille)
                 + UnlockedIf(s.HealPct, permille)
                 + UnlockedIf(s.InformantPct, permille);
        }

        private static int UnlockedIf(int pct, int permille)
            => pct >= 1 && pct <= 100 && permille >= pct * 10 ? 1 : 0;

        private static void InsertSorted(List<KeyValuePair<int, string[]>> entries, int pct, string iconKey, string label)
        {
            if (pct < 1 || pct > 100) return;
            int index = entries.Count;
            while (index > 0 && entries[index - 1].Key > pct) index--;
            entries.Insert(index, new KeyValuePair<int, string[]>(pct, new[] { iconKey, label }));
        }
    }
}
