using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;

namespace Werewolf.UI
{
    public sealed class EradicationRevealPanel : IClientPanel
    {
        public string LayerName => "EradicationReveal";

        private static readonly Vector2 IconSize = new Vector2(280f, 280f);
        private static readonly Vector2 IconPos = new Vector2(0f, 250f);
        private static readonly Vector2 TitlePos = new Vector2(0f, 30f);
        private static readonly Vector2 RowPos = new Vector2(0f, -140f);

        private static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.85f);
        private static readonly Color TitleColor = new Color(0.85f, 0.08f, 0.08f, 1f);

        private GameObject _root;
        private CanvasGroup _group;
        private RectTransform _stampRect;
        private CanvasGroup _stampGroup;
        private Image _iconImage;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _titleTextBack;
        private VoteRow _row;

        private bool _contentReady;

        public bool Exists => _root != null;

        public bool Visible => _root != null && _root.activeSelf;

        public void Build(Transform layerRoot)
        {
            if (_root != null || layerRoot == null) return;

            RectTransform rect = RevealPanelKit.BuildFullscreenRoot(
                layerRoot, "WW_EradicationReveal", BackdropColor, out _group);
            _root = rect.gameObject;

            _stampRect = UiKit.CreateRect(rect, "Stamp", Vector2.zero, new Vector2(1400f, 900f));
            _stampGroup = _stampRect.gameObject.AddComponent<CanvasGroup>();
            _stampGroup.alpha = 0f;
            _stampGroup.blocksRaycasts = false;
            _stampGroup.interactable = false;

            _iconImage = UiKit.CreateImage(_stampRect, "Icon", IconPos, IconSize, Color.white);
            _iconImage.enabled = false;

            _titleText = RevealPanelKit.CreateStampTitle(_stampRect, TitlePos,
                new Vector2(1400f, 130f), "", 96f, TitleColor, Color.black, 0.12f,
                out _titleTextBack);

            _root.SetActive(false);
            WLog.Line("eradication_reveal_built", secret: false);
        }

        public void Show(WPlayer victim, Func<int, PlayerAvatar> resolveAvatar,
                         Team winningTeam, bool vanished)
        {
            if (_root == null || victim == null) return;

            string title = Texts.Get(EradicationCeremony.TitleId(winningTeam, vanished));
            _titleText.text = title;
            _titleTextBack.text = title;

            RevealPanelKit.SetIcon(_iconImage, AssetCatalog.GetSprite("img_taxman_death"));

            _row?.Destroy();
            _row = VoteRow.Build(_stampRect, victim, resolveAvatar, VoteRowGrid.RowSize);
            if (_row.Root != null) _row.Root.anchoredPosition = RowPos;

            _stampGroup.alpha = 0f;
            _stampRect.localScale = Vector3.one;
            _contentReady = true;
        }

        public IEnumerator Play(Action onStamp)
        {
            if (_root == null || !_contentReady)
            {
                WLog.Line("eradication_reveal_skip", secret: false, ("reason", "not_built"));
                yield break;
            }

            yield return UiTween.Hold(EradicationCeremony.GraceSec);

            _group.alpha = 0f;
            _root.transform.parent.SetAsLastSibling();
            _root.SetActive(true);
            WLog.Line("eradication_reveal_show", secret: false);

            yield return UiTween.Fade(_group, 0f, 1f,
                EradicationCeremony.BackdropFadeSec, UiTween.EaseIn());

            yield return RevealPanelKit.StampEntrance(
                _stampRect, _stampGroup, EradicationCeremony.StampEntranceSec);

            RevealPanelKit.InvokeSfx(onStamp, "eradication_stamp_sfx_error");

            yield return UiTween.Hold(EradicationCeremony.StampHoldSec);
            WLog.Line("eradication_reveal_hold_end", secret: false);
        }

        public void Tick()
        {
            if (!Visible) return;
            _row?.Tick();
        }

        public void HideNow()
        {
            _contentReady = false;
            _row?.Destroy();
            _row = null;
            if (_root == null) return;
            _group.alpha = 0f;
            _stampGroup.alpha = 0f;
            _stampRect.localScale = Vector3.one;
            _root.SetActive(false);
        }

        public void Destroy()
        {
            _contentReady = false;
            _row?.Destroy();
            _row = null;
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
            _group = null;
            _stampRect = null;
            _stampGroup = null;
            _iconImage = null;
            _titleText = null;
            _titleTextBack = null;
        }
    }
}
