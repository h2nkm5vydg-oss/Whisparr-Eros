using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Test.Qualities
{
    [TestFixture]
    public class VrResolutionMapperFixture
    {
        [TestCase(4, 1920)]
        [TestCase(5, 2700)]
        [TestCase(6, 2880)]
        [TestCase(8, 3840)]
        [TestCase(12, 5760)]
        public void should_map_marketing_resolutions(int marketingResolution, int expectedResolution)
        {
            VrResolutionMapper.FromMarketingResolution(marketingResolution).Should().Be(expectedResolution);
        }

        [TestCase(3840, 1920, 1920)]
        [TestCase(4096, 2048, 1920)]
        [TestCase(3840, 2160, 1920)]
        [TestCase(5120, 2560, 2700)]
        [TestCase(5400, 2700, 2700)]
        [TestCase(5400, 5400, 2880)]
        [TestCase(5760, 2880, 2880)]
        [TestCase(5800, 2900, 2880)]
        [TestCase(6016, 3384, 2880)]
        [TestCase(6144, 3072, 2880)]
        [TestCase(6144, 3160, 2880)]
        [TestCase(7680, 3840, 3840)]
        [TestCase(8192, 4096, 3840)]
        [TestCase(7680, 4320, 3840)]
        [TestCase(8192, 4320, 3840)]
        [TestCase(11520, 5760, 5760)]
        [TestCase(12288, 6144, 5760)]
        public void should_map_known_vr_dimensions(int width, int height, int expectedResolution)
        {
            VrResolutionMapper.FromDimensions(width, height).Should().Be(expectedResolution);
            VrResolutionMapper.FromDimensions(height, width).Should().Be(expectedResolution);
        }

        [TestCase(4000, 2000, 1920)]
        [TestCase(5200, 2600, 2700)]
        [TestCase(5900, 2950, 2880)]
        [TestCase(8000, 4000, 3840)]
        [TestCase(12000, 6000, 5760)]
        public void should_map_dimensions_near_a_known_long_edge(int width, int height, int expectedResolution)
        {
            VrResolutionMapper.FromDimensions(width, height).Should().Be(expectedResolution);
        }

        [TestCase(0, 0)]
        [TestCase(1920, 1080)]
        [TestCase(6800, 3400)]
        [TestCase(10240, 5120)]
        [TestCase(16000, 8000)]
        public void should_not_map_dimensions_outside_known_families(int width, int height)
        {
            VrResolutionMapper.FromDimensions(width, height).Should().BeNull();
        }
    }
}
