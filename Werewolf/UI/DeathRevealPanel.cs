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

        private const float EntranceStartScale = 2.6f;
        private const float ExitRiseY = 700f;

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

            if (!layerRoot.gameObject.activeSelf) layerRoot.gameObject.SetActive(true);

            var go = new GameObject("WW_DeathReveal", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(layerRoot, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _root = go;

            _group = go.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            Image backdrop = UiKit.CreateImage(rect, "Backdrop", Vector2.zero,
                new Vector2(1920f, 1080f), BackdropColor);
            var bgRect = backdrop.rectTransform;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            _content = UiKit.CreateRect(rect, "Content", Vector2.zero, new Vector2(1280f, 900f));

            _iconImage = UiKit.CreateImage(_content, "Icon", new Vector2(0f, 260f), IconSize, Color.white);
            _iconImage.enabled = false;

            _titleTextBack = UiKit.CreateText(_content, "TitleBack", new Vector2(0f, 40f),
                new Vector2(1200f, 140f), "", 120f, Color.black, TextAlignmentOptions.Center);
            _titleTextBack.outlineColor = Color.white;
            _titleTextBack.outlineWidth = 0.30f;
            _titleTextBack.fontStyle = FontStyles.Bold;
            _titleText = UiKit.CreateText(_content, "Title", new Vector2(0f, 40f),
                new Vector2(1200f, 140f), "", 120f, Color.black, TextAlignmentOptions.Center);
            _titleText.outlineColor = Color.red;
            _titleText.outlineWidth = 0.15f;
            _titleText.fontStyle = FontStyles.Bold;

            _root.SetActive(false);
            WLog.Line("death_reveal_built", secret: false);
        }

        public void Show(IReadOnlyList<WPlayer> deadRoster, Func<int, PlayerAvatar> resolveAvatar)
        {
            if (_root == null) return;
            ClearRows();

            bool hasDeaths = deadRoster != null && deadRoster.Count > 0;
            string title = Texts.Get(hasDeaths ? TextId.DeathRevealTitle : TextId.DeathRevealNone);
            _titleText.text = title;
            _titleTextBack.text = title;

            Sprite icon = AssetCatalog.GetSprite(hasDeaths ? "img_taxman_death" : "img_taxman_nodeath");
            if (icon != null)
            {
                _iconImage.sprite = icon;
                _iconImage.preserveAspect = true;
                _iconImage.enabled = true;
            }
            else
            {
                _iconImage.sprite = null;
                _iconImage.enabled = false;
            }

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
            _content.localScale = Vector3.one * EntranceStartScale;
            _content.anchoredPosition = Vector2.zero;
            _root.SetActive(true);
            WLog.Line("death_reveal_show", secret: false, ("rows", _rows.Count));

            yield return UiTween.Parallel(
                UiTween.Scale(_content, EntranceStartScale, 1f, DeathReveal.EntranceSec, UiTween.EaseIn()),
                UiTween.Fade(_group, 0f, 1f, DeathReveal.EntranceSec, UiTween.EaseIn()));

            try { onImpact?.Invoke(); }
            catch (Exception e)
            {
                WLog.Line("death_reveal_impact_error", secret: false, ("err", e.Message));
            }

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
    }
}
