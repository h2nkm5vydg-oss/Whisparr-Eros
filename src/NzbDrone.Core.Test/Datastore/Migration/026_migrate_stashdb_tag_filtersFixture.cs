using System.Linq;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NzbDrone.Core.Datastore.Migration;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.Migration
{
    [TestFixture]
    public class migrate_stashdb_tag_filtersFixture : MigrationTest<migrate_stashdb_tag_filters>
    {
        [Test]
        public void should_migrate_existing_tag_filter_settings()
        {
            var db = WithMigrationTestDb(migration =>
            {
                InsertImportList(migration, "Includes", "StashDBTagsImport", """{"tags":"include-id","tagsFilter":0}""");
                InsertImportList(migration, "Excludes", "StashDBFavoriteImport", """{"tags":"exclude-id","tagsFilter":"EXCLUDES"}""");
                InsertImportList(migration, "Other", "TMDbListImport", """{"tags":"unchanged","tagsFilter":0}""");
            });

            var rows = db.Query<ImportListMigrationRow>("SELECT \"Name\", \"Settings\" FROM \"ImportLists\"")
                .ToDictionary(row => row.Name);

            var includedSettings = JObject.Parse(rows["Includes"].Settings);
            includedSettings.Value<string>("includedTags").Should().Be("include-id");
            includedSettings.ContainsKey("excludedTags").Should().BeFalse();
            includedSettings.ContainsKey("tags").Should().BeFalse();
            includedSettings.ContainsKey("tagsFilter").Should().BeFalse();

            var excludedSettings = JObject.Parse(rows["Excludes"].Settings);
            excludedSettings.Value<string>("excludedTags").Should().Be("exclude-id");
            excludedSettings.ContainsKey("includedTags").Should().BeFalse();
            excludedSettings.ContainsKey("tags").Should().BeFalse();
            excludedSettings.ContainsKey("tagsFilter").Should().BeFalse();

            var otherSettings = JObject.Parse(rows["Other"].Settings);
            otherSettings.Value<string>("tags").Should().Be("unchanged");
            otherSettings.Value<int>("tagsFilter").Should().Be(0);
        }

        private static void InsertImportList(
            migrate_stashdb_tag_filters migration,
            string name,
            string implementation,
            string settings)
        {
            migration.Insert.IntoTable("ImportLists").Row(new
            {
                Enabled = true,
                Name = name,
                Implementation = implementation,
                ConfigContract = "TestSettings",
                Settings = settings,
                EnableAuto = false,
                RootFolderPath = "/movies",
                QualityProfileId = 1,
                Tags = "[]",
                SearchOnAdd = false,
                Monitor = 0
            });
        }
    }

    public class ImportListMigrationRow
    {
        public string Name { get; set; }
        public string Settings { get; set; }
    }
}
