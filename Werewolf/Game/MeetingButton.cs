using System;
using UnityEngine;
using Werewolf.Core;
using Werewolf.UI;

namespace Werewolf.Game
{
    public sealed class MeetingButton
    {
        private const float InteractDistance = 2.0f;

        private GameObject _visual;
        private float _promptCooldown;
        private bool _isLocalPlayerNear;

        private bool _useGrabInteract;
        private Transform _pressVisual;
        private MeshRenderer _pressRenderer;
        private AnimationCurve _pressCurve;
        private bool _pressAnimating;
        private float _pressEval;
        private static readonly Color PressColorStart = new Color(1f, 0.5f, 0f, 1f);
        private const float AimDotThreshold = 0.9f;

        public Action OnConvene;

        public Action OnIncompleteHold;

        private readonly ConveneHold _hold = new ConveneHold();

        public float HoldRatio => _hold.Ratio;

        public bool HoldCharging => _hold.IsCharging;

        public Vector3? VisualWorldPos => _visual != null ? (Vector3?)_visual.transform.position : null;

        public MeetingButton()
        {
        }

        public bool Exists => _visual != null;

        public bool IsLocalPlayerNear => _isLocalPlayerNear;

        public bool Create()
        {
            if (_visual != null) return true;

            TruckSafetySpawnPoint anchor = TruckSafetySpawnPoint.instance;
            if (anchor == null)
            {
                WLog.Line("meeting_button_error", secret: false, ("reason", "no_truck_anchor"));
                return false;
            }

            Transform baseT = anchor.transform;
            GameConfig config = Plugin.GameConfig;
            float ox = config != null ? config.ButtonOffsetX : 0f;
            float oy = config != null ? config.ButtonOffsetY : 0f;
            float oz = config != null ? config.ButtonOffsetZ : 0f;
            float yaw = config != null ? config.ButtonYaw : 0f;
            float pitch = config != null ? config.ButtonPitch : 0f;
            Vector3 offset = new Vector3(ox, oy, oz);
            Vector3 pos = baseT.position + baseT.TransformVector(offset);
            Quaternion rot = baseT.rotation * Quaternion.Euler(pitch, yaw, 0f);

            _visual = BuildVisual();
            if (_visual == null)
            {
                WLog.Line("meeting_button_error", secret: false, ("reason", "visual_build_failed"));
                return false;
            }
            _visual.transform.SetPositionAndRotation(pos, rot);

            WLog.Line("meeting_button_created", secret: false,
                ("pos", $"{pos.x:F2},{pos.y:F2},{pos.z:F2}"));
            return true;
        }

        public void Destroy()
        {
            if (_visual != null)
            {
                UnityEngine.Object.Destroy(_visual);
                _visual = null;
            }
            _isLocalPlayerNear = false;
            _hold.Reset();
            _useGrabInteract = false;
            _pressVisual = null;
            _pressRenderer = null;
            _pressCurve = null;
            _pressAnimating = false;
            _pressEval = 0f;
        }

        private bool IsAimingAtButton(PlayerAvatar local)
        {
            Camera cam = Camera.main;
            if (cam == null) return true;
            Vector3 toBtn = _visual.transform.position - cam.transform.position;
            float len = toBtn.magnitude;
            if (len < 0.01f) return true;
            toBtn /= len;
            float dot = Vector3.Dot(cam.transform.forward, toBtn);
            return dot >= AimDotThreshold;
        }

        private static bool IsLocalTumbling(PlayerAvatar local)
        {
            return GameRefs.PlayerAvatar_isTumbling != null && GameRefs.PlayerAvatar_isTumbling(local);
        }

        private void StartPressAnimation()
        {
            if (_pressVisual == null) return;
            _pressVisual.localScale = new Vector3(1f, 0.1f, 1f);
            _pressAnimating = true;
            _pressEval = 0f;
        }

        private void TickPressAnimation()
        {
            if (!_pressAnimating || _pressVisual == null || _pressCurve == null) return;

            _pressEval += Time.deltaTime * 2f;
            _pressEval = Mathf.Clamp01(_pressEval);
            float t = _pressCurve.Evaluate(_pressEval);
            if (_pressRenderer != null && _pressRenderer.material != null)
            {
                _pressRenderer.material.SetColor("_EmissionColor",
                    Color.Lerp(PressColorStart, Color.white, t));
            }
            float scaleY = Mathf.Clamp(t, 0.5f, 1f);
            _pressVisual.localScale = new Vector3(1f, scaleY, 1f);
            if (_pressEval >= 1f)
            {
                _pressAnimating = false;
                _pressEval = 0f;
            }
        }

        public void Tick(bool localCanConvene, long readyAtUnixMs, long nowUnixMs, int rightsRemaining)
        {
            TickPressAnimation();

            if (_visual == null) { _isLocalPlayerNear = false; _hold.Reset(); return; }
            PlayerAvatar local = PlayerAvatar.instance;
            if (local == null) { _isLocalPlayerNear = false; _hold.Reset(); return; }

            float dist = Vector3.Distance(local.transform.position, _visual.transform.position);
            bool near = dist <= InteractDistance;
            _isLocalPlayerNear = near;
            if (!near) { _hold.Tick(false, Time.deltaTime); return; }

            try
            {
                _promptCooldown -= Time.deltaTime;
                if (_promptCooldown <= 0f)
                {
                    string label;
                    if (localCanConvene)
                    {
                        long remainMs = readyAtUnixMs - nowUnixMs;
                        if (remainMs > 0)
                        {
                            int remainSec = (int)((remainMs + 999) / 1000);
                            label = Texts.Format(TextId.MeetingButtonSuppressCountdownFormat, remainSec);
                        }
                        else
                        {
                            string prompt = _useGrabInteract
                                ? Texts.Get(TextId.MeetingButtonConveneGrabPrompt)
                                : Texts.Get(TextId.MeetingButtonConveneInteractPrompt);
                            label = rightsRemaining >= 0
                                ? prompt + Texts.Format(TextId.MeetingButtonRightsSuffixFormat, rightsRemaining)
                                : prompt;
                        }
                    }
                    else
                    {
                        label = Texts.Get(TextId.MeetingButtonSuppressedPrompt);
                    }
                    SemiFunc.UIFocusText(label, Color.white, new Color(1f, 0.6f, 0.6f), 0.2f);
                    _promptCooldown = 0.1f;
                }

                InputKey inputKey = _useGrabInteract ? InputKey.Grab : InputKey.Interact;
                bool inputHeld = SemiFunc.InputHold(inputKey);
                bool inputReleased = SemiFunc.InputUp(inputKey);
                bool aimed = !_useGrabInteract || IsAimingAtButton(local);
                bool suppressed = readyAtUnixMs - nowUnixMs > 0;
                bool otherwiseEligible = localCanConvene && !suppressed && aimed && !IsLocalTumbling(local);
                bool wasCharging = _hold.IsCharging;
                bool fired = _hold.Tick(otherwiseEligible && inputHeld, Time.deltaTime);
                if (fired)
                {
                    WLog.Line("meeting_button_pressed", secret: false,
                        ("localGate", localCanConvene),
                        ("mode", _useGrabInteract ? "grab" : "interact"));
                    StartPressAnimation();
                    OnConvene?.Invoke();
                }
                else if (wasCharging && inputReleased && otherwiseEligible)
                {
                    WLog.Line("meeting_button_hold_incomplete", secret: false,
                        ("mode", _useGrabInteract ? "grab" : "interact"));
                    OnIncompleteHold?.Invoke();
                }
            }
            catch (Exception e)
            {
                WLog.Line("meeting_button_tick_error", secret: false, ("err", e.Message));
            }
        }

        private GameObject BuildVisual()
        {
            GameObject fromShop = TryBuildFromShopButtonTemplate();
            if (fromShop != null) return fromShop;
            GameObject fromBundle = TryBuildFromAssetBundle();
            if (fromBundle != null) return fromBundle;
            return BuildPrimitive();
        }

        private GameObject TryBuildFromShopButtonTemplate()
        {
            try
            {
                if (!MeetingButtonAssetResolver.TryResolve())
                {
                    return null;
                }
                GameObject template = MeetingButtonAssetResolver.Template;
                if (template == null) return null;

                GameObject instance = UnityEngine.Object.Instantiate(template);
                instance.name = "WW_MeetingButton_ShopClone";

                MeshRenderer mr = instance.GetComponent<MeshRenderer>()
                    ?? instance.GetComponentInChildren<MeshRenderer>(true);

                _pressVisual = instance.transform;
                _pressRenderer = mr;
                _pressCurve = MeetingButtonAssetResolver.PressCurve;
                _useGrabInteract = true;
                _pressAnimating = false;
                _pressEval = 0f;

                WLog.Line("meeting_button_visual_shop", secret: false,
                    ("hasRenderer", mr != null),
                    ("hasCurve", _pressCurve != null));
                return instance;
            }
            catch (System.Exception e)
            {
                WLog.Line("meeting_button_shop_fallback", secret: false, ("err", e.Message));
                _useGrabInteract = false;
                _pressVisual = null;
                _pressRenderer = null;
                _pressCurve = null;
                return null;
            }
        }

        private GameObject TryBuildFromAssetBundle()
        {
            try
            {
                GameObject prefab = AssetCatalog.GetPrefab("MeetingButton");
                if (prefab == null) return null;

                GameObject instance = UnityEngine.Object.Instantiate(prefab);
                instance.name = "WW_MeetingButton";

                foreach (Collider col in instance.GetComponentsInChildren<Collider>(true))
                {
                    UnityEngine.Object.Destroy(col);
                }

                return instance;
            }
            catch (Exception e)
            {
                WLog.Line("meeting_button_bundle_fallback", secret: false, ("err", e.Message));
                return null;
            }
        }

        private GameObject BuildPrimitive()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "WW_MeetingButton";

            Collider col = go.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.Destroy(col);

            go.transform.localScale = new Vector3(0.3f, 0.15f, 0.3f);

            Renderer r = go.GetComponent<Renderer>();
            if (r != null && r.material != null)
            {
                r.material.color = Color.red;
                r.material.EnableKeyword("_EMISSION");
                r.material.SetColor("_EmissionColor", Color.red);
            }
            return go;
        }
    }
}
