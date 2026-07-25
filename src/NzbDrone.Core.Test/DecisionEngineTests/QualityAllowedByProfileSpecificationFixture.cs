using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]

    public class QualityAllowedByProfileSpecificationFixture : CoreTest<QualityAllowedByProfileSpecification>
    {
        private RemoteMovie _remoteMovie;

        public static object[] AllowedTestCases =
        {
            new object[] { Quality.DVD },
            new object[] { Quality.HDTV720p },
            new object[] { Quality.Bluray1080p }
        };

        public static object[] DeniedTestCases =
        {
            new object[] { Quality.SDTV },
            new object[] { Quality.WEBDL720p },
            new object[] { Quality.Bluray720p }
        };

        [SetUp]
        public void Setup()
        {
            var fakeSeries = Builder<Movie>.CreateNew()
                .With(c => c.QualityProfile = new QualityProfile { Cutoff = Quality.Bluray1080p.Id })
                         .Build();

            _remoteMovie = new RemoteMovie
            {
                Movie = fakeSeries,
                ParsedMovieInfo = new ParsedMovieInfo { Quality = new QualityModel(Quality.DVD, new Revision(version: 2)) },
            };
        }

        [Test]
        [TestCaseSource("AllowedTestCases")]
        public void should_allow_if_quality_is_defined_in_profile(Quality qualityType)
        {
            _remoteMovie.ParsedMovieInfo.Quality.Quality = qualityType;
            _remoteMovie.Movie.QualityProfile.Items = Qualities.QualityFixture.GetDefaultQualities(Quality.DVD, Quality.HDTV720p, Quality.Bluray1080p);

            Subject.IsSatisfiedBy(_remoteMovie, null).Accepted.Should().BeTrue();
        }

        [Test]
        [TestCaseSource("DeniedTestCases")]
        public void should_not_allow_if_quality_is_not_defined_in_profile(Quality qualityType)
        {
            _remoteMovie.ParsedMovieInfo.Quality.Quality = qualityType;
            _remoteMovie.Movie.QualityProfile.Items = Qualities.QualityFixture.GetDefaultQualities(Quality.DVD, Quality.HDTV720p, Quality.Bluray1080p);

            Subject.IsSatisfiedBy(_remoteMovie, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_allow_vr_resolution_variant_in_vr_profile()
        {
            _remoteMovie.ParsedMovieInfo.Quality.Quality = Quality.VR8K;
            _remoteMovie.Movie.QualityProfile.Items = new List<QualityProfileQualityItem>
            {
                new QualityProfileQualityItem { Quality = Quality.VR, Allowed = true },
                new QualityProfileQualityItem { Quality = Quality.VR4K, Allowed = true },
                new QualityProfileQualityItem { Quality = Quality.VR5K, Allowed = true },
                new QualityProfileQualityItem { Quality = Quality.VR6K, Allowed = true },
                new QualityProfileQualityItem { Quality = Quality.VR8K, Allowed = true },
                new QualityProfileQualityItem { Quality = Quality.VR12K, Allowed = true }
            };

            Subject.IsSatisfiedBy(_remoteMovie, null).Accepted.Should().BeTrue();
        }
    }
}
