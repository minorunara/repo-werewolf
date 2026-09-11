using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Werewolf.UI
{
    internal sealed class UiPolyline : MaskableGraphic
    {
        private readonly List<Vector2> _points = new List<Vector2>();
        private float _thickness = 3f;

        public float Thickness
        {
            get => _thickness;
            set
            {
                if (Mathf.Approximately(_thickness, value)) return;
                _thickness = value;
                SetVerticesDirty();
            }
        }

        public void SetPoints(List<Vector2> points)
        {
            _points.Clear();
            if (points != null) _points.AddRange(points);
            SetVerticesDirty();
        }

        public void Clear()
        {
            _points.Clear();
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_points.Count < 2) return;
            Color32 c = color;
            float h = _thickness * 0.5f;
            for (int i = 1; i < _points.Count; i++)
            {
                Vector2 a = _points[i - 1];
                Vector2 b = _points[i];
                Vector2 d = b - a;
                float len = d.magnitude;
                if (len < 1e-3f) continue;
                Vector2 n = new Vector2(-d.y, d.x) * (h / len);
                AddQuad(vh, a - n, a + n, b + n, b - n, c);
            }
            for (int i = 1; i < _points.Count - 1; i++)
            {
                Vector2 p = _points[i];
                AddQuad(vh,
                    new Vector2(p.x - h, p.y - h), new Vector2(p.x - h, p.y + h),
                    new Vector2(p.x + h, p.y + h), new Vector2(p.x + h, p.y - h), c);
            }
        }

        private static void AddQuad(VertexHelper vh, Vector2 v0, Vector2 v1, Vector2 v2, Vector2 v3, Color32 c)
        {
            int vi = vh.currentVertCount;
            vh.AddVert(v0, c, Vector2.zero);
            vh.AddVert(v1, c, Vector2.zero);
            vh.AddVert(v2, c, Vector2.zero);
            vh.AddVert(v3, c, Vector2.zero);
            vh.AddTriangle(vi, vi + 1, vi + 2);
            vh.AddTriangle(vi, vi + 2, vi + 3);
        }
    }
}
