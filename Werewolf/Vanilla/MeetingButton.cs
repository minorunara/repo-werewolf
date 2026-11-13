#nullable enable
using System;
using UnityEngine;
using Werewolf.Core;

namespace Werewolf.Vanilla
{
    public sealed class MeetingButton
    {
        private const float InteractDistance = 2.0f;

        private sealed class VisualState
        {
            public VisualState(GameObject root, bool usesGrabInteract, MeetingButtonPressVisual press)
            {
                Root = root;
                UsesGrabInteract = usesGrabInteract;
                Press = press;
            }

            public GameObject Root { get; }
            public bool UsesGrabInteract { get; }
            public MeetingButtonPressVisual Press { get; }
        }

        private VisualState? _visual;
        private float _promptCooldown;
        private bool _isLocalPlayerNear;

        private const float AimDotThreshold = 0.9f;

        public Action? OnConvene;

        public Action? OnIncompleteHold;

        internal static Func<string, GameObject?>? PrefabResolver;

        private readonly ConveneHold _hold = new ConveneHold();

        public float HoldRatio => _hold.Ratio;

        public bool HoldCharging => _hold.IsCharging;

        public bool UsesGrabInteract => GetLiveVisualOrNull()?.UsesGrabInteract ?? false;

        public Vector3? VisualWorldPos
        {
            get
            {
                VisualState? visual = GetLiveVisualOrNull();
                return visual != null ? (Vector3?)visual.Root.transform.position : null;
            }
        }

        public MeetingButton()
        {
        }

        public bool Exists => GetLiveVisualOrNull() != null;

        public bool IsLocalPlayerNear => _isLocalPlayerNear;

        public static bool AnchorExists => TruckSafetySpawnPoint.instance != null;

        public bool Create()
        {
            if (GetLiveVisualOrNull() != null) return true;

            TruckSafetySpawnPoint anchor = TruckSafetySpawnPoint.instance;
            if (anchor == null)
            {
                WLog.Line("meeting_button_error", secret: false, ("reason", "no_truck_anchor"));
                return false;
            }

            Transform baseT = anchor.transform;
            GameConfig? config = Plugin.GameConfig;
            float ox = config != null ? config.ButtonOffsetX : 0f;
            float oy = config != null ? config.ButtonOffsetY : 0f;
            float oz = config != null ? config.ButtonOffsetZ : 0f;
            float yaw = config != null ? config.ButtonYaw : 0f;
            float pitch = config != null ? config.ButtonPitch : 0f;
            Vector3 offset = new Vector3(ox, oy, oz);
            Vector3 pos = baseT.position + baseT.TransformVector(offset);
            Quaternion rot = baseT.rotation * Quaternion.Euler(pitch, yaw, 0f);

            VisualState? visual = null;
            try
            {
                visual = BuildVisual();
                visual.Root.transform.SetPositionAndRotation(pos, rot);
                _visual = visual;
                return true;
            }
            catch (Exception e)
            {
                if (visual != null && visual.Root != null) UnityEngine.Object.Destroy(visual.Root);
                WLog.Line("meeting_button_error", secret: false,
                    ("reason", "visual_build_failed"), ("err", e.Message));
                return false;
            }
        }

        public void Destroy()
        {
            VisualState? visual = _visual;
            _visual = null;
            ResetVisualState(visual);
            if (visual != null && visual.Root != null) UnityEngine.Object.Destroy(visual.Root);
        }

        private static bool IsAimingAtButton(PlayerAvatar local, VisualState visual)
        {
            Camera cam = Camera.main;
            if (cam == null) return true;
            Vector3 toBtn = visual.Root.transform.position - cam.transform.position;
            float len = toBtn.magnitude;
            if (len < 0.01f) return true;
            toBtn /= len;
            float dot = Vector3.Dot(cam.transform.forward, toBtn);
            return dot >= AimDotThreshold;
        }

        private static bool IsLocalTumbling(PlayerAvatar local)
        {
            return local.isTumbling;
        }

        public void Tick(bool localCanConvene, long readyAtUnixMs, long nowUnixMs, int rightsRemaining)
        {
            VisualState? visual = GetLiveVisualOrNull();
            visual?.Press.Tick(Time.deltaTime);
            TickHold(visual, localCanConvene, readyAtUnixMs, nowUnixMs, rightsRemaining);
            visual?.Press.Sync(_hold.IsCharging);
        }

        private void TickHold(VisualState? visual, bool localCanConvene,
            long readyAtUnixMs, long nowUnixMs, int rightsRemaining)
        {
            if (visual == null) { _isLocalPlayerNear = false; _hold.Reset(); return; }
            PlayerAvatar local = PlayerAvatar.instance;
            if (local == null) { _isLocalPlayerNear = false; _hold.Reset(); return; }

            float dist = Vector3.Distance(local.transform.position, visual.Root.transform.position);
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
                            string prompt = visual.UsesGrabInteract
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

                InputKey inputKey = visual.UsesGrabInteract ? InputKey.Grab : InputKey.Interact;
                bool inputHeld = SemiFunc.InputHold(inputKey);
                bool inputReleased = SemiFunc.InputUp(inputKey);
                bool aimed = !visual.UsesGrabInteract || IsAimingAtButton(local, visual);
                bool suppressed = readyAtUnixMs - nowUnixMs > 0;
                bool otherwiseEligible = localCanConvene && !suppressed && aimed && !IsLocalTumbling(local);
                bool wasCharging = _hold.IsCharging;
                bool fired = _hold.Tick(otherwiseEligible && inputHeld, Time.deltaTime);
                if (fired)
                {
                    WLog.Line("meeting_button_pressed", secret: false,
                        ("localGate", localCanConvene),
                        ("mode", visual.UsesGrabInteract ? "grab" : "interact"));
                    OnConvene?.Invoke();
                }
                else if (wasCharging && inputReleased && otherwiseEligible)
                {
                    WLog.Line("meeting_button_hold_incomplete", secret: false,
                        ("mode", visual.UsesGrabInteract ? "grab" : "interact"));
                    OnIncompleteHold?.Invoke();
                }
            }
            catch (Exception e)
            {
                WLog.Line("meeting_button_tick_error", secret: false, ("err", e.Message));
            }
        }

        private VisualState BuildVisual()
        {
            VisualState? fromShop = TryBuildFromShopButtonTemplate();
            if (fromShop != null) return fromShop;
            GameObject? fromBundle = TryBuildFromAssetBundle();
            GameObject root = fromBundle ?? BuildPrimitive();
            return new VisualState(root, usesGrabInteract: false, new MeetingButtonPressVisual());
        }

        private VisualState? TryBuildFromShopButtonTemplate()
        {
            GameObject? instance = null;
            try
            {
                if (!MeetingButtonAssetResolver.TryResolve())
                {
                    return null;
                }
                GameObject? template = MeetingButtonAssetResolver.Template;
                if (template == null) return null;

                instance = UnityEngine.Object.Instantiate(template);
                instance.name = "WW_MeetingButton_ShopClone";

                MeshRenderer? mr = instance.GetComponent<MeshRenderer>()
                    ?? instance.GetComponentInChildren<MeshRenderer>(true);

                AnimationCurve? curve = MeetingButtonAssetResolver.PressCurve;
                var press = new MeetingButtonPressVisual();
                press.Bind(instance.transform, mr, curve);
                return new VisualState(instance, usesGrabInteract: true, press);
            }
            catch (System.Exception e)
            {
                WLog.Line("meeting_button_shop_fallback", secret: false, ("err", e.Message));
                if (instance != null) UnityEngine.Object.Destroy(instance);
                return null;
            }
        }

        private VisualState? GetLiveVisualOrNull()
        {
            VisualState? visual = _visual;
            if (visual == null) return null;
            if (visual.Root != null) return visual;
            _visual = null;
            ResetVisualState(visual);
            return null;
        }

        private void ResetVisualState(VisualState? visual)
        {
            _isLocalPlayerNear = false;
            _hold.Reset();
            visual?.Press.Clear();
        }

        private GameObject? TryBuildFromAssetBundle()
        {
            GameObject? instance = null;
            try
            {
                GameObject? prefab = PrefabResolver != null ? PrefabResolver("MeetingButton") : null;
                if (prefab == null) return null;

                instance = UnityEngine.Object.Instantiate(prefab);
                instance.name = "WW_MeetingButton";

                foreach (Collider col in instance.GetComponentsInChildren<Collider>(true))
                {
                    UnityEngine.Object.Destroy(col);
                }

                return instance;
            }
            catch (Exception e)
            {
                if (instance != null) UnityEngine.Object.Destroy(instance);
                WLog.Line("meeting_button_bundle_fallback", secret: false, ("err", e.Message));
                return null;
            }
        }

        private GameObject BuildPrimitive()
        {
            GameObject? go = null;
            try
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
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
            catch
            {
                if (go != null) UnityEngine.Object.Destroy(go);
                throw;
            }
        }
    }
}
