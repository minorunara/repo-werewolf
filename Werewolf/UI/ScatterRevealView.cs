using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class ScatterRevealView
    {
        private const long ScatterIntroMs = 900;
        private const long ScatterShuffleMs = 3200;
        private const long ScatterFadeMs = 700;
        private const long ScatterSwapIntervalMs = 90;
        private const long ScatterSettledReadMs = 4400;

        public const long HoldRequiredMs =
            ScatterIntroMs + ScatterShuffleMs + ScatterFadeMs + ScatterSettledReadMs;

        private readonly List<VoteRow> _rows;
        private readonly Func<RectTransform> _resolvePanelRoot;
        private readonly Color _sloganTextColor;

        private bool _scatterActive;
        private bool _scatterSettled;
        private bool _scatterMusicStopped;
        private long _scatterStartUnixMs;
        private long _scatterNextSwapUnixMs;
        private readonly Dictionary<int, char> _scatterLetterByActor = new Dictionary<int, char>();
        private char[] _scatterLetters = Array.Empty<char>();
        private readonly System.Random _scatterRng = new System.Random();
        private Action<float> _scatterPumpMusic;
        private Action _scatterStopMusic;
        private Action _scatterPlayJingle;
        private GameObject _scatterSloganRoot;

        public ScatterRevealView(List<VoteRow> rows, Func<RectTransform> resolvePanelRoot, Color sloganTextColor)
        {
            _rows = rows;
            _resolvePanelRoot = resolvePanelRoot;
            _sloganTextColor = sloganTextColor;
        }

        public bool IsActive => _scatterActive;

        public bool Start(List<List<int>> groups, long nowUnixMs,
            Action<float> pumpMusic, Action stopMusic, Action playJingle)
        {
            if (_resolvePanelRoot() == null || _rows.Count == 0) return false;
            if (groups == null || groups.Count < 2) return false;

            _scatterLetterByActor.Clear();
            var letters = new List<char>(groups.Count);
            for (int g = 0; g < groups.Count; g++)
            {
                char letter = (char)('A' + g);
                letters.Add(letter);
                foreach (int actor in groups[g]) _scatterLetterByActor[actor] = letter;
            }

            bool anyRow = false;
            foreach (VoteRow row in _rows)
            {
                if (_scatterLetterByActor.ContainsKey(row.ActorNumber)) { anyRow = true; break; }
            }
            if (!anyRow)
            {
                _scatterLetterByActor.Clear();
                return false;
            }

            _scatterLetters = letters.ToArray();
            _scatterActive = true;
            _scatterSettled = false;
            _scatterMusicStopped = false;
            _scatterStartUnixMs = nowUnixMs;
            _scatterNextSwapUnixMs = 0;
            _scatterPumpMusic = pumpMusic;
            _scatterStopMusic = stopMusic;
            _scatterPlayJingle = playJingle;

            EnsureSloganBuilt();
            if (_scatterSloganRoot != null) _scatterSloganRoot.SetActive(true);

            string unknown = Texts.Get(TextId.VoteScatterBadgeUnknown);
            foreach (VoteRow row in _rows)
            {
                if (_scatterLetterByActor.ContainsKey(row.ActorNumber))
                {
                    row.SetScatterBadge(unknown, settled: false);
                }
            }

            WLog.Line("vote_panel_scatter_reveal", secret: false,
                ("groups", groups.Count), ("actors", _scatterLetterByActor.Count));
            return true;
        }

        public void Tick(long nowUnixMs)
        {
            if (!_scatterActive) return;
            long elapsed = nowUnixMs - _scatterStartUnixMs;

            if (elapsed < ScatterIntroMs)
            {
                _scatterPumpMusic?.Invoke(1f);
                return;
            }

            if (elapsed < ScatterIntroMs + ScatterShuffleMs)
            {
                _scatterPumpMusic?.Invoke(1f);
                if (nowUnixMs < _scatterNextSwapUnixMs) return;
                _scatterNextSwapUnixMs = nowUnixMs + ScatterSwapIntervalMs;
                foreach (VoteRow row in _rows)
                {
                    if (!_scatterLetterByActor.ContainsKey(row.ActorNumber)) continue;
                    char spin = _scatterLetters[_scatterRng.Next(_scatterLetters.Length)];
                    row.SetScatterBadge(Texts.Format(TextId.VoteScatterBadgeFormat, spin), settled: false);
                }
                return;
            }

            if (!_scatterSettled)
            {
                _scatterSettled = true;
                foreach (VoteRow row in _rows)
                {
                    if (_scatterLetterByActor.TryGetValue(row.ActorNumber, out char letter))
                    {
                        row.SetScatterBadge(Texts.Format(TextId.VoteScatterBadgeFormat, letter), settled: true);
                    }
                }
                _scatterPlayJingle?.Invoke();
                WLog.Line("vote_panel_scatter_settled", secret: false);
            }

            if (_scatterMusicStopped) return;
            long fadeElapsed = elapsed - ScatterIntroMs - ScatterShuffleMs;
            if (fadeElapsed >= ScatterFadeMs)
            {
                _scatterMusicStopped = true;
                _scatterStopMusic?.Invoke();
            }
            else
            {
                _scatterPumpMusic?.Invoke(1f - fadeElapsed / (float)ScatterFadeMs);
            }
        }

        public void Stop()
        {
            if (_scatterActive && !_scatterMusicStopped) _scatterStopMusic?.Invoke();
            if (_scatterActive)
            {
                foreach (VoteRow row in _rows) row.SetScatterBadge(null, settled: false);
            }
            _scatterActive = false;
            _scatterSettled = false;
            _scatterMusicStopped = false;
            _scatterLetterByActor.Clear();
            _scatterPumpMusic = null;
            _scatterStopMusic = null;
            _scatterPlayJingle = null;
            if (_scatterSloganRoot != null) _scatterSloganRoot.SetActive(false);
        }

        public void OnPanelDestroy()
        {
            _scatterSloganRoot = null;
        }

        private void EnsureSloganBuilt()
        {
            RectTransform rootRect = _resolvePanelRoot();
            if (_scatterSloganRoot != null || rootRect == null) return;
            RectTransform slogan = UiKit.CreateRect(rootRect, "ScatterSlogan",
                new Vector2(0f, 284f), new Vector2(520f, 32f));
            _scatterSloganRoot = slogan.gameObject;
            string text = Texts.Get(TextId.NoticeScatterSlogan);
            TextMeshProUGUI label = UiKit.CreateText(slogan, "Label", Vector2.zero,
                new Vector2(520f, 32f), text, 24f, _sloganTextColor, TextAlignmentOptions.Center);
            Sprite wink = AssetCatalog.GetSprite("img_taxman_wink")
                ?? AssetCatalog.GetSprite("img_taxman_nodeath");
            if (wink != null)
            {
                const float iconSize = 26f;
                const float gap = 4f;
                float textWidth = label.GetPreferredValues(text).x;
                label.rectTransform.anchoredPosition = new Vector2(-(gap + iconSize) / 2f, 0f);
                Image icon = UiKit.CreateImage(slogan, "Icon",
                    new Vector2(textWidth / 2f + gap / 2f, 0f),
                    new Vector2(iconSize, iconSize), Color.white);
                icon.sprite = wink;
                icon.preserveAspect = true;
            }
            _scatterSloganRoot.SetActive(false);
        }
    }
}
