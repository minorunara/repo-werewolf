using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Werewolf.Core;
using Werewolf.Core.Replay;

namespace Werewolf.UI
{
    public sealed partial class ReplayViewerPanel
    {
        private void UpdateMarkers()
        {
            _playerHits.Clear();
            if (_markersRoot == null) return;

            MapPanelProjection proj = default;
            bool hasProj = false;
            if (_still != null)
            {
                proj = MapPanelProjection.FromWorldRect(
                    _still.MinX, _still.MaxX, _still.MinZ, _still.MaxZ, PanelWidth, MapHeight);
                hasProj = proj.Valid;
            }

            int playerCount = 0;
            int trailCount = 0;
            int popupCount = 0;
            int deathMarkCount = 0;

            if (DemoActive)
            {
                _demoPhase += Mathf.Max(0f, Time.unscaledDeltaTime);
                playerCount = UpdateDemoMarkers();
            }
            else if (_playback != null && hasProj && _clock != null)
            {
                double t = _clock.T;

                if (_selectedActors.Count > 0)
                {
                    for (int i = 0; i < _playback.Players.Count; i++)
                    {
                        ReplayPlayerEntry departed = _playback.Players[i];
                        if (departed.IsDepartedAt(t)) _selectedActors.Remove(departed.Actor);
                    }
                }
                bool anySelected = _selectedActors.Count > 0;

                for (int i = 0; i < _playback.Players.Count; i++)
                {
                    ReplayPlayerEntry entry = _playback.Players[i];
                    if (entry.IsDepartedAt(t)) continue;
                    bool dead = !entry.IsAliveAt(t);
                    if (!TryGetMarkerPos(entry, dead, t, out float wx, out float wz)) continue;
                    var pos = new Vector2(proj.PanelX(wx), proj.PanelY(wz));
                    bool selected = _selectedActors.Contains(entry.Actor);
                    ConfigurePlayerMarker(EnsurePlayerMarker(playerCount++), entry, pos,
                        dead: dead,
                        self: _playback.Header != null && entry.Actor == _playback.Header.LocalActor,
                        dimmed: anySelected && !selected);
                    _playerHits.Add((entry.Actor, pos));

                    if (selected)
                    {
                        trailCount = DrawTrail(entry, t, proj, trailCount);
                    }
                }

                UpdateDotPool(_enemyPool, _enemyLayer, _playback.Enemies, t, proj,
                    EnemyDotSize, EnemyColor, circle: true, labelled: true);
                UpdateDotPool(_valuablePool, _valuableLayer, _playback.Valuables, t, proj,
                    ValuableDotSize, ValuableColor, circle: true, labelled: false);
                UpdateDotPool(_itemPool, _itemLayer, _playback.Items, t, proj,
                    ItemDotSize, ItemColor, circle: true, labelled: false);
                UpdateDotPool(_cartPool, _cartLayer, _playback.Carts, t, proj,
                    CartSize, CartColor, circle: false, labelled: false);
                UpdateEpMarkers(t, proj);
                deathMarkCount = UpdateDeathMarks(t, proj);
                popupCount = UpdatePopups(t, proj);
            }

            HidePlayerExtra(playerCount);
            HideTrailExtra(trailCount);
            HidePopupExtra(popupCount);
            HideDeathMarkExtra(deathMarkCount);
            if (DemoActive || _playback == null || !hasProj)
            {
                HideExtraDots(_enemyPool, 0);
                HideExtraDots(_valuablePool, 0);
                HideExtraDots(_itemPool, 0);
                HideExtraDots(_cartPool, 0);
                HideExtraDots(_epPool, 0);
            }
        }

        private int UpdateDemoMarkers()
        {
            int count = 0;
            for (int i = 0; i < _demoCount; i++)
            {
                float radius = 40f + (i % 12) * 24f;
                float speed = 0.3f + (i % 7) * 0.12f;
                float angle = _demoPhase * speed + i * 2.39996f;
                var pos = new Vector2(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius * 0.52f);
                PlayerMarker marker = EnsurePlayerMarker(count++);
                var entry = new ReplayPlayerEntry { Actor = i + 1, ParticipantId = i + 1, Name = "P" + (i + 1) };
                ConfigurePlayerMarker(marker, entry, pos, dead: false, self: false, dimmed: false);
                _playerHits.Add((entry.Actor, pos));
            }
            return count;
        }

        private bool TryGetMarkerPos(ReplayPlayerEntry entry, bool dead, double t, out float x, out float z)
        {
            if (dead && entry.CorpseTrack != null
                && _playback.TryGetPos(entry.CorpseTrack, t, out x, out _, out z))
            {
                return true;
            }
            return _playback.TryGetPos(entry.Track, t, out x, out _, out z);
        }

        private int DrawTrail(ReplayPlayerEntry entry, double t, MapPanelProjection proj, int trailCount)
        {
            ReplayEntityTrack track = !entry.IsAliveAt(t) && entry.CorpseTrack != null
                ? entry.CorpseTrack
                : entry.Track;
            _trailScratch.Clear();
            _playback.TrailInto(track, t - TrailSeconds, t, _trailScratch);
            if (_playback.TryGetPos(track, t, out float cx, out _, out float cz))
            {
                _trailScratch.Add(new ReplayTrailPoint { T = t, X = cx, Z = cz });
            }
            if (_trailScratch.Count < 2) return trailCount;

            ReplayMarkerPalette.ColorFor(entry.ParticipantId, out float r, out float g, out float b);
            var color = new Color(r, g, b, 0.8f);
            for (int i = 1; i < _trailScratch.Count; i++)
            {
                ReplayTrailPoint p0 = _trailScratch[i - 1];
                ReplayTrailPoint p1 = _trailScratch[i];
                if (p1.T - p0.T > ReplayPlayback.PresenceGapSec) continue;
                var a = new Vector2(proj.PanelX(p0.X), proj.PanelY(p0.Z));
                var c = new Vector2(proj.PanelX(p1.X), proj.PanelY(p1.Z));
                Vector2 d = c - a;
                float len = d.magnitude;
                if (len < 0.5f) continue;

                Image seg = EnsureTrailSegment(trailCount++);
                seg.color = color;
                var rect = seg.rectTransform;
                rect.anchoredPosition = (a + c) * 0.5f;
                rect.sizeDelta = new Vector2(len, TrailThickness);
                rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            }
            return trailCount;
        }

        private void ConfigurePlayerMarker(PlayerMarker marker, ReplayPlayerEntry entry, Vector2 pos,
            bool dead, bool self, bool dimmed)
        {
            marker.Rect.anchoredPosition = pos;

            ReplayMarkerPalette.ColorFor(entry.ParticipantId, out float r, out float g, out float b);
            Color fill = dead ? new Color(0.45f, 0.45f, 0.45f, 1f) : new Color(r, g, b, 1f);
            Color frame = entry.IsWerewolfSide ? FrameWolfColor : (self ? FrameSelfColor : FrameDefaultColor);
            Color name = entry.IsWerewolfSide ? NameWolfColor : NameDefaultColor;
            float alpha = (dimmed || dead) ? DimFactor : 1f;
            fill.a *= alpha;
            frame.a *= alpha;
            name.a *= alpha;

            if (marker.Fill.color != fill) marker.Fill.color = fill;
            if (marker.Frame.color != frame) marker.Frame.color = frame;

            string idText = entry.ParticipantId > 0 ? entry.ParticipantId.ToString() : "?";
            if (marker.Id.text != idText)
            {
                marker.Id.text = idText;
                marker.Id.fontSize = idText.Length >= 3 ? PlayerIdFontSize - 3f : PlayerIdFontSize;
            }
            Color idColor = new Color(0f, 0f, 0f, 0.9f * alpha);
            if (marker.Id.color != idColor) marker.Id.color = idColor;

            bool showName = !dimmed;
            if (marker.Name.gameObject.activeSelf != showName) marker.Name.gameObject.SetActive(showName);
            if (showName)
            {
                string nameText = entry.Name ?? "";
                if (marker.Name.text != nameText) marker.Name.text = nameText;
                if (marker.Name.color != name) marker.Name.color = name;
            }

            if (!marker.Root.activeSelf) marker.Root.SetActive(true);
        }

        private void UpdateDotPool(List<DotMarker> pool, RectTransform layer,
            List<ReplayEntityTrack> tracks, double t,
            MapPanelProjection proj, float size, Color color, bool circle, bool labelled)
        {
            int count = 0;
            if (tracks != null)
            {
                for (int i = 0; i < tracks.Count; i++)
                {
                    ReplayEntityTrack track = tracks[i];
                    if (track.HiddenDuplicate) continue;
                    if (!_playback.TryGetPos(track, t, out float wx, out _, out float wz)) continue;
                    DotMarker dot = EnsureDot(pool, layer, count++, size, color, circle, labelled);
                    dot.Rect.anchoredPosition = new Vector2(proj.PanelX(wx), proj.PanelY(wz));
                    if (dot.Dot.color != color) dot.Dot.color = color;
                    if (labelled && dot.Label != null)
                    {
                        string label = track.Name ?? "";
                        if (dot.Label.text != label) dot.Label.text = label;
                    }
                    if (!dot.Root.activeSelf) dot.Root.SetActive(true);
                }
            }
            HideExtraDots(pool, count);
        }

        private void UpdateEpMarkers(double t, MapPanelProjection proj)
        {
            int count = 0;
            if (_playback != null)
            {
                for (int i = 0; i < _playback.Eps.Count; i++)
                {
                    ReplayEpEntry ep = _playback.Eps[i];
                    DotMarker dot = EnsureEpMarker(count++);
                    dot.Rect.anchoredPosition = new Vector2(proj.PanelX(ep.X), proj.PanelY(ep.Z));
                    string stateName = ep.StateAt(t).Name;
                    Color color = EpStateColor(stateName);
                    if (dot.Dot.color != color) dot.Dot.color = color;
                    if (dot.Label != null)
                    {
                        string label = ep.Number.ToString();
                        if (dot.Label.text != label) dot.Label.text = label;
                    }
                    if (dot.Sub != null)
                    {
                        string state = "EP" + ep.Number + " " + ReplayEpStateText.Label(stateName);
                        if (dot.Sub.text != state) dot.Sub.text = state;
                        if (dot.Sub.color != color) dot.Sub.color = color;
                    }
                    if (!dot.Root.activeSelf) dot.Root.SetActive(true);
                }
            }
            HideExtraDots(_epPool, count);
        }

        private static Color EpStateColor(string stateName)
        {
            switch (stateName)
            {
                case "Complete":
                case "Success":
                case "Surplus": return new Color(0.31f, 0.75f, 0.42f, 1f);
                case "Cancel": return new Color(0.9f, 0.33f, 0.29f, 1f);
                case "Extracting":
                case "TaxReturn": return new Color(0.31f, 0.65f, 0.75f, 1f);
                case "Idle":
                case "None": return new Color(0.6f, 0.6f, 0.65f, 1f);
                default: return new Color(1f, 0.65f, 0.2f, 1f);
            }
        }

        private int UpdateDeathMarks(double t, MapPanelProjection proj)
        {
            int count = 0;
            List<ReplayDeathMark> marks = _playback.DeathMarks;
            for (int i = 0; i < marks.Count; i++)
            {
                ReplayDeathMark mark = marks[i];
                if (!mark.IsVisibleAt(t)) continue;
                RectTransform rect = EnsureDeathMark(count++);
                rect.anchoredPosition = new Vector2(proj.PanelX(mark.X), proj.PanelY(mark.Z));
                if (!rect.gameObject.activeSelf) rect.gameObject.SetActive(true);
            }
            return count;
        }

        private RectTransform EnsureDeathMark(int index)
        {
            while (_deathMarkPool.Count <= index)
            {
                RectTransform root = UiKit.CreateRect(_deathMarkLayer, "Death" + _deathMarkPool.Count,
                    Vector2.zero, new Vector2(DeathMarkSize, DeathMarkSize));
                if (_deathMarkSprite != null)
                {
                    Image cross = UiKit.CreateImage(root, "Cross", Vector2.zero,
                        new Vector2(DeathMarkSize, DeathMarkSize), Color.white);
                    cross.sprite = _deathMarkSprite;
                    cross.preserveAspect = true;
                }
                else
                {
                    foreach (float angle in new[] { 45f, -45f })
                    {
                        Image bar = UiKit.CreateImage(root, "CrossBar", Vector2.zero,
                            new Vector2(DeathMarkSize * 0.95f, DeathMarkBarThickness),
                            DeathMarkFallbackColor);
                        bar.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
                    }
                }
                root.gameObject.SetActive(false);
                _deathMarkPool.Add(root);
            }
            return _deathMarkPool[index];
        }

        private void HideDeathMarkExtra(int used)
        {
            for (int i = used; i < _deathMarkPool.Count; i++)
            {
                if (_deathMarkPool[i].gameObject.activeSelf) _deathMarkPool[i].gameObject.SetActive(false);
            }
        }

        private int UpdatePopups(double t, MapPanelProjection proj)
        {
            if (_playback == null || _clock == null || _popupRoot == null) return 0;

            double life = PopupVisibleRealSec * _clock.EffectiveSpeed();
            if (life < PopupMinLifeSec) life = PopupMinLifeSec;
            if (life > PopupMaxLifeSec) life = PopupMaxLifeSec;

            int used = 0;
            for (int i = _playback.LastPopupIndexAtOrBefore(t); i >= 0 && used < PopupMaxVisible; i--)
            {
                ReplayValuePopup p = _playback.Popups[i];
                double age = t - p.T;
                if (age > life) break;
                float f = (float)(age / life);

                TextMeshProUGUI label = EnsurePopup(used++);
                bool deliver = p.Kind == ReplayValueEventKind.Deliver;
                string text = (deliver ? "+$" : "-$") + p.Amount;
                if (label.text != text) label.text = text;
                Color color = deliver ? PopupDeliverColor : PopupLostColor;
                color.a = 1f - f * f;
                if (label.color != color) label.color = color;
                label.rectTransform.anchoredPosition = new Vector2(
                    proj.PanelX(p.X),
                    proj.PanelY(p.Z) + PopupBaseOffsetY + PopupRiseY * f);
            }
            return used;
        }

        private void UpdateGauge()
        {
            if (_gaugeRoot == null) return;

            bool show = !DemoActive && _playback != null && _clock != null && _playback.BaseDollars > 0;
            if (_gaugeRoot.activeSelf != show) _gaugeRoot.SetActive(show);
            if (!show) return;

            double t = _clock.T;
            int baseDollars = _playback.BaseDollars;
            int lost = _playback.LostDollarsAt(t);
            int delivered = _playback.DeliveredDollarsAt(t);

            if (_gaugeLossFill != null)
            {
                _gaugeLossFill.fillAmount = Mathf.Clamp01((float)lost / baseDollars);
            }
            if (_gaugeDeliveryFill != null)
            {
                _gaugeDeliveryFill.fillAmount = Mathf.Clamp01((float)delivered / baseDollars);
            }
            if (lost != _gaugeShownLoss)
            {
                _gaugeShownLoss = lost;
                if (_gaugeLossText != null)
                {
                    _gaugeLossText.text = Texts.Format(TextId.ReplayGaugeLossFormat, lost, baseDollars);
                }
            }
            if (delivered != _gaugeShownDelivered)
            {
                _gaugeShownDelivered = delivered;
                if (_gaugeDeliveredText != null)
                {
                    _gaugeDeliveredText.text = _gaugeHasDelivery
                        ? Texts.Format(TextId.ReplayGaugeDeliveredFormat, delivered)
                        : string.Empty;
                }
            }
        }

        private PlayerMarker EnsurePlayerMarker(int index)
        {
            while (_playerPool.Count <= index)
            {
                var marker = new PlayerMarker();
                RectTransform root = UiKit.CreateRect(_playerLayer, "Player" + _playerPool.Count,
                    Vector2.zero, new Vector2(PlayerFrameSize, PlayerFrameSize));
                marker.Root = root.gameObject;
                marker.Rect = root;

                marker.Frame = UiKit.CreateImage(root, "Frame", Vector2.zero,
                    new Vector2(PlayerFrameSize, PlayerFrameSize), FrameDefaultColor);
                marker.Frame.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
                marker.Fill = UiKit.CreateImage(root, "Fill", Vector2.zero,
                    new Vector2(PlayerFillSize, PlayerFillSize), Color.white);
                marker.Fill.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);

                marker.Id = UiKit.CreateText(root, "Id", Vector2.zero,
                    new Vector2(PlayerFrameSize * 2f, PlayerFrameSize),
                    "", PlayerIdFontSize, Color.black, TextAlignmentOptions.Center);
                marker.Name = UiKit.CreateText(root, "Name", new Vector2(0f, PlayerNameOffsetY),
                    new Vector2(160f, 18f),
                    "", PlayerNameFontSize, NameDefaultColor, TextAlignmentOptions.Center);

                marker.Root.SetActive(false);
                _playerPool.Add(marker);
            }
            return _playerPool[index];
        }

        private void HidePlayerExtra(int used)
        {
            for (int i = used; i < _playerPool.Count; i++)
            {
                if (_playerPool[i].Root.activeSelf) _playerPool[i].Root.SetActive(false);
            }
        }

        private DotMarker EnsureDot(List<DotMarker> pool, RectTransform layer, int index, float size,
            Color color, bool circle, bool labelled)
        {
            while (pool.Count <= index)
            {
                var dot = new DotMarker();
                RectTransform root = UiKit.CreateRect(layer, "Dot" + pool.Count,
                    Vector2.zero, new Vector2(size, size));
                dot.Root = root.gameObject;
                dot.Rect = root;
                dot.Dot = UiKit.CreateImage(root, "Img", Vector2.zero, new Vector2(size, size), color);
                if (circle) dot.Dot.sprite = UiKit.CircleSprite();
                if (labelled)
                {
                    dot.Label = UiKit.CreateText(root, "Label",
                        new Vector2(0f, EnemyNameOffsetY), new Vector2(140f, 16f),
                        "", EnemyNameFontSize, EnemyColor, TextAlignmentOptions.Center);
                }
                dot.Root.SetActive(false);
                pool.Add(dot);
            }
            return pool[index];
        }

        private DotMarker EnsureEpMarker(int index)
        {
            while (_epPool.Count <= index)
            {
                var dot = new DotMarker();
                RectTransform root = UiKit.CreateRect(_epLayer, "Ep" + _epPool.Count,
                    Vector2.zero, new Vector2(EpSize, EpSize));
                dot.Root = root.gameObject;
                dot.Rect = root;
                dot.Dot = UiKit.CreateImage(root, "Img", Vector2.zero,
                    new Vector2(EpSize, EpSize), Color.white);
                dot.Label = UiKit.CreateText(root, "Number", Vector2.zero,
                    new Vector2(EpSize * 2f, EpSize),
                    "", EpFontSize, Color.black, TextAlignmentOptions.Center);
                dot.Sub = UiKit.CreateText(root, "State", new Vector2(0f, EpStateOffsetY),
                    new Vector2(220f, 18f),
                    "", EpStateFontSize, Color.white, TextAlignmentOptions.Center);
                dot.Root.SetActive(false);
                _epPool.Add(dot);
            }
            return _epPool[index];
        }

        private TextMeshProUGUI EnsurePopup(int index)
        {
            while (_popupPool.Count <= index)
            {
                TextMeshProUGUI label = UiKit.CreateText(_popupRoot, "Popup" + _popupPool.Count,
                    Vector2.zero, new Vector2(180f, 24f),
                    "", PopupFontSize, PopupLostColor, TextAlignmentOptions.Center);
                label.gameObject.SetActive(false);
                _popupPool.Add(label);
            }
            TextMeshProUGUI result = _popupPool[index];
            if (!result.gameObject.activeSelf) result.gameObject.SetActive(true);
            return result;
        }

        private void HidePopupExtra(int used)
        {
            for (int i = used; i < _popupPool.Count; i++)
            {
                if (_popupPool[i].gameObject.activeSelf) _popupPool[i].gameObject.SetActive(false);
            }
        }

        private static void HideExtraDots(List<DotMarker> pool, int used)
        {
            for (int i = used; i < pool.Count; i++)
            {
                if (pool[i].Root.activeSelf) pool[i].Root.SetActive(false);
            }
        }

        private Image EnsureTrailSegment(int index)
        {
            while (_trailPool.Count <= index)
            {
                Image seg = UiKit.CreateImage(_trailLayer, "Trail" + _trailPool.Count,
                    Vector2.zero, new Vector2(1f, TrailThickness), Color.white);
                seg.gameObject.SetActive(false);
                _trailPool.Add(seg);
            }
            Image result = _trailPool[index];
            if (!result.gameObject.activeSelf) result.gameObject.SetActive(true);
            return result;
        }

        private void HideTrailExtra(int used)
        {
            for (int i = used; i < _trailPool.Count; i++)
            {
                if (_trailPool[i].gameObject.activeSelf) _trailPool[i].gameObject.SetActive(false);
            }
        }

        private Image EnsureSeekOverlay(List<Image> pool, int index, string namePrefix, Color color)
        {
            while (pool.Count <= index)
            {
                Image img = UiKit.CreateImage(_seekBarRect, namePrefix + pool.Count,
                    Vector2.zero, new Vector2(2f, SeekBarThickness), color);
                pool.Add(img);
            }
            Image result = pool[index];
            if (!result.gameObject.activeSelf) result.gameObject.SetActive(true);
            return result;
        }

        private static void HideExtra(List<Image> pool, int used)
        {
            for (int i = used; i < pool.Count; i++)
            {
                if (pool[i].gameObject.activeSelf) pool[i].gameObject.SetActive(false);
            }
        }
    }
}
