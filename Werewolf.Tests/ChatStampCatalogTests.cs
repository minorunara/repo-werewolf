using System.Collections.Generic;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ChatStampCatalogTests
    {
        [Fact]
        public void Ids_AreUniqueAndNonZero()
        {
            var seen = new HashSet<byte>();
            foreach (ChatStampDef def in ChatStampCatalog.All)
            {
                Assert.NotEqual(0, def.Id);
                Assert.True(seen.Add(def.Id), $"スタンプID {def.Id} が重複している");
            }
        }

        [Fact]
        public void TryGet_RoundtripsAllDefs()
        {
            foreach (ChatStampDef def in ChatStampCatalog.All)
            {
                Assert.True(ChatStampCatalog.TryGet(def.Id, out ChatStampDef found));
                Assert.Equal(def.TextId, found.TextId);
                Assert.Equal(def.Category, found.Category);
            }
        }

        [Fact]
        public void TryGet_UnknownId_Fails()
        {
            Assert.False(ChatStampCatalog.TryGet(0, out _));
            Assert.False(ChatStampCatalog.TryGet(255, out _));
        }

        [Fact]
        public void EveryCategory_HasAtLeastOneStamp()
        {
            foreach (ChatStampCategoryDef cat in ChatStampCatalog.CategoryDefs)
            {
                Assert.NotEmpty(ChatStampCatalog.ForCategory(cat.Category));
            }
        }

        [Fact]
        public void Categories_CoverAllDefs()
        {
            int total = 0;
            foreach (ChatStampCategoryDef cat in ChatStampCatalog.CategoryDefs)
            {
                total += ChatStampCatalog.ForCategory(cat.Category).Count;
            }
            Assert.Equal(ChatStampCatalog.All.Count, total);
        }

        [Fact]
        public void Emojis_AreEmptyOrSubstitutableSprites()
        {
            foreach (ChatStampDef def in ChatStampCatalog.All)
            {
                if (def.Emoji.Length == 0) continue;
                Assert.True(ChatEmoji.TryReadSpriteAt(def.Emoji, 0, out _, out int next) && next == def.Emoji.Length,
                    $"スタンプID {def.Id} の絵文字 {def.Emoji} は ChatEmoji.SpriteKeys の単一絵文字でない");
            }
        }

        [Fact]
        public void ChatText_KnownId_ComposesEmojiAndText()
        {
            Assert.True(ChatStampCatalog.TryGet(20, out ChatStampDef def));
            Assert.NotEmpty(def.Emoji);
            Assert.Equal(def.Emoji + " " + Texts.Get(def.TextId), ChatStampCatalog.ChatText(20));
        }

        [Fact]
        public void ChatText_UnknownId_FallsBackToUnknownText()
        {
            Assert.Equal(Texts.Get(TextId.ChatStampUnknown), ChatStampCatalog.ChatText(200));
        }

        [Fact]
        public void ReplayText_OmitsEmoji()
        {
            Assert.Equal(Texts.Get(TextId.ChatStampClaimTheyDidIt), ChatStampCatalog.ReplayText(20));
            Assert.Equal(Texts.Get(TextId.ChatStampUnknown), ChatStampCatalog.ReplayText(200));
        }
    }
}
