using System;

namespace Werewolf.Core
{
    public readonly struct CanvasBounds : IEquatable<CanvasBounds>
    {
        public CanvasBounds(float width, float height)
        {
            Width = width;
            Height = height;
        }

        public float Width { get; }

        public float Height { get; }

        public float Left => -Width * 0.5f;

        public float Right => Width * 0.5f;

        public float Bottom => -Height * 0.5f;

        public float Top => Height * 0.5f;

        public bool IsMeasured => Width >= 1f && Height >= 1f;

        public bool Equals(CanvasBounds other) => Width == other.Width && Height == other.Height;

        public override bool Equals(object obj) => obj is CanvasBounds other && Equals(other);

        public override int GetHashCode() => unchecked(Width.GetHashCode() * 397 ^ Height.GetHashCode());

        public static bool operator ==(CanvasBounds left, CanvasBounds right) => left.Equals(right);

        public static bool operator !=(CanvasBounds left, CanvasBounds right) => !left.Equals(right);

        public override string ToString() => $"{Width}x{Height}";
    }
}
