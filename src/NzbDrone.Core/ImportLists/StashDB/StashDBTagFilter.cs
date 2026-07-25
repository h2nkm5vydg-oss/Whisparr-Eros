using System;
using System.Collections.Generic;
using System.Linq;

namespace NzbDrone.Core.ImportLists.StashDB
{
    public interface IStashDBTagFilterSettings
    {
        string IncludedTags { get; }
        string ExcludedTags { get; }
    }

    public static class StashDBTagFilter
    {
        public static IReadOnlyCollection<string> ParseIds(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Array.Empty<string>();
            }

            return value
                .Split(",")
                .Select(id => id.Trim())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static bool HasCombinedFilters(IStashDBTagFilterSettings settings)
        {
            return ParseIds(settings.IncludedTags).Count > 0 &&
                   ParseIds(settings.ExcludedTags).Count > 0;
        }

        public static FilterType GetServerFilter(IStashDBTagFilterSettings settings)
        {
            var includedTags = ParseIds(settings.IncludedTags);
            if (includedTags.Count > 0)
            {
                return new FilterType(FilterModifier.INCLUDES, includedTags.ToList());
            }

            var excludedTags = ParseIds(settings.ExcludedTags);
            if (excludedTags.Count > 0)
            {
                return new FilterType(FilterModifier.EXCLUDES, excludedTags.ToList());
            }

            return null;
        }

        public static int GetPageCount(int resultCount, int pageSize, int maxResults)
        {
            if (resultCount <= 0 || pageSize <= 0 || maxResults <= 0)
            {
                return 0;
            }

            var availablePages = (int)Math.Ceiling((double)resultCount / pageSize);
            var allowedPages = (int)Math.Ceiling((double)maxResults / pageSize);

            return Math.Min(availablePages, allowedPages);
        }
    }
}
