using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class DeathRevealPanel : IClientPanel
    {
        public string LayerName => "DeathReveal";

        private const float GridTopY = -80f;
        private static readonly Vector2 IconSize = new Vector2(280f, 280f);

        private const float ExitRiseY = 700f;

        private const float TitleFontSize = 120f;
        private const float GuardTitleFontSizeMin = 56f;

        private static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.75f);

        private GameObject _root;
        private CanvasGroup _group;
        private RectTransform _content;
        private Image _iconImage;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _titleTextBack;
        private readonly List<VoteRow> _rows = new List<VoteRow>();

        public bool Exists => _root != null;

        public bool Visible => _root != null && _root.activeSelf;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;

            RectTransform rect = RevealPanelKit.BuildFullscreenRoot(
                layerRoot, "WW_DeathReveal", BackdropColor, out _group);
            _root = rect.gameObject;

            _content = UiKit.CreateRect(rect, "Content", Vector2.zero, new Vector2(1280f, 900f));

            _iconImage = UiKit.CreateImage(_content, "Icon", new Vector2(0f, 260f), IconSize, Color.white);
            _iconImage.enabled = false;

            _titleText = RevealPanelKit.CreateStampTitle(_content, new Vector2(0f, 40f),
                new Vector2(1200f, 140f), "", TitleFontSize, Color.black, Color.red, 0.15f,
                out _titleTextBack);

            _root.SetActive(false);
            WLog.Line("death_reveal_built", secret: false);
        }

        public void Show(IReadOnlyList<WPlayer> deadRoster, Func<int, PlayerAvatar> resolveAvatar,
                         ConveneKind kind = ConveneKind.Button)
        {
            if (_root == null) return;
            ClearRows();

            bool hasDeaths = deadRoster != null && deadRoster.Count > 0;
            bool guard = kind == ConveneKind.ScatterGuard && hasDeaths;
            string title = Texts.Get(guard ? TextId.DeathRevealScatterGuardTitle
                : hasDeaths ? TextId.DeathRevealTitle : TextId.DeathRevealNone);
            ApplyTitle(_titleText, title, autoShrink: guard);
            ApplyTitle(_titleTextBack, title, autoShrink: guard);

            Sprite icon = guard
                ? (AssetCatalog.GetSprite("img_taxman_handover") ?? AssetCatalog.GetSprite("img_taxman_death"))
                : AssetCatalog.GetSprite(hasDeaths ? "img_taxman_death" : "img_taxman_nodeath");
            RevealPanelKit.SetIcon(_iconImage, icon);

            if (hasDeaths)
            {
                for (int i = 0; i < deadRoster.Count; i++)
                {
                    WPlayer player = deadRoster[i];
                    if (player == null) continue;
                    VoteRow row = VoteRow.Build(_content, player, resolveAvatar, VoteRowGrid.RowSize);
                    RectTransform rowRect = row.Root;
                    if (rowRect != null)
                    {
                        if (deadRoster.Count == 1)
                        {
                            rowRect.anchoredPosition = new Vector2(0f, GridTopY);
                        }
                        else
                        {
                            rowRect.anchoredPosition = VoteRowGrid.Position(
                                i % VoteRowGrid.Columns,
                                Math.Min(i / VoteRowGrid.Columns, VoteRowGrid.RowsPerColumn - 1),
                                GridTopY);
                        }
                    }
                    _rows.Add(row);
                }
            }
        }

        public IEnumerator Play(Action onImpact)
        {
            if (_root == null)
            {
                WLog.Line("death_reveal_skip", secret: false, ("reason", "not_built"));
                yield break;
            }

            _group.alpha = 0f;
            _content.localScale = Vector3.one * RevealPanelKit.EntranceStartScale;
            _content.anchoredPosition = Vector2.zero;
            _root.SetActive(true);
            WLog.Line("death_reveal_show", secret: false, ("rows", _rows.Count));

            yield return RevealPanelKit.StampEntrance(_content, _group, DeathReveal.EntranceSec);

            RevealPanelKit.InvokeSfx(onImpact, "death_reveal_impact_error");

            yield return UiTween.Hold(DeathReveal.HoldSec);

            yield return UiTween.Parallel(
                UiTween.Move(_content, Vector2.zero, new Vector2(0f, ExitRiseY), DeathReveal.ExitSec, UiTween.EaseIn()),
                UiTween.Fade(_group, 1f, 0f, DeathReveal.ExitSec, UiTween.EaseIn()));

            HideNow();
            WLog.Line("death_reveal_end", secret: false);
        }

        public void Tick()
        {
            if (!Visible) return;
            foreach (VoteRow row in _rows) row.Tick();
        }

        public void HideNow()
        {
            ClearRows();
            if (_root == null) return;
            _group.alpha = 0f;
            _content.localScale = Vector3.one;
            _content.anchoredPosition = Vector2.zero;
            _root.SetActive(false);
        }

        public void Destroy()
        {
            ClearRows();
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            _group = null;
            _content = null;
            _iconImage = null;
            _titleText = null;
            _titleTextBack = null;
        }

        private void ClearRows()
        {
            foreach (VoteRow row in _rows) row.Destroy();
            _rows.Clear();
        }

        private static void ApplyTitle(TextMeshProUGUI label, string title, bool autoShrink)
        {
            if (label == null) return;
            label.enableWordWrapping = false;
            label.fontSize = TitleFontSize;
            label.fontSizeMin = GuardTitleFontSizeMin;
            label.fontSizeMax = TitleFontSize;
            label.enableAutoSizing = autoShrink;
            label.text = title;
        }
    }
}
