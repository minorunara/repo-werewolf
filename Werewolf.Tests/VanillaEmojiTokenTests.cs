using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class VanillaEmojiTokenTests
    {
        [Fact]
        public void TryReadAt_ReadsNameAndPositionAfterToken()
        {
            Assert.True(VanillaEmojiToken.TryReadAt("a[[yawn]]b", 1, out string name, out int next));
            Assert.Equal("yawn", name);
            Assert.Equal(9, next);
            Assert.Equal('b', "a[[yawn]]b"[next]);
        }

        [Theory]
        [InlineData("[[yawn]]", 1)]
        [InlineData("[yawn]]", 0)]
        [InlineData("[[yawn", 0)]
        [InlineData("", 0)]
        [InlineData(null, 0)]
        public void TryReadAt_RejectsNonTokens(string text, int index)
        {
            Assert.False(VanillaEmojiToken.TryReadAt(text, index, out string name, out int next));
            Assert.Null(name);
            Assert.Equal(index, next);
        }

        [Fact]
        public void TryReadAt_AllowsEmptyName()
        {
            Assert.True(VanillaEmojiToken.TryReadAt("[[]]", 0, out string name, out int next));
            Assert.Equal(string.Empty, name);
            Assert.Equal(4, next);
        }

        [Theory]
        [InlineData("[[yawn]]", true, "yawn")]
        [InlineData("[[yawn]] ", false, null)]
        [InlineData(" [[yawn]]", false, null)]
        [InlineData("[[a]][[b]]", false, null)]
        [InlineData("yawn", false, null)]
        public void TryParseWhole_AcceptsOnlyASingleToken(string text, bool expected, string expectedName)
        {
            Assert.Equal(expected, VanillaEmojiToken.TryParseWhole(text, out string name));
            Assert.Equal(expectedName, name);
        }

        [Fact]
        public void Names_EnumeratesInOrderAndReportsUnclosedAsNull()
        {
            Assert.Equal(new[] { "a", "b" }, VanillaEmojiToken.Names("x[[a]]y[[b]]z").ToArray());
            Assert.Equal(new[] { "a", null }, VanillaEmojiToken.Names("[[a]] [[b").ToArray());
            Assert.Empty(VanillaEmojiToken.Names("no tokens"));
            Assert.Empty(VanillaEmojiToken.Names(null));
        }

        [Theory]
        [InlineData("サイロ化を解消しよう[[;)]]", "サイロ化を解消しよう")]
        [InlineData("[[a]]x[[b]]", "x")]
        [InlineData("[[open", "[[open")]
        [InlineData("plain", "plain")]
        [InlineData(null, "")]
        public void Strip_RemovesWholeTokensOnly(string text, string expected)
        {
            Assert.Equal(expected, VanillaEmojiToken.Strip(text));
        }

        [Fact]
        public void Wrap_RoundTripsThroughTryParseWhole()
        {
            string token = VanillaEmojiToken.Wrap("extraction");

            Assert.Equal("[[extraction]]", token);
            Assert.True(VanillaEmojiToken.TryParseWhole(token, out string name));
            Assert.Equal("extraction", name);
        }
    }
}
