using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Core.MediaFiles.MovieImport.Aggregation.Aggregators;
using NzbDrone.Core.MediaFiles.MovieImport.Aggregation.Aggregators.Augmenters.Quality;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.MovieImport.Aggregation.Aggregators
{
    [TestFixture]
    public class AggregateQualityFixture : CoreTest<AggregateQuality>
    {
        private Mock<IAugmentQuality> _mediaInfoAugmenter;
        private Mock<IAugmentQuality> _fileExtensionAugmenter;
        private Mock<IAugmentQuality> _nameAugmenter;
        private Mock<IAugmentQuality> _releaseNameAugmenter;

        [SetUp]
        public void Setup()
        {
            _mediaInfoAugmenter = new Mock<IAugmentQuality>();
            _fileExtensionAugmenter = new Mock<IAugmentQuality>();
            _nameAugmenter = new Mock<IAugmentQuality>();
            _releaseNameAugmenter = new Mock<IAugmentQuality>();

            _fileExtensionAugmenter.SetupGet(s => s.Order).Returns(1);
            _nameAugmenter.SetupGet(s => s.Order).Returns(2);
            _mediaInfoAugmenter.SetupGet(s => s.Order).Returns(4);
            _releaseNameAugmenter.SetupGet(s => s.Order).Returns(5);

            _mediaInfoAugmenter.Setup(s => s.AugmentQuality(It.IsAny<LocalMovie>(), It.IsAny<DownloadClientItem>())).Returns(AugmentQualityResult.ResolutionOnly((int)Resolution.R1080p, Confidence.MediaInfo));

            _fileExtensionAugmenter.Setup(s => s.AugmentQuality(It.IsAny<LocalMovie>(), It.IsAny<DownloadClientItem>()))
                                   .Returns(new AugmentQualityResult(QualitySource.Television, Confidence.Fallback, (int)Resolution.R720p, Confidence.Fallback, new Revision(), Confidence.Fallback));

            _nameAugmenter.Setup(s => s.AugmentQuality(It.IsAny<LocalMovie>(), It.IsAny<DownloadClientItem>()))
                          .Returns(new AugmentQualityResult(QualitySource.Television, Confidence.Default, 480, Confidence.Default, new Revision(), Confidence.Default));

            _releaseNameAugmenter.Setup(s => s.AugmentQuality(It.IsAny<LocalMovie>(), It.IsAny<DownloadClientItem>()))
                                 .Returns(AugmentQualityResult.SourceOnly(QualitySource.Web, Confidence.MediaInfo));
        }

        private void GivenAugmenters(params Mock<IAugmentQuality>[] mocks)
        {
            Mocker.SetConstant<IEnumerable<IAugmentQuality>>(mocks.Select(c => c.Object));
        }

        [Test]
        public void should_return_HDTV720_from_extension_when_other_augments_are_null()
        {
            var nullMock = new Mock<IAugmentQuality>();
            nullMock.Setup(s => s.AugmentQuality(It.IsAny<LocalMovie>(), It.IsAny<DownloadClientItem>()))
                    .Returns<LocalMovie, DownloadClientItem>((l, d) => null);

            GivenAugmenters(_fileExtensionAugmenter, nullMock);

            var result = Subject.Aggregate(new LocalMovie(), null);

            result.Quality.SourceDetectionSource.Should().Be(QualityDetectionSource.Extension);
            result.Quality.ResolutionDetectionSource.Should().Be(QualityDetectionSource.Extension);
            result.Quality.Quality.Should().Be(Quality.HDTV720p);
        }

        [Test]
        public void should_return_SDTV_when_HDTV720_came_from_extension()
        {
            GivenAugmenters(_fileExtensionAugmenter, _nameAugmenter);

            var result = Subject.Aggregate(new LocalMovie(), null);

            result.Quality.SourceDetectionSource.Should().Be(QualityDetectionSource.Name);
            result.Quality.ResolutionDetectionSource.Should().Be(QualityDetectionSource.Name);
            result.Quality.Quality.Should().Be(Quality.SDTV);
        }

        [Test]
        public void should_return_HDTV1080p_when_HDTV720_came_from_extension_and_mediainfo_indicates_1080()
        {
            GivenAugmenters(_fileExtensionAugmenter, _mediaInfoAugmenter);

            var result = Subject.Aggregate(new LocalMovie(), null);

            result.Quality.SourceDetectionSource.Should().Be(QualityDetectionSource.Extension);
            result.Quality.ResolutionDetectionSource.Should().Be(QualityDetectionSource.MediaInfo);
            result.Quality.Quality.Should().Be(Quality.HDTV1080p);
        }

        [Test]
        public void should_return_HDTV1080p_when_SDTV_came_from_name_and_mediainfo_indicates_1080()
        {
            GivenAugmenters(_nameAugmenter, _mediaInfoAugmenter);

            var result = Subject.Aggregate(new LocalMovie(), null);

            result.Quality.SourceDetectionSource.Should().Be(QualityDetectionSource.Name);
            result.Quality.ResolutionDetectionSource.Should().Be(QualityDetectionSource.MediaInfo);
            result.Quality.Quality.Should().Be(Quality.HDTV1080p);
        }

        [Test]
        public void should_return_WEBDL480p_when_file_name_has_HDTV480p_but_release_name_indicates_webdl_source()
        {
            GivenAugmenters(_nameAugmenter, _releaseNameAugmenter);

            var result = Subject.Aggregate(new LocalMovie(), new DownloadClientItem());

            result.Quality.SourceDetectionSource.Should().Be(QualityDetectionSource.Name);
            result.Quality.ResolutionDetectionSource.Should().Be(QualityDetectionSource.Name);
            result.Quality.Quality.Should().Be(Quality.WEBDL480p);
        }

        [Test]
        public void should_return_version_1_when_no_version_specified()
        {
            GivenAugmenters(_nameAugmenter, _releaseNameAugmenter);

            var result = Subject.Aggregate(new LocalMovie(), new DownloadClientItem());

            result.Quality.Revision.Version.Should().Be(1);
            result.Quality.RevisionDetectionSource.Should().Be(QualityDetectionSource.Unknown);
        }

        [Test]
        public void should_return_version_2_when_name_indicates_proper()
        {
            _nameAugmenter.Setup(s => s.AugmentQuality(It.IsAny<LocalMovie>(), It.IsAny<DownloadClientItem>()))
                          .Returns(new AugmentQualityResult(QualitySource.Television, Confidence.Default, 480, Confidence.Default, new Revision(2), Confidence.Tag));

            GivenAugmenters(_nameAugmenter, _releaseNameAugmenter);

            var result = Subject.Aggregate(new LocalMovie(), new DownloadClientItem());

            result.Quality.Revision.Version.Should().Be(2);
            result.Quality.RevisionDetectionSource.Should().Be(QualityDetectionSource.Name);
        }

        [Test]
        public void should_return_version_0_when_file_name_indicates_v0()
        {
            _nameAugmenter.Setup(s => s.AugmentQuality(It.IsAny<LocalMovie>(), It.IsAny<DownloadClientItem>()))
                          .Returns(new AugmentQualityResult(QualitySource.Television, Confidence.Default, 480, Confidence.Default, new Revision(0), Confidence.Tag));

            GivenAugmenters(_nameAugmenter, _releaseNameAugmenter);

            var result = Subject.Aggregate(new LocalMovie(), new DownloadClientItem());

            result.Quality.Revision.Version.Should().Be(0);
            result.Quality.RevisionDetectionSource.Should().Be(QualityDetectionSource.Name);
        }

        [Test]
        public void should_return_version_2_when_file_name_indicates_v0_and_release_name_indicates_v2()
        {
            _nameAugmenter.Setup(s => s.AugmentQuality(It.IsAny<LocalMovie>(), It.IsAny<DownloadClientItem>()))
                          .Returns(new AugmentQualityResult(QualitySource.Television, Confidence.Default, 480, Confidence.Default, new Revision(0), Confidence.Tag));

            _releaseNameAugmenter.Setup(s => s.AugmentQuality(It.IsAny<LocalMovie>(), It.IsAny<DownloadClientItem>()))
                                 .Returns(new AugmentQualityResult(QualitySource.Television, Confidence.Default, 480, Confidence.Default, new Revision(2), Confidence.Tag));

            GivenAugmenters(_nameAugmenter, _releaseNameAugmenter);

            var result = Subject.Aggregate(new LocalMovie(), new DownloadClientItem());

            result.Quality.Revision.Version.Should().Be(2);
            result.Quality.RevisionDetectionSource.Should().Be(QualityDetectionSource.Name);
        }

        [TestCase(3840, 1920, 37)]
        [TestCase(5400, 2700, 38)]
        [TestCase(5760, 2880, 39)]
        [TestCase(7680, 3840, 40)]
        [TestCase(11520, 5760, 41)]
        public void should_combine_vr_release_source_with_media_info_dimensions(int width, int height, int expectedQualityId)
        {
            _releaseNameAugmenter.Setup(s => s.AugmentQuality(It.IsAny<LocalMovie>(), It.IsAny<DownloadClientItem>()))
                .Returns(new AugmentQualityResult(QualitySource.VR, Confidence.Tag, Quality.VR.Resolution, Confidence.Default, new Revision(), Confidence.Default));

            GivenAugmenters(_mediaInfoAugmenter, _releaseNameAugmenter);

            var localMovie = new LocalMovie
            {
                MediaInfo = new MediaInfoModel
                {
                    Width = width,
                    Height = height
                }
            };

            var result = Subject.Aggregate(localMovie, new DownloadClientItem());

            result.Quality.Quality.Should().Be(Quality.FindById(expectedQualityId));
            result.Quality.ResolutionDetectionSource.Should().Be(QualityDetectionSource.MediaInfo);
        }

        [Test]
        public void should_retain_explicit_vr_quality_when_media_info_dimensions_are_ambiguous()
        {
            _releaseNameAugmenter.Setup(s => s.AugmentQuality(It.IsAny<LocalMovie>(), It.IsAny<DownloadClientItem>()))
                .Returns(new AugmentQualityResult(QualitySource.VR, Confidence.Tag, Quality.VR8K.Resolution, Confidence.Tag, new Revision(), Confidence.Default));

            GivenAugmenters(_mediaInfoAugmenter, _releaseNameAugmenter);

            var localMovie = new LocalMovie
            {
                MediaInfo = new MediaInfoModel
                {
                    Width = 6800,
                    Height = 3400
                }
            };

            var result = Subject.Aggregate(localMovie, new DownloadClientItem());

            result.Quality.Quality.Should().Be(Quality.VR8K);
            result.Quality.ResolutionDetectionSource.Should().Be(QualityDetectionSource.Name);
        }

        [Test]
        public void should_correct_explicit_vr_quality_when_media_info_maps_to_another_vr_tier()
        {
            _releaseNameAugmenter.Setup(s => s.AugmentQuality(It.IsAny<LocalMovie>(), It.IsAny<DownloadClientItem>()))
                .Returns(new AugmentQualityResult(QualitySource.VR, Confidence.Tag, Quality.VR8K.Resolution, Confidence.Tag, new Revision(), Confidence.Default));

            GivenAugmenters(_mediaInfoAugmenter, _releaseNameAugmenter);

            var localMovie = new LocalMovie
            {
                MediaInfo = new MediaInfoModel
                {
                    Width = 3840,
                    Height = 1920
                }
            };

            Subject.Aggregate(localMovie, new DownloadClientItem())
                   .Quality.Quality.Should().Be(Quality.VR4K);
        }

        [Test]
        public void should_keep_non_vr_8k_quality_unchanged()
        {
            _mediaInfoAugmenter.Setup(s => s.AugmentQuality(It.IsAny<LocalMovie>(), It.IsAny<DownloadClientItem>()))
                .Returns(AugmentQualityResult.ResolutionOnly((int)Resolution.R4320p, Confidence.MediaInfo));

            GivenAugmenters(_mediaInfoAugmenter, _releaseNameAugmenter);

            var localMovie = new LocalMovie
            {
                MediaInfo = new MediaInfoModel
                {
                    Width = 7680,
                    Height = 4320
                }
            };

            Subject.Aggregate(localMovie, new DownloadClientItem())
                   .Quality.Quality.Should().Be(Quality.WEBDL4320p);
        }
    }
}
