using System;
using System.Collections.Generic;

namespace Werewolf.Core
{
    public sealed class OverheadIdGate
    {
        public const float ConeDegrees = 12f;

        public const float LingerSeconds = 0.3f;

        public const float FadeInSeconds = 0.12f;

        public const float FadeOutSeconds = 0.25f;

        private sealed class ActorState
        {
            public float Alpha;
            public float SecondsSinceVisible = float.MaxValue;
        }

        private readonly float _cosThreshold;
        private readonly Dictionary<int, ActorState> _states = new Dictionary<int, ActorState>();

        public OverheadIdGate(float coneDegrees = ConeDegrees)
        {
            float deg = coneDegrees > 0f ? coneDegrees : ConeDegrees;
            _cosThreshold = (float)Math.Cos(deg * Math.PI / 180.0);
        }

        public float Tick(int actor, bool bodyVisible,
            float forwardX, float forwardY, float forwardZ,
            float toTargetX, float toTargetY, float toTargetZ,
            float deltaSeconds)
        {
            if (!_states.TryGetValue(actor, out ActorState state))
            {
                state = new ActorState();
                _states[actor] = state;
            }

            float dt = deltaSeconds > 0f ? deltaSeconds : 0f;
            bool inCone = bodyVisible
                && WithinCone(forwardX, forwardY, forwardZ, toTargetX, toTargetY, toTargetZ);
            if (inCone)
            {
                state.SecondsSinceVisible = 0f;
                state.Alpha = Math.Min(1f, state.Alpha + dt / FadeInSeconds);
            }
            else
            {
                if (state.SecondsSinceVisible < float.MaxValue)
                {
                    state.SecondsSinceVisible += dt;
                }
                if (state.SecondsSinceVisible > LingerSeconds)
                {
                    state.Alpha = Math.Max(0f, state.Alpha - dt / FadeOutSeconds);
                }
            }
            return state.Alpha;
        }

        public void Reset()
        {
            _states.Clear();
        }

        private bool WithinCone(float fx, float fy, float fz, float tx, float ty, float tz)
        {
            float forwardLen = (float)Math.Sqrt(fx * fx + fy * fy + fz * fz);
            float targetLen = (float)Math.Sqrt(tx * tx + ty * ty + tz * tz);
            if (forwardLen < 1e-6f || targetLen < 1e-6f) return false;
            float dot = (fx * tx + fy * ty + fz * tz) / (forwardLen * targetLen);
            return dot >= _cosThreshold;
        }
    }
}
