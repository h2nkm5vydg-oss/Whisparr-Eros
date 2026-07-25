using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Qualities
{
    [TestFixture]
    public class QualityFixture : CoreTest
    {
        public static object[] FromIntCases =
                {
                        new object[] { 0, Quality.Unknown },
                        new object[] { 1, Quality.SDTV },
                        new object[] { 2, Quality.DVD },
                        new object[] { 3, Quality.WEBDL1080p },
                        new object[] { 4, Quality.HDTV720p },
                        new object[] { 5, Quality.WEBDL720p },
                        new object[] { 6, Quality.Bluray720p },
                        new object[] { 7, Quality.Bluray1080p },
                        new object[] { 8, Quality.WEBDL480p },
                        new object[] { 9, Quality.HDTV1080p },

                        // new object[] {10, Quality.RAWHD},
                        new object[] { 16, Quality.HDTV2160p },
                        new object[] { 18, Quality.WEBDL2160p },
                        new object[] { 19, Quality.Bluray2160p },
                        new object[] { 32, Quality.VR },
                        new object[] { 37, Quality.VR4K },
                        new object[] { 38, Quality.VR5K },
                        new object[] { 39, Quality.VR6K },
                        new object[] { 40, Quality.VR8K },
                        new object[] { 41, Quality.VR12K },
                };

        public static object[] ToIntCases =
                {
                        new object[] { Quality.Unknown, 0 },
                        new object[] { Quality.SDTV, 1 },
                        new object[] { Quality.DVD, 2 },
                        new object[] { Quality.WEBDL1080p, 3 },
                        new object[] { Quality.HDTV720p, 4 },
                        new object[] { Quality.WEBDL720p, 5 },
                        new object[] { Quality.Bluray720p, 6 },
                        new object[] { Quality.Bluray1080p, 7 },
                        new object[] { Quality.WEBDL480p, 8 },
                        new object[] { Quality.HDTV1080p, 9 },

                        // new object[] {Quality.RAWHD, 10},
                        new object[] { Quality.HDTV2160p, 16 },
                        new object[] { Quality.WEBDL2160p, 18 },
                        new object[] { Quality.Bluray2160p, 19 },
                        new object[] { Quality.VR, 32 },
                        new object[] { Quality.VR4K, 37 },
                        new object[] { Quality.VR5K, 38 },
                        new object[] { Quality.VR6K, 39 },
                        new object[] { Quality.VR8K, 40 },
                        new object[] { Quality.VR12K, 41 },
                };

        [Test]
        [TestCaseSource("FromIntCases")]
        public void should_be_able_to_convert_int_to_qualityTypes(int source, Quality expected)
        {
            var quality = (Quality)source;
            quality.Should().Be(expected);
        }

        [Test]
        [TestCaseSource("ToIntCases")]
        public void should_be_able_to_convert_qualityTypes_to_int(Quality source, int expected)
        {
            var i = (int)source;
            i.Should().Be(expected);
        }

        [Test]
        public void should_have_unique_quality_ids_and_source_resolution_pairs()
        {
            Quality.All.Select(quality => quality.Id).Should().OnlyHaveUniqueItems();
            Quality.All.Select(quality => new { quality.Source, quality.Resolution }).Should().OnlyHaveUniqueItems();
        }

        public static List<QualityProfileQualityItem> GetDefaultQualities(params Quality[] allowed)
        {
            var qualities = new List<Quality>
            {
                Quality.Unknown,
                Quality.SDTV,
                Quality.DVD,
                Quality.DVDR,
                Quality.HDTV720p,
                Quality.HDTV1080p,
                Quality.HDTV2160p,
                Quality.WEBDL480p,
                Quality.WEBDL720p,
                Quality.WEBDL1080p,
                Quality.WEBDL2160p,
                Quality.Bluray480p,
                Quality.Bluray576p,
                Quality.Bluray720p,
                Quality.Bluray1080p,
                Quality.Bluray2160p,
                Quality.BRDISK,
                Quality.RAWHD
            };

            if (allowed.Length == 0)
            {
                allowed = qualities.ToArray();
            }

            var items = qualities
                .Except(allowed)
                .Concat(allowed)
                .Select(v => new QualityProfileQualityItem
                {
                    Quality = v,
                    Allowed = allowed.Contains(v)
                }).ToList();

            return items;
        }
    }
}
