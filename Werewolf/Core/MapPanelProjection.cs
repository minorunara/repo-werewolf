using System;

namespace Werewolf.Core
{
    public readonly struct MapPanelProjection
    {
        public readonly bool Valid;
        private readonly float _mapScale;
        private readonly float _originMiniX;
        private readonly float _originMiniZ;
        private readonly float _camMiniX;
        private readonly float _camMiniZ;
        private readonly float _pxPerMiniX;
        private readonly float _pxPerMiniZ;

        public MapPanelProjection(
            float mapScale, float originMiniX, float originMiniZ,
            float camMiniX, float camMiniZ,
            float orthoSize, float aspect,
            float panelWidth, float panelHeight)
        {
            Valid = mapScale > 1e-6f && orthoSize > 1e-3f && aspect > 1e-3f
                && panelWidth > 0f && panelHeight > 0f;
            _mapScale = mapScale;
            _originMiniX = originMiniX;
            _originMiniZ = originMiniZ;
            _camMiniX = camMiniX;
            _camMiniZ = camMiniZ;
            _pxPerMiniX = Valid ? panelWidth * 0.5f / (orthoSize * aspect) : 0f;
            _pxPerMiniZ = Valid ? panelHeight * 0.5f / orthoSize : 0f;
        }

        public float PanelX(float worldX)
            => (worldX * _mapScale + _originMiniX - _camMiniX) * _pxPerMiniX;

        public float PanelY(float worldZ)
            => (worldZ * _mapScale + _originMiniZ - _camMiniZ) * _pxPerMiniZ;

        public static MapPanelProjection FromWorldRect(
            float minX, float maxX, float minZ, float maxZ,
            float panelWidth, float panelHeight)
        {
            float halfW = (maxX - minX) * 0.5f;
            float halfH = (maxZ - minZ) * 0.5f;
            if (halfW <= 1e-3f || halfH <= 1e-3f) return default;
            return new MapPanelProjection(
                1f, 0f, 0f,
                (minX + maxX) * 0.5f, (minZ + maxZ) * 0.5f,
                halfH, halfW / halfH,
                panelWidth, panelHeight);
        }
    }
}
