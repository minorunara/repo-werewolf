using System.Collections.Generic;
using System.Linq;
using Werewolf.Core;
using Xunit;

namespace Werewolf.Tests
{
    public class ManualCatalogTests
    {
        [Fact]
        public void Pages_AreNonEmpty()
        {
            Assert.True(ManualCatalog.PageCount > 0);
            Assert.Equal(ManualCatalog.Pages.Length, ManualCatalog.PageCount);
        }

        [Fact]
        public void Pages_HaveAtLeastOneBlock()
        {
            foreach (var page in ManualCatalog.Pages)
            {
                Assert.NotNull(page.Blocks);
                Assert.NotEmpty(page.Blocks);
            }
        }

        [Fact]
        public void Pages_HaveNoDuplicateTextIds()
        {
            var seen = new HashSet<TextId>();
            foreach (var page in ManualCatalog.Pages)
            {
                Assert.True(seen.Add(page.TitleId), $"TitleId 重複: {page.TitleId}");
                foreach (var block in page.Blocks)
                {
                    Assert.True(seen.Add(block.BodyId), $"BodyId 重複: {block.BodyId}");
                }
            }
        }

        [Fact]
        public void Pages_TextsAreRegistered()
        {
            foreach (var page in ManualCatalog.Pages)
            {
                Assert.False(string.IsNullOrEmpty(Texts.Get(page.TitleId)), $"タイトル未登録: {page.TitleId}");
                foreach (var block in page.Blocks)
                {
                    Assert.False(string.IsNullOrEmpty(Texts.Get(block.BodyId)), $"本文未登録: {block.BodyId}");
                }
            }
        }

        [Fact]
        public void BlockIconKeys_AreNonEmptyWhenPresent()
        {
            foreach (var page in ManualCatalog.Pages)
            {
                foreach (var block in page.Blocks)
                {
                    if (block.IconKey != null)
                    {
                        Assert.False(string.IsNullOrWhiteSpace(block.IconKey));
                    }
                }
            }
        }

        [Fact]
        public void ImageKeys_UseKnownPrefixAndAreUnique()
        {
            var keys = ManualCatalog.Pages.Where(p => p.ImageKey != null).Select(p => p.ImageKey).ToList();
            Assert.Equal(keys.Count, keys.Distinct().Count());
            Assert.All(keys, k => Assert.True(
                k.StartsWith("manual_") || k.StartsWith("role_") || k.StartsWith("perk_")
                || k.StartsWith("img_"),
                $"未知のImageKeyプレフィックス: {k}"));
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, 0)]
        [InlineData(5, 5)]
        [InlineData(int.MaxValue, -2)]
        public void ClampIndex_ClampsAtBothEnds(int input, int expected)
        {
            int resolved = expected == -2 ? ManualCatalog.PageCount - 1 : expected;
            Assert.Equal(resolved, ManualCatalog.ClampIndex(input));
        }

        [Fact]
        public void Sections_AreOrderedAndCoverEveryPage()
        {
            Assert.NotEmpty(ManualCatalog.Sections);
            Assert.Equal(0, ManualCatalog.Sections[0].StartIndex);
            Assert.All(ManualCatalog.Sections, s =>
            {
                Assert.InRange(s.StartIndex, 0, ManualCatalog.PageCount - 1);
                Assert.False(string.IsNullOrEmpty(Texts.Get(s.TitleId)));
            });
            Assert.Equal(ManualCatalog.Sections.Length,
                ManualCatalog.Sections.Select(s => s.StartIndex).Distinct().Count());
            Assert.True(ManualCatalog.Sections.Select(s => s.StartIndex).SequenceEqual(
                ManualCatalog.Sections.Select(s => s.StartIndex).OrderBy(i => i)));

            for (int i = 0; i < ManualCatalog.PageCount; i++)
            {
                int section = ManualCatalog.SectionIndexForPage(i);
                Assert.InRange(section, 0, ManualCatalog.Sections.Length - 1);
                Assert.True(ManualCatalog.Sections[section].StartIndex <= i);
            }
        }

        [Theory]
        [InlineData(0, 0, 4)]
        [InlineData(7, 0, 8)]
        [InlineData(17, 15, 21)]
        [InlineData(20, 15, 21)]
        [InlineData(27, 24, 27)]
        public void SectionNavigation_JumpsToAdjacentSectionStarts(int page, int previous, int next)
        {
            Assert.Equal(previous, ManualCatalog.PreviousSectionStart(page));
            Assert.Equal(next, ManualCatalog.NextSectionStart(page));
        }
    }
}
