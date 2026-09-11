using UnityEngine;
using Werewolf.Core;

namespace Werewolf.Vanilla
{
    internal sealed class MeetingButtonPressVisual
    {
        private const float PressedScaleY = 0.5f;
        private static readonly Color PressColorStart = new Color(1f, 0.5f, 0f, 1f);

        private readonly ButtonPressState _state = new ButtonPressState();
        private Transform _target;
        private MeshRenderer _renderer;
        private AnimationCurve _curve;

        public void Bind(Transform target, MeshRenderer renderer, AnimationCurve curve)
        {
            _target = target;
            _renderer = renderer;
            _curve = curve;
            _state.Reset();
        }

        public void Clear()
        {
            _target = null;
            _renderer = null;
            _curve = null;
            _state.Reset();
        }

        public void Tick(float deltaSeconds)
        {
            if (!_state.Tick(deltaSeconds)) return;
            if (_target == null || _curve == null) return;
            float t = _curve.Evaluate(_state.ReleaseEval);
            SetEmission(Color.Lerp(PressColorStart, Color.white, t));
            _target.localScale = new Vector3(1f, Mathf.Clamp(t, PressedScaleY, 1f), 1f);
        }

        public void Sync(bool charging)
        {
            if (!_state.Sync(charging)) return;
            if (_target == null) return;
            _target.localScale = new Vector3(1f, PressedScaleY, 1f);
            SetEmission(PressColorStart);
        }

        private void SetEmission(Color color)
        {
            if (_renderer != null && _renderer.material != null)
            {
                _renderer.material.SetColor("_EmissionColor", color);
            }
        }
    }
}
