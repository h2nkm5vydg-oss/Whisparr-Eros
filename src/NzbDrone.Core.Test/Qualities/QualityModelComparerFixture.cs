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
    public class QualityModelComparerFixture : CoreTest
    {
        public QualityModelComparer Subject { get; set; }

        [SetUp]
        public void Setup()
        {
        }

        private void GivenDefaultProfile()
        {
            Subject = new QualityModelComparer(new QualityProfile { Items = QualityFixture.GetDefaultQualities() });
        }

        private void GivenCustomProfile()
        {
            Subject = new QualityModelComparer(new QualityProfile { Items = QualityFixture.GetDefaultQualities(Quality.Bluray720p, Quality.DVD) });
        }

        private void GivenGroupedProfile()
        {
            var profile = new QualityProfile
            {
                Items = new List<QualityProfileQualityItem>
                                      {
                                          new QualityProfileQualityItem
                                          {
                                              Allowed = false,
                                              Quality = Quality.SDTV
                                          },
                                          new QualityProfileQualityItem
                                          {
                                              Allowed = false,
                                              Quality = Quality.DVD
                                          },
                                          new QualityProfileQualityItem
                                          {
                                              Allowed = true,
                                              Items = new List<QualityProfileQualityItem>
                                                      {
                                                          new QualityProfileQualityItem
                                                          {
                                                              Allowed = true,
                                                              Quality = Quality.HDTV720p
                                                          },
                                                          new QualityProfileQualityItem
                                                          {
                                                              Allowed = true,
                                                              Quality = Quality.WEBDL720p
                                                          }
                                                      }
                                          },
                                          new QualityProfileQualityItem
                                          {
                                              Allowed = true,
                                              Quality = Quality.Bluray720p
                                          }
                                      }
            };

            Subject = new QualityModelComparer(profile);
        }

        [Test]
        public void should_be_greater_when_first_quality_is_greater_than_second()
        {
            GivenDefaultProfile();

            var first = new QualityModel(Quality.Bluray1080p);
            var second = new QualityModel(Quality.DVD);

            var compare = Subject.Compare(first, second);

            compare.Should().BeGreaterThan(0);
        }

        [Test]
        public void should_be_lesser_when_second_quality_is_greater_than_first()
        {
            GivenDefaultProfile();

            var first = new QualityModel(Quality.DVD);
            var second = new QualityModel(Quality.Bluray1080p);

            var compare = Subject.Compare(first, second);

            compare.Should().BeLessThan(0);
        }

        [Test]
        public void should_be_greater_when_first_quality_is_a_proper_for_the_same_quality()
        {
            GivenDefaultProfile();

            var first = new QualityModel(Quality.Bluray1080p, new Revision(version: 2));
            var second = new QualityModel(Quality.Bluray1080p, new Revision(version: 1));

            var compare = Subject.Compare(first, second);

            compare.Should().BeGreaterThan(0);
        }

        [Test]
        public void should_be_greater_when_using_a_custom_profile()
        {
            GivenCustomProfile();

            var first = new QualityModel(Quality.DVD);
            var second = new QualityModel(Quality.Bluray720p);

            var compare = Subject.Compare(first, second);

            compare.Should().BeGreaterThan(0);
        }

        [Test]
        public void should_ignore_group_order_by_default()
        {
            GivenGroupedProfile();

            var first = new QualityModel(Quality.HDTV720p);
            var second = new QualityModel(Quality.WEBDL720p);

            var compare = Subject.Compare(first, second);

            compare.Should().Be(0);
        }

        [Test]
        public void should_respect_group_order()
        {
            GivenGroupedProfile();

            var first = new QualityModel(Quality.HDTV720p);
            var second = new QualityModel(Quality.WEBDL720p);

            var compare = Subject.Compare(first, second, true);

            compare.Should().BeLessThan(0);
        }

        [Test]
        public void should_order_vr_resolution_variants_for_upgrades()
        {
            var qualities = new[]
            {
                Quality.VR,
                Quality.VR4K,
                Quality.VR5K,
                Quality.VR6K,
                Quality.VR8K,
                Quality.VR12K
            };

            Subject = new QualityModelComparer(new QualityProfile
            {
                Items = qualities.Select(quality => new QualityProfileQualityItem
                {
                    Quality = quality,
                    Allowed = true
                }).ToList()
            });

            for (var i = 1; i < qualities.Length; i++)
            {
                Subject.Compare(new QualityModel(qualities[i]), new QualityModel(qualities[i - 1]))
                       .Should().BeGreaterThan(0);
            }
        }
    }
}
