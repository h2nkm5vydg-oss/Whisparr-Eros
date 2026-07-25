using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.Migration
{
    [TestFixture]
    public class add_vr_resolution_qualitiesFixture : MigrationTest<AddVrResolutionQualities>
    {
        [Test]
        public void should_copy_vr_limits_and_upgrade_stock_vr_profile()
        {
            var db = WithMigrationTestDb(migration =>
            {
                InsertGenericVrDefinition(migration, minSize: 8, maxSize: 200, preferredSize: 150);
                InsertProfile(
                    migration,
                    "VR",
                    32,
                    new List<QualityProfileItem242>
                    {
                        QualityItem(0, false),
                        QualityItem(32, true)
                    });
            });

            var definitions = db.Query<QualityDefinition242>(
                "SELECT \"Quality\", \"Title\", \"MinSize\", \"MaxSize\", \"PreferredSize\" FROM \"QualityDefinitions\" WHERE \"Quality\" >= 37 ORDER BY \"Quality\"");

            definitions.Select(definition => definition.Quality).Should().Equal(37, 38, 39, 40, 41);
            definitions.Should().OnlyContain(definition => definition.MinSize == 8 &&
                                                            definition.MaxSize == 200 &&
                                                            definition.PreferredSize == 150);

            var profile = GetProfile(db);
            var items = Json.Deserialize<List<QualityProfileItem242>>(profile.Items);

            profile.Cutoff.Should().Be(41);
            items.Select(item => item.Quality).Should().Equal(0, 32, 37, 38, 39, 40, 41);
            items.Where(item => item.Quality >= 32).Should().OnlyContain(item => item.Allowed);
        }

        [Test]
        public void should_preserve_custom_cutoff_and_disabled_vr_state()
        {
            var db = WithMigrationTestDb(migration =>
            {
                InsertGenericVrDefinition(migration);
                InsertProfile(
                    migration,
                    "Custom",
                    32,
                    new List<QualityProfileItem242>
                    {
                        QualityItem(0, true),
                        QualityItem(32, false)
                    });
            });

            var profile = GetProfile(db);
            var items = Json.Deserialize<List<QualityProfileItem242>>(profile.Items);

            profile.Cutoff.Should().Be(32);
            items.Where(item => item.Quality >= 37).Should().OnlyContain(item => !item.Allowed);
        }

        [Test]
        public void should_preserve_an_unset_generic_vr_minimum_size()
        {
            var db = WithMigrationTestDb(migration => InsertGenericVrDefinition(migration, minSize: null));

            var definitions = db.Query<QualityDefinition242>(
                "SELECT \"Quality\", \"Title\", \"MinSize\", \"MaxSize\", \"PreferredSize\" FROM \"QualityDefinitions\" WHERE \"Quality\" >= 37");

            definitions.Should().OnlyContain(definition => definition.MinSize == null);
        }

        [Test]
        public void should_insert_after_the_group_containing_generic_vr_and_use_effective_allowed_state()
        {
            var db = WithMigrationTestDb(migration =>
            {
                InsertGenericVrDefinition(migration);
                InsertProfile(
                    migration,
                    "Grouped",
                    0,
                    new List<QualityProfileItem242>
                    {
                        new QualityProfileItem242
                        {
                            Id = 1000,
                            Name = "Mixed",
                            Allowed = false,
                            Items = new List<QualityProfileItem242>
                            {
                                QualityItem(32, true)
                            }
                        },
                        QualityItem(0, true)
                    });
            });

            var items = Json.Deserialize<List<QualityProfileItem242>>(GetProfile(db).Items);

            items[0].Id.Should().Be(1000);
            items.Skip(1).Take(5).Select(item => item.Quality).Should().Equal(37, 38, 39, 40, 41);
            items.Skip(1).Take(5).Should().OnlyContain(item => !item.Allowed);
            items.Last().Quality.Should().Be(0);
        }

        [Test]
        public void should_append_disabled_variants_when_generic_vr_is_missing()
        {
            var db = WithMigrationTestDb(migration =>
            {
                InsertGenericVrDefinition(migration);
                InsertProfile(
                    migration,
                    "No VR",
                    0,
                    new List<QualityProfileItem242>
                    {
                        QualityItem(0, true)
                    });
            });

            var items = Json.Deserialize<List<QualityProfileItem242>>(GetProfile(db).Items);

            items.Select(item => item.Quality).Should().Equal(0, 37, 38, 39, 40, 41);
            items.Skip(1).Should().OnlyContain(item => !item.Allowed);
        }

        [Test]
        public void should_not_duplicate_an_existing_vr_variant()
        {
            var db = WithMigrationTestDb(migration =>
            {
                InsertGenericVrDefinition(migration);
                migration.Insert.IntoTable("QualityDefinitions").Row(new
                {
                    Quality = 37,
                    Title = "VR-4K",
                    MinSize = 4,
                    MaxSize = (double?)null,
                    PreferredSize = (double?)null
                });

                InsertProfile(
                    migration,
                    "Partial",
                    0,
                    new List<QualityProfileItem242>
                    {
                        QualityItem(32, true),
                        QualityItem(37, true)
                    });
            });

            var definitions = db.Query<QualityDefinition242>(
                "SELECT \"Quality\", \"Title\", \"MinSize\", \"MaxSize\", \"PreferredSize\" FROM \"QualityDefinitions\" WHERE \"Quality\" >= 37");
            var items = Json.Deserialize<List<QualityProfileItem242>>(GetProfile(db).Items);

            definitions.Select(definition => definition.Quality).Should().OnlyHaveUniqueItems();
            items.Select(item => item.Quality).Should().OnlyHaveUniqueItems();
            items.Select(item => item.Quality).Should().Equal(32, 37, 38, 39, 40, 41);
        }

        [Test]
        public void should_abort_when_a_new_quality_id_is_already_occupied()
        {
            Action migrate = () => WithMigrationTestDb(migration =>
            {
                InsertGenericVrDefinition(migration);
                migration.Insert.IntoTable("QualityDefinitions").Row(new
                {
                    Quality = 37,
                    Title = "Conflicting Quality",
                    MinSize = 0,
                    MaxSize = (double?)null,
                    PreferredSize = (double?)null
                });
            });

            migrate.Should().Throw<Exception>();
        }

        private static void InsertGenericVrDefinition(AddVrResolutionQualities migration, double? minSize = 4, double? maxSize = null, double? preferredSize = null)
        {
            migration.Insert.IntoTable("QualityDefinitions").Row(new
            {
                Quality = 32,
                Title = "VR",
                MinSize = minSize,
                MaxSize = maxSize,
                PreferredSize = preferredSize
            });
        }

        private static void InsertProfile(AddVrResolutionQualities migration, string name, int cutoff, List<QualityProfileItem242> items)
        {
            migration.Insert.IntoTable("QualityProfiles").Row(new
            {
                Name = name,
                Cutoff = cutoff,
                Items = items.ToJson()
            });
        }

        private static QualityProfileItem242 QualityItem(int quality, bool allowed)
        {
            return new QualityProfileItem242
            {
                Quality = quality,
                Allowed = allowed
            };
        }

        private static QualityProfileResult242 GetProfile(IDirectDataMapper db)
        {
            return db.Query<QualityProfileResult242>("SELECT \"Name\", \"Cutoff\", \"Items\" FROM \"QualityProfiles\"").Single();
        }
    }

    public class QualityProfileResult242
    {
        public string Name { get; set; }
        public int Cutoff { get; set; }
        public string Items { get; set; }
    }
}
