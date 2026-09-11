namespace Werewolf.Core
{
    public sealed class ClickGate
    {
        public const float DragThreshold = 10f;

        private bool _pressed;
        private bool _dragged;
        private float _pressX;
        private float _pressY;

        public bool IsPressed => _pressed;

        public bool Tick(bool down, bool up, float x, float y)
        {
            if (down)
            {
                _pressed = true;
                _dragged = false;
                _pressX = x;
                _pressY = y;
            }
            if (!_pressed) return false;

            if (!_dragged)
            {
                float dx = x - _pressX;
                float dy = y - _pressY;
                if (dx * dx + dy * dy > DragThreshold * DragThreshold) _dragged = true;
            }

            if (!up) return false;
            bool click = !_dragged;
            _pressed = false;
            _dragged = false;
            return click;
        }

        public void Reset()
        {
            _pressed = false;
            _dragged = false;
        }
    }
}
