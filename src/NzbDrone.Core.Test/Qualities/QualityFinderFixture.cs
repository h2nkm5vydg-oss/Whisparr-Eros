using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Test.Qualities
{
    [TestFixture]
    public class QualityFinderFixture
    {
        [TestCase(QualitySource.Unknown, 480)]
        public void should_return_WebDL_480(QualitySource source, int resolution)
        {
            QualityFinder.FindBySourceAndResolution(source, resolution).Should().Be(Quality.WEBDL480p);
        }

        [TestCase(QualitySource.DVDRaw, 480)]
        public void should_return_DVD_Remux(QualitySource source, int resolution)
        {
            QualityFinder.FindBySourceAndResolution(source, resolution).Should().Be(Quality.DVDR);
        }

        [TestCase(QualitySource.DVD, 480)]
        [TestCase(QualitySource.DVD, 576)]
        public void should_return_DVD(QualitySource source, int resolution)
        {
            QualityFinder.FindBySourceAndResolution(source, resolution).Should().Be(Quality.DVD);
        }

        [TestCase(QualitySource.Television, 480)]
        public void should_return_SDTV(QualitySource source, int resolution)
        {
            QualityFinder.FindBySourceAndResolution(source, resolution).Should().Be(Quality.SDTV);
        }

        [TestCase(QualitySource.Unknown, 720)]
        public void should_return_WebDL_720p(QualitySource source, int resolution)
        {
            QualityFinder.FindBySourceAndResolution(source, resolution).Should().Be(Quality.WEBDL720p);
        }

        [TestCase(QualitySource.Television, 720)]
        public void should_return_HDTV_720p(QualitySource source, int resolution)
        {
            QualityFinder.FindBySourceAndResolution(source, resolution).Should().Be(Quality.HDTV720p);
        }

        [TestCase(QualitySource.Unknown, 1080)]
        public void should_return_WebDL_1080p(QualitySource source, int resolution)
        {
            QualityFinder.FindBySourceAndResolution(source, resolution).Should().Be(Quality.WEBDL1080p);
        }

        [TestCase(QualitySource.Television, 1080)]
        public void should_return_HDTV_1080p(QualitySource source, int resolution)
        {
            QualityFinder.FindBySourceAndResolution(source, resolution).Should().Be(Quality.HDTV1080p);
        }

        [TestCase(QualitySource.Bluray, 720)]
        public void should_return_Bluray720p(QualitySource source, int resolution)
        {
            QualityFinder.FindBySourceAndResolution(source, resolution).Should().Be(Quality.Bluray720p);
        }

        [TestCase(0, 32)]
        [TestCase(1920, 37)]
        [TestCase(2700, 38)]
        [TestCase(2880, 39)]
        [TestCase(3840, 40)]
        [TestCase(5760, 41)]
        public void should_return_exact_vr_quality(int resolution, int expectedQualityId)
        {
            QualityFinder.FindBySourceAndResolution(QualitySource.VR, resolution)
                         .Should().Be(Quality.FindById(expectedQualityId));
        }

        [Test]
        public void should_return_generic_vr_for_an_unknown_vr_resolution()
        {
            QualityFinder.FindBySourceAndResolution(QualitySource.VR, 5000).Should().Be(Quality.VR);
        }

        [TestCase(QualitySource.Unknown)]
        [TestCase(QualitySource.Web)]
        [TestCase(QualitySource.Bluray)]
        public void should_not_infer_vr_from_a_vr_only_resolution(QualitySource source)
        {
            QualityFinder.FindBySourceAndResolution(source, 3840).Should().Be(Quality.Unknown);
        }

        [Test]
        public void should_prefer_non_vr_quality_when_resolution_is_shared()
        {
            QualityFinder.FindBySourceAndResolution(QualitySource.Unknown, 2880).Should().Be(Quality.WEBDL2880p);
        }
    }
}
