using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class LangFileTests
    {
        [Fact]
        public void Parse_ValidLine_MapsEnumKeyToValue()
        {
            var result = LangFile.Parse("NoticeCatAwakened=Someone used a beacon");
            Assert.Equal("Someone used a beacon", result[TextId.NoticeCatAwakened]);
        }

        [Fact]
        public void Parse_CommentLine_IsIgnored()
        {
            var result = LangFile.Parse("# this is a comment\nNoticeCatAwakened=value");
            Assert.Single(result);
            Assert.Equal("value", result[TextId.NoticeCatAwakened]);
        }

        [Fact]
        public void Parse_EmptyLines_AreIgnored()
        {
            var result = LangFile.Parse("\n\nNoticeCatAwakened=value\n\n");
            Assert.Single(result);
            Assert.Equal("value", result[TextId.NoticeCatAwakened]);
        }

        [Fact]
        public void Parse_LineWithoutEquals_IsSkipped()
        {
            var result = LangFile.Parse("NoEqualsHere\nNoticeCatAwakened=value");
            Assert.Single(result);
            Assert.Equal("value", result[TextId.NoticeCatAwakened]);
        }

        [Fact]
        public void Parse_UnknownKey_IsIgnored()
        {
            var result = LangFile.Parse("ThisKeyDoesNotExist=value\nNoticeCatAwakened=value2");
            Assert.Single(result);
            Assert.Equal("value2", result[TextId.NoticeCatAwakened]);
        }

        [Fact]
        public void Parse_EmptyKey_IsIgnored()
        {
            var result = LangFile.Parse("=orphanvalue\nNoticeCatAwakened=value");
            Assert.Single(result);
            Assert.Equal("value", result[TextId.NoticeCatAwakened]);
        }

        [Fact]
        public void Parse_NumericStringKey_IsNotMisinterpretedAsEnumOrdinal()
        {
            var result = LangFile.Parse("0=should not map to the first enum value");
            Assert.Empty(result);
        }

        [Fact]
        public void Parse_SingleBackslashN_ExpandsToRealNewline()
        {
            var result = LangFile.Parse("ConveneCountdownHeaderFormat={0} called a meeting!\\nWarping in...");
            Assert.Equal("{0} called a meeting!\nWarping in...", result[TextId.ConveneCountdownHeaderFormat]);
        }

        [Fact]
        public void Parse_DoubleBackslashN_StaysLiteralBackslashN()
        {
            var result = LangFile.Parse("NoticeCatAwakened=literal \\\\n here");
            Assert.Equal("literal \\n here", result[TextId.NoticeCatAwakened]);
        }

        [Fact]
        public void Parse_ValueContainingEquals_SplitsOnlyOnFirstEquals()
        {
            var result = LangFile.Parse("NoticeCatAwakened=a=b=c");
            Assert.Equal("a=b=c", result[TextId.NoticeCatAwakened]);
        }

        [Fact]
        public void Parse_CrLfLineEndings_AreHandled()
        {
            var result = LangFile.Parse("NoticeCatAwakened=value\r\nNoticeNoExecution=value2\r\n");
            Assert.Equal(2, result.Count);
            Assert.Equal("value", result[TextId.NoticeCatAwakened]);
            Assert.Equal("value2", result[TextId.NoticeNoExecution]);
        }

        [Fact]
        public void Parse_NullContent_ReturnsEmptyDictionary()
        {
            Assert.Empty(LangFile.Parse(null));
        }

        [Fact]
        public void Parse_EmptyContent_ReturnsEmptyDictionary()
        {
            Assert.Empty(LangFile.Parse(string.Empty));
        }

        [Fact]
        public void Parse_KeyWithSurroundingWhitespace_IsTrimmed()
        {
            var result = LangFile.Parse("  NoticeCatAwakened  =value");
            Assert.Single(result);
            Assert.Equal("value", result[TextId.NoticeCatAwakened]);
        }
    }
}
