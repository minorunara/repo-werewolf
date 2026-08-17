using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Werewolf.Core.Replay;
using Xunit;

namespace Werewolf.Tests
{
    public class ReplayJsonTests
    {
        [Fact]
        public void AppendEscaped_HandlesQuotesBackslashesControlCharsAndCjk()
        {
            var sb = new StringBuilder();
            ReplayJson.AppendEscaped(sb, "壺 \"A\"\\B\n\t\u0001");
            Assert.Equal("\"壺 \\\"A\\\"\\\\B\\n\\t\\u0001\"", sb.ToString());
        }

        [Fact]
        public void AppendEscaped_NullBecomesEmptyString()
        {
            var sb = new StringBuilder();
            ReplayJson.AppendEscaped(sb, null);
            Assert.Equal("\"\"", sb.ToString());
        }

        [Fact]
        public void F2_UsesInvariantCulture_EvenUnderCommaDecimalLocale()
        {
            CultureInfo original = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
                Assert.Equal("12.25", ReplayJson.F2(12.25));
                Assert.Equal("3", ReplayJson.F2(3.0));
                Assert.Equal("-0.5", ReplayJson.F2(-0.5));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = original;
            }
        }

        [Fact]
        public void AppendValue_SerializesNestedIntLists()
        {
            var groups = new List<List<int>>
            {
                new List<int> { 1, 2, 3 },
                new List<int> { 4, 5 },
            };
            var sb = new StringBuilder();
            ReplayJson.AppendValue(sb, groups);
            Assert.Equal("[[1,2,3],[4,5]]", sb.ToString());
        }

        [Fact]
        public void AppendValue_SerializesPrimitivesAndNull()
        {
            var sb = new StringBuilder();
            ReplayJson.AppendValue(sb, null);
            sb.Append('|');
            ReplayJson.AppendValue(sb, true);
            sb.Append('|');
            ReplayJson.AppendValue(sb, 42);
            sb.Append('|');
            ReplayJson.AppendValue(sb, (byte)7);
            sb.Append('|');
            ReplayJson.AppendValue(sb, 1.5f);
            sb.Append('|');
            ReplayJson.AppendValue(sb, "x");
            Assert.Equal("null|true|42|7|1.5|\"x\"", sb.ToString());
        }

        [Fact]
        public void EventLine_WithoutFields_EmitsBareEventObject()
        {
            Assert.Equal("{\"k\":\"ev\",\"t\":13,\"e\":\"meet_warp\"}",
                ReplayJson.EventLine(13.0, "meet_warp", new (string, object)[0]));
            Assert.Equal("{\"k\":\"ev\",\"t\":13,\"e\":\"meet_warp\"}",
                ReplayJson.EventLine(13.0, "meet_warp", null));
        }
    }
}
