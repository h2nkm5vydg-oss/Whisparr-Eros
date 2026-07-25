using System;
using System.Collections.Generic;
using System.Linq;

namespace NzbDrone.Core.Qualities
{
    public static class VrResolutionMapper
    {
        private const double MaximumAnchorDifference = 0.10;

        private static readonly Dictionary<int, int> MarketingResolutionMap = new Dictionary<int, int>
        {
            { 4, Quality.VR4K.Resolution },
            { 5, Quality.VR5K.Resolution },
            { 6, Quality.VR6K.Resolution },
            { 8, Quality.VR8K.Resolution },
            { 12, Quality.VR12K.Resolution }
        };

        private static readonly Dictionary<int, int> VerticalResolutionMap = new Dictionary<int, int>
        {
            { 1920, Quality.VR4K.Resolution },
            { 2048, Quality.VR4K.Resolution },
            { 2160, Quality.VR4K.Resolution },
            { 2560, Quality.VR5K.Resolution },
            { 2700, Quality.VR5K.Resolution },
            { 2880, Quality.VR6K.Resolution },
            { 2900, Quality.VR6K.Resolution },
            { 3000, Quality.VR6K.Resolution },
            { 3072, Quality.VR6K.Resolution },
            { 3160, Quality.VR6K.Resolution },
            { 3384, Quality.VR6K.Resolution },
            { 3840, Quality.VR8K.Resolution },
            { 4096, Quality.VR8K.Resolution },
            { 4320, Quality.VR8K.Resolution },
            { 5760, Quality.VR12K.Resolution },
            { 6144, Quality.VR12K.Resolution }
        };

        private static readonly Dictionary<DimensionPair, int> DimensionResolutionMap = new Dictionary<DimensionPair, int>
        {
            { DimensionPair.Create(3840, 1920), Quality.VR4K.Resolution },
            { DimensionPair.Create(4096, 2048), Quality.VR4K.Resolution },
            { DimensionPair.Create(3840, 2160), Quality.VR4K.Resolution },
            { DimensionPair.Create(5120, 2560), Quality.VR5K.Resolution },
            { DimensionPair.Create(5400, 2700), Quality.VR5K.Resolution },
            { DimensionPair.Create(5400, 5400), Quality.VR6K.Resolution },
            { DimensionPair.Create(5760, 2880), Quality.VR6K.Resolution },
            { DimensionPair.Create(5800, 2900), Quality.VR6K.Resolution },
            { DimensionPair.Create(6016, 3384), Quality.VR6K.Resolution },
            { DimensionPair.Create(6144, 3072), Quality.VR6K.Resolution },
            { DimensionPair.Create(6144, 3160), Quality.VR6K.Resolution },
            { DimensionPair.Create(7680, 3840), Quality.VR8K.Resolution },
            { DimensionPair.Create(8192, 4096), Quality.VR8K.Resolution },
            { DimensionPair.Create(7680, 4320), Quality.VR8K.Resolution },
            { DimensionPair.Create(8192, 4320), Quality.VR8K.Resolution },
            { DimensionPair.Create(11520, 5760), Quality.VR12K.Resolution },
            { DimensionPair.Create(12288, 6144), Quality.VR12K.Resolution }
        };

        private static readonly Dictionary<int, int> LongEdgeAnchors = new Dictionary<int, int>
        {
            { 3840, Quality.VR4K.Resolution },
            { 5400, Quality.VR5K.Resolution },
            { 5760, Quality.VR6K.Resolution },
            { 7680, Quality.VR8K.Resolution },
            { 11520, Quality.VR12K.Resolution }
        };

        public static int? FromMarketingResolution(int marketingResolution)
        {
            return MarketingResolutionMap.TryGetValue(marketingResolution, out var resolution) ? resolution : null;
        }

        public static int? FromVerticalResolution(int verticalResolution)
        {
            return VerticalResolutionMap.TryGetValue(verticalResolution, out var resolution) ? resolution : null;
        }

        public static int? FromDimensions(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                return null;
            }

            var dimensions = DimensionPair.Create(width, height);

            if (DimensionResolutionMap.TryGetValue(dimensions, out var exactResolution))
            {
                return exactResolution;
            }

            var nearestAnchor = LongEdgeAnchors
                .Select(anchor => new
                {
                    anchor.Value,
                    Difference = Math.Abs(dimensions.LongEdge - anchor.Key) / (double)anchor.Key
                })
                .OrderBy(anchor => anchor.Difference)
                .First();

            return nearestAnchor.Difference <= MaximumAnchorDifference ? nearestAnchor.Value : null;
        }

        private readonly struct DimensionPair : IEquatable<DimensionPair>
        {
            private DimensionPair(int longEdge, int shortEdge)
            {
                LongEdge = longEdge;
                ShortEdge = shortEdge;
            }

            public int LongEdge { get; }
            public int ShortEdge { get; }

            public static DimensionPair Create(int width, int height)
            {
                return new DimensionPair(Math.Max(width, height), Math.Min(width, height));
            }

            public bool Equals(DimensionPair other)
            {
                return LongEdge == other.LongEdge && ShortEdge == other.ShortEdge;
            }

            public override bool Equals(object obj)
            {
                return obj is DimensionPair other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(LongEdge, ShortEdge);
            }
        }
    }
}
