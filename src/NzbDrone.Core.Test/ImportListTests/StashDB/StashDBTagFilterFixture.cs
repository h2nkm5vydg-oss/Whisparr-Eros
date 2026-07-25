using System;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ImportLists.StashDB;
using NzbDrone.Core.ImportLists.StashDB.Favorite;
using NzbDrone.Core.ImportLists.StashDB.Performer;
using NzbDrone.Core.ImportLists.StashDB.Studio;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ImportListTests.StashDB
{
    [TestFixture]
    public class StashDBTagFilterFixture : CoreTest
    {
        [Test]
        public void should_normalize_tag_ids()
        {
            var result = StashDBTagFilter.ParseIds(" first, SECOND,first, ,second ");

            result.Should().Equal("first", "SECOND");
        }

        [Test]
        public void should_prefer_included_tags_for_combined_server_filter()
        {
            var settings = new StashDBTagsSettings
            {
                IncludedTags = "included",
                ExcludedTags = "excluded"
            };

            var result = StashDBTagFilter.GetServerFilter(settings);

            result.Modifier.Should().Be(FilterModifier.INCLUDES);
            result.Value.Should().Equal("included");
            StashDBTagFilter.HasCombinedFilters(settings).Should().BeTrue();
        }

        [Test]
        public void should_use_excluded_tags_when_no_included_tags_are_set()
        {
            var settings = new StashDBTagsSettings
            {
                ExcludedTags = "excluded"
            };

            var result = StashDBTagFilter.GetServerFilter(settings);

            result.Modifier.Should().Be(FilterModifier.EXCLUDES);
            result.Value.Should().Equal("excluded");
            StashDBTagFilter.HasCombinedFilters(settings).Should().BeFalse();
        }

        [TestCase(0, 100, 100, 0)]
        [TestCase(50, 100, 50, 1)]
        [TestCase(100, 100, 100, 1)]
        [TestCase(101, 100, 150, 2)]
        [TestCase(5000, 100, 1000, 10)]
        public void should_calculate_page_count_using_ceilings(int resultCount, int pageSize, int maxResults, int expected)
        {
            StashDBTagFilter.GetPageCount(resultCount, pageSize, maxResults).Should().Be(expected);
        }

        [TestCase(typeof(StashDBFavoriteSettings))]
        [TestCase(typeof(StashDBPerformerSettings))]
        [TestCase(typeof(StashDBStudioSettings))]
        [TestCase(typeof(StashDBTagsSettings))]
        public void should_expose_separate_provider_fields(Type settingsType)
        {
            var includedProperty = settingsType.GetProperty(nameof(IStashDBTagFilterSettings.IncludedTags));
            var excludedProperty = settingsType.GetProperty(nameof(IStashDBTagFilterSettings.ExcludedTags));

            includedProperty.Should().NotBeNull();
            excludedProperty.Should().NotBeNull();
            includedProperty.GetCustomAttribute<FieldDefinitionAttribute>().Should().NotBeNull();
            excludedProperty.GetCustomAttribute<FieldDefinitionAttribute>().Should().NotBeNull();
            settingsType.GetProperty("Tags").Should().BeNull();
            settingsType.GetProperty("TagsFilter").Should().BeNull();
        }

        [TestCase("favorite", FilterModifier.INCLUDES, false)]
        [TestCase("performer", FilterModifier.INCLUDES, false)]
        [TestCase("studio", FilterModifier.INCLUDES, false)]
        [TestCase("tags", FilterModifier.INCLUDES, false)]
        [TestCase("favorite", FilterModifier.EXCLUDES, false)]
        [TestCase("performer", FilterModifier.EXCLUDES, false)]
        [TestCase("studio", FilterModifier.EXCLUDES, false)]
        [TestCase("tags", FilterModifier.EXCLUDES, false)]
        [TestCase("favorite", FilterModifier.INCLUDES, true)]
        [TestCase("performer", FilterModifier.INCLUDES, true)]
        [TestCase("studio", FilterModifier.INCLUDES, true)]
        [TestCase("tags", FilterModifier.INCLUDES, true)]
        public void should_serialize_tag_filter_for_every_stashdb_list(string listType, FilterModifier modifier, bool includeSceneTags)
        {
            var filter = new FilterType(modifier, new List<string> { "tag-id" });
            var query = CreateQuery(listType, filter, includeSceneTags);
            var variables = JObject.Parse(query.Variables);

            variables["input"]["tags"]["modifier"].Value<string>().Should().Be(modifier.ToString());
            variables["input"]["tags"]["value"].Values<string>().Should().Equal("tag-id");
            query.Query.Contains("tags {").Should().Be(includeSceneTags);
        }

        [TestCase("favorite")]
        [TestCase("performer")]
        [TestCase("studio")]
        [TestCase("tags")]
        public void should_omit_tag_criterion_when_no_tags_are_configured(string listType)
        {
            var query = CreateQuery(listType, null, false);
            var variables = JObject.Parse(query.Variables);

            variables["input"]["tags"].Should().BeNull();
            query.Query.Should().NotContain("tags {");
        }

        private static StashDBQuery CreateQuery(string listType, FilterType filter, bool includeSceneTags)
        {
            return listType switch
            {
                "favorite" => FromFavorite(filter, includeSceneTags),
                "performer" => FromPerformer(filter, includeSceneTags),
                "studio" => FromStudio(filter, includeSceneTags),
                "tags" => FromTags(filter, includeSceneTags),
                _ => throw new ArgumentOutOfRangeException(nameof(listType), listType, null)
            };
        }

        private static StashDBQuery FromFavorite(FilterType filter, bool includeSceneTags)
        {
            var query = new QueryFavoriteSceneQuery(1, 100, FavoriteFilter.PERFORMER, filter, includeSceneTags, SceneSort.CREATED, null);
            return new StashDBQuery(query.Query, query.Variables);
        }

        private static StashDBQuery FromPerformer(FilterType filter, bool includeSceneTags)
        {
            var query = new QueryPerformerSceneQuery(
                1,
                100,
                new List<string> { "performer" },
                new List<string>(),
                FilterModifier.INCLUDES,
                filter,
                includeSceneTags,
                false,
                SceneSort.CREATED,
                null);

            return new StashDBQuery(query.Query, query.Variables);
        }

        private static StashDBQuery FromStudio(FilterType filter, bool includeSceneTags)
        {
            var query = new QueryStudioSceneQuery(
                1,
                100,
                new List<string> { "studio" },
                filter,
                includeSceneTags,
                false,
                SceneSort.CREATED,
                null);

            return new StashDBQuery(query.Query, query.Variables);
        }

        private static StashDBQuery FromTags(FilterType filter, bool includeSceneTags)
        {
            var query = new QueryTagsSceneQuery(1, 100, filter, includeSceneTags, SceneSort.CREATED, null);
            return new StashDBQuery(query.Query, query.Variables);
        }

        private sealed class StashDBQuery
        {
            public StashDBQuery(string query, string variables)
            {
                Query = query;
                Variables = variables;
            }

            public string Query { get; }
            public string Variables { get; }
        }
    }
}
