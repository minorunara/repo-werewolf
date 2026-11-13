#nullable enable
using System;

namespace Werewolf.Core
{
    public sealed class TextRefresh
    {
        private readonly string _owner;
        private Func<bool>? _alive;
        private Action? _apply;
        private bool _subscribed;

        public TextRefresh(string owner)
        {
            _owner = owner;
        }

        public bool IsSubscribed => _subscribed;

        public void Subscribe(Func<bool> alive, Action apply)
        {
            _alive = alive ?? throw new ArgumentNullException(nameof(alive));
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
            if (_subscribed) return;
            Texts.Changed += OnChanged;
            _subscribed = true;
        }

        public void Subscribe(Action apply) => Subscribe(AlwaysAlive, apply);

        private static bool AlwaysAlive() => true;

        public void Unsubscribe()
        {
            if (_subscribed)
            {
                Texts.Changed -= OnChanged;
                _subscribed = false;
            }
            _alive = null;
            _apply = null;
        }

        private void OnChanged()
        {
            Func<bool>? alive = _alive;
            Action? apply = _apply;
            if (alive == null || apply == null) return;
            try
            {
                if (alive()) apply();
            }
            catch (Exception e)
            {
                WLog.Exception("panel_text_refresh_error", e, secret: false, ("panel", _owner));
            }
        }
    }
}
