using System.IO;
using Xunit;

namespace Werewolf.Tests
{
    public class PreplayAssetTests
    {
        [Fact]
        public void CardBackAsset_IsSquareAndMatchesRoleCardDimensions()
        {
            string assets = Path.Combine(AuditEnvironment.Meta("Werewolf.SourceDir"), "Assets");
            byte[] back = File.ReadAllBytes(Path.Combine(assets, "role_card_back.png"));
            byte[] role = File.ReadAllBytes(Path.Combine(assets, "role_shaman.png"));
            Assert.Equal(PngSize(role), PngSize(back));
            Assert.Equal((512, 512), PngSize(back));
        }

        private static (int Width, int Height) PngSize(byte[] png)
        {
            Assert.True(png.Length >= 24);
            int width = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
            int height = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
            return (width, height);
        }
    }
}
