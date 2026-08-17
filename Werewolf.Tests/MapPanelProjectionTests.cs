using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class MapPanelProjectionTests
    {
        [Fact]
        public void Projects_WorldToPanel_LinearMapping()
        {
            var proj = new MapPanelProjection(
                mapScale: 0.1f, originMiniX: 2f, originMiniZ: 3f,
                camMiniX: 2f, camMiniZ: 3f,
                orthoSize: 36f, aspect: 5f / 3f,
                panelWidth: 1200f, panelHeight: 720f);
            Assert.True(proj.Valid);
            Assert.Equal(0f, proj.PanelX(0f), 3);
            Assert.Equal(10f, proj.PanelX(10f), 3);
            Assert.Equal(-7.2f, proj.PanelY(-7.2f), 3);
        }

        [Fact]
        public void CameraOffset_ShiftsPanelOrigin()
        {
            var proj = new MapPanelProjection(
                mapScale: 0.1f, originMiniX: 0f, originMiniZ: 0f,
                camMiniX: 1f, camMiniZ: 0f,
                orthoSize: 36f, aspect: 5f / 3f,
                panelWidth: 1200f, panelHeight: 720f);
            Assert.Equal(0f, proj.PanelX(10f), 3);
        }

        [Fact]
        public void InvalidInputs_AreFlagged()
        {
            Assert.False(new MapPanelProjection(0f, 0, 0, 0, 0, 36f, 1.6f, 1200f, 720f).Valid);
            Assert.False(new MapPanelProjection(0.1f, 0, 0, 0, 0, 0f, 1.6f, 1200f, 720f).Valid);
            Assert.False(new MapPanelProjection(0.1f, 0, 0, 0, 0, 36f, 0f, 1200f, 720f).Valid);
            Assert.False(new MapPanelProjection(0.1f, 0, 0, 0, 0, 36f, 1.6f, 0f, 720f).Valid);
        }

        [Fact]
        public void FromWorldRect_MapsCornersToPanelCorners()
        {
            MapPanelProjection proj = MapPanelProjection.FromWorldRect(
                -100f, 60f, -30f, 50f, 1200f, 720f);
            Assert.True(proj.Valid);
            Assert.Equal(-600f, proj.PanelX(-100f), 3);
            Assert.Equal(600f, proj.PanelX(60f), 3);
            Assert.Equal(0f, proj.PanelX(-20f), 3);
            Assert.Equal(-360f, proj.PanelY(-30f), 3);
            Assert.Equal(360f, proj.PanelY(50f), 3);
        }

        [Fact]
        public void FromWorldRect_DegenerateRect_IsInvalid()
        {
            Assert.False(MapPanelProjection.FromWorldRect(5f, 5f, -30f, 50f, 1200f, 720f).Valid);
            Assert.False(MapPanelProjection.FromWorldRect(-100f, 60f, 7f, 7f, 1200f, 720f).Valid);
        }
    }
}
