using System;
using System.Collections.Generic;
using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class WorldgenSpecTests
    {

        [Fact]
        public void Encode_SortsNamesAscendingOrdinal()
        {
            var map = new Dictionary<string, int>
            {
                ["banana"] = 2,
                ["apple"] = 1,
                ["Cherry"] = 3,
            };

            Assert.Equal("Cherry:3,apple:1,banana:2", WorldgenSpec.Encode(map));
        }

        [Fact]
        public void Encode_ExcludesZeroAndNegativeCounts()
        {
            var map = new Dictionary<string, int>
            {
                ["keep"] = 1,
                ["zero"] = 0,
                ["negative"] = -5,
            };

            Assert.Equal("keep:1", WorldgenSpec.Encode(map));
        }

        [Fact]
        public void Encode_EmptyOrNullMap_ReturnsEmptyString()
        {
            Assert.Equal("", WorldgenSpec.Encode(new Dictionary<string, int>()));
            Assert.Equal("", WorldgenSpec.Encode(null));
        }

        [Fact]
        public void Encode_AllPositiveExcluded_ReturnsEmptyString()
        {
            var map = new Dictionary<string, int> { ["zero"] = 0 };
            Assert.Equal("", WorldgenSpec.Encode(map));
        }

        [Fact]
        public void Encode_UsesInvariantCultureForCounts()
        {
            var map = new Dictionary<string, int> { ["item"] = 12345 };
            Assert.Equal("item:12345", WorldgenSpec.Encode(map));
        }

        [Theory]
        [InlineData("has,comma")]
        [InlineData("has:colon")]
        [InlineData("has|pipe")]
        [InlineData("has;semicolon")]
        [InlineData("has=equals")]
        public void Encode_SkipsNameContainingForbiddenChar_AndReportsIt(string badName)
        {
            var map = new Dictionary<string, int>
            {
                [badName] = 2,
                ["good"] = 1,
            };
            var skipped = new List<string>();

            var spec = WorldgenSpec.Encode(map, skipped);

            Assert.Equal("good:1", spec);
            Assert.Equal(new[] { badName }, skipped);
        }

        [Fact]
        public void Encode_SkipsWhitespaceOnlyName_AndReportsIt()
        {
            var map = new Dictionary<string, int>
            {
                ["   "] = 1,
                ["good"] = 1,
            };
            var skipped = new List<string>();

            var spec = WorldgenSpec.Encode(map, skipped);

            Assert.Equal("good:1", spec);
            Assert.Equal(new[] { "   " }, skipped);
        }

        [Fact]
        public void Encode_ZeroCountForbiddenName_IsNotReportedAsSkipped()
        {
            var map = new Dictionary<string, int> { ["bad|name"] = 0 };
            var skipped = new List<string>();

            Assert.Equal("", WorldgenSpec.Encode(map, skipped));
            Assert.Empty(skipped);
        }

        [Fact]
        public void Encode_OutputNeverContainsCatalogForbiddenChars()
        {
            var map = new Dictionary<string, int>
            {
                ["Item Power Crystal"] = 3,
                ["bad|1"] = 1,
                ["bad;2"] = 1,
                ["bad=3"] = 1,
            };

            var spec = WorldgenSpec.Encode(map);

            Assert.DoesNotContain('|', spec);
            Assert.DoesNotContain(';', spec);
            Assert.DoesNotContain('=', spec);
        }

        [Fact]
        public void Encode_WithoutCollector_StillSkipsForbiddenNames()
        {
            var map = new Dictionary<string, int>
            {
                ["bad,name"] = 2,
                ["good"] = 1,
            };

            Assert.Equal("good:1", WorldgenSpec.Encode(map));
        }

        [Fact]
        public void Decode_ParsesCanonicalSpec()
        {
            var result = WorldgenSpec.Decode("Cherry:3,apple:1,banana:2");

            Assert.Equal(3, result.Count);
            Assert.Equal(3, result["Cherry"]);
            Assert.Equal(1, result["apple"]);
            Assert.Equal(2, result["banana"]);
        }

        [Fact]
        public void Decode_NullOrEmpty_ReturnsEmptyMap()
        {
            Assert.Empty(WorldgenSpec.Decode(null));
            Assert.Empty(WorldgenSpec.Decode(""));
            Assert.Empty(WorldgenSpec.Decode("   "));
        }

        [Theory]
        [InlineData("noColon")]
        [InlineData("a:1:2")]
        [InlineData("a:abc")]
        [InlineData("a:")]
        [InlineData(":5")]
        [InlineData("a:0")]
        [InlineData("a:-3")]
        [InlineData(",")]
        public void Decode_SkipsMalformedToken_WithoutThrowing(string badToken)
        {
            var result = WorldgenSpec.Decode(badToken + ",good:1");

            Assert.Single(result);
            Assert.Equal(1, result["good"]);
        }

        [Fact]
        public void Decode_AllTokensMalformed_ReturnsEmptyMap()
        {
            Assert.Empty(WorldgenSpec.Decode("bad,worse:,:1,x:y"));
        }

        [Fact]
        public void Decode_NameWithInternalSpaces_IsPreserved()
        {
            var result = WorldgenSpec.Decode("Item Power Crystal:3");

            Assert.Equal(3, result["Item Power Crystal"]);
        }

        [Fact]
        public void Decode_TrimsOuterWhitespaceOfNameAndCount()
        {
            var result = WorldgenSpec.Decode(" apple : 1 , banana:2");

            Assert.Equal(2, result.Count);
            Assert.Equal(1, result["apple"]);
            Assert.Equal(2, result["banana"]);
        }

        [Fact]
        public void Decode_DuplicateName_LastWins()
        {
            var result = WorldgenSpec.Decode("a:1,a:5");

            Assert.Single(result);
            Assert.Equal(5, result["a"]);
        }

        [Fact]
        public void Decode_IntOverflow_IsSkipped()
        {
            var result = WorldgenSpec.Decode("big:99999999999999999999,good:1");

            Assert.Single(result);
            Assert.Equal(1, result["good"]);
        }

        [Fact]
        public void EncodeDecode_Roundtrip_PreservesPositiveEntries()
        {
            var map = new Dictionary<string, int>
            {
                ["Gun"] = 2,
                ["Item Power Crystal"] = 10,
                ["Health Pack Small"] = 1,
                ["excluded"] = 0,
            };

            var decoded = WorldgenSpec.Decode(WorldgenSpec.Encode(map));

            var expected = map.Where(p => p.Value > 0).ToDictionary(p => p.Key, p => p.Value);
            Assert.Equal(expected.Count, decoded.Count);
            foreach (var pair in expected)
                Assert.Equal(pair.Value, decoded[pair.Key]);
        }

        [Fact]
        public void EncodeAfterDecode_IsIdempotent_OnCanonicalString()
        {
            const string canonical = "Apple:1,banana:2,cherry:3";

            Assert.Equal(canonical, WorldgenSpec.Encode(WorldgenSpec.Decode(canonical)));
        }

        [Fact]
        public void EncodeAfterDecode_NormalizesNonCanonicalInput()
        {
            const string messy = "banana:2, Apple:1 ,junk,cherry:3";

            Assert.Equal("Apple:1,banana:2,cherry:3", WorldgenSpec.Encode(WorldgenSpec.Decode(messy)));
        }
    }
}
