using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using FluentMigrator;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(242)]
    public class AddVrResolutionQualities : NzbDroneMigrationBase
    {
        private const int GenericVrQualityId = 32;

        private static readonly VrQualityDefinition242[] VrQualityDefinitions =
        {
            new VrQualityDefinition242(37, "VR-4K"),
            new VrQualityDefinition242(38, "VR-5K"),
            new VrQualityDefinition242(39, "VR-6K"),
            new VrQualityDefinition242(40, "VR-8K"),
            new VrQualityDefinition242(41, "VR-12K")
        };

        protected override void MainDbUpgrade()
        {
            Execute.WithConnection(MigrateVrQualities);
        }

        private void MigrateVrQualities(IDbConnection connection, IDbTransaction transaction)
        {
            InsertQualityDefinitions(connection, transaction);
            UpdateQualityProfiles(connection, transaction);
        }

        private static void InsertQualityDefinitions(IDbConnection connection, IDbTransaction transaction)
        {
            var existingDefinitions = connection.Query<QualityDefinition242>(
                @"SELECT ""Quality"", ""Title"", ""MinSize"", ""MaxSize"", ""PreferredSize"" FROM ""QualityDefinitions""",
                transaction: transaction).ToList();

            foreach (var definition in VrQualityDefinitions)
            {
                var qualityCollision = existingDefinitions.SingleOrDefault(existing => existing.Quality == definition.Quality);
                var titleCollision = existingDefinitions.SingleOrDefault(existing => string.Equals(existing.Title, definition.Title, StringComparison.OrdinalIgnoreCase));

                if (qualityCollision != null && !string.Equals(qualityCollision.Title, definition.Title, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Quality ID {definition.Quality} is already assigned to '{qualityCollision.Title}'");
                }

                if (titleCollision != null && titleCollision.Quality != definition.Quality)
                {
                    throw new InvalidOperationException($"Quality title '{definition.Title}' is already assigned to ID {titleCollision.Quality}");
                }
            }

            var genericVrDefinition = existingDefinitions.SingleOrDefault(existing => existing.Quality == GenericVrQualityId);
            var minSize = genericVrDefinition == null ? 4 : genericVrDefinition.MinSize;
            var maxSize = genericVrDefinition?.MaxSize;
            var preferredSize = genericVrDefinition?.PreferredSize;

            foreach (var definition in VrQualityDefinitions.Where(definition => existingDefinitions.All(existing => existing.Quality != definition.Quality)))
            {
                connection.Execute(
                    @"INSERT INTO ""QualityDefinitions"" (""Quality"", ""Title"", ""MinSize"", ""MaxSize"", ""PreferredSize"")
                      VALUES (@Quality, @Title, @MinSize, @MaxSize, @PreferredSize)",
                    new
                    {
                        definition.Quality,
                        definition.Title,
                        MinSize = minSize,
                        MaxSize = maxSize,
                        PreferredSize = preferredSize
                    },
                    transaction);
            }
        }

        private static void UpdateQualityProfiles(IDbConnection connection, IDbTransaction transaction)
        {
            var profiles = connection.Query<QualityProfile242>(
                @"SELECT ""Id"", ""Name"", ""Cutoff"", ""Items"" AS ""ItemsJson"" FROM ""QualityProfiles""",
                transaction: transaction).ToList();

            foreach (var profile in profiles)
            {
                profile.Items = Json.Deserialize<List<QualityProfileItem242>>(profile.ItemsJson) ?? new List<QualityProfileItem242>();

                var isStockVrProfile = IsStockVrProfile(profile);
                var changed = InsertVrQualities(profile);

                if (isStockVrProfile)
                {
                    profile.Cutoff = VrQualityDefinitions.Last().Quality;
                    changed = true;
                }

                if (!changed)
                {
                    continue;
                }

                connection.Execute(
                    @"UPDATE ""QualityProfiles"" SET ""Cutoff"" = @Cutoff, ""Items"" = @ItemsJson WHERE ""Id"" = @Id",
                    new
                    {
                        profile.Id,
                        profile.Cutoff,
                        ItemsJson = profile.Items.ToJson()
                    },
                    transaction);
            }
        }

        private static bool InsertVrQualities(QualityProfile242 profile)
        {
            var existingQualityIds = profile.Items
                .SelectMany(GetLeafItems)
                .Where(item => item.Quality.HasValue)
                .Select(item => item.Quality.Value)
                .ToHashSet();

            var missingQualities = VrQualityDefinitions
                .Where(definition => !existingQualityIds.Contains(definition.Quality))
                .ToList();

            if (!missingQualities.Any())
            {
                return false;
            }

            var vrQualityIds = VrQualityDefinitions.Select(definition => definition.Quality).ToHashSet();
            var existingTopLevelVariants = profile.Items
                .Where(item => item.Quality.HasValue && vrQualityIds.Contains(item.Quality.Value))
                .ToDictionary(item => item.Quality.Value);

            profile.Items.RemoveAll(item => item.Quality.HasValue && vrQualityIds.Contains(item.Quality.Value));

            var genericVrRootIndex = profile.Items.FindIndex(item => ContainsQuality(item, GenericVrQualityId));
            var genericVrAllowed = genericVrRootIndex >= 0 &&
                                   TryGetEffectiveAllowed(profile.Items[genericVrRootIndex], GenericVrQualityId, true, out var effectiveAllowed) &&
                                   effectiveAllowed;
            var insertIndex = genericVrRootIndex >= 0 ? genericVrRootIndex + 1 : profile.Items.Count;

            foreach (var definition in VrQualityDefinitions)
            {
                if (existingTopLevelVariants.TryGetValue(definition.Quality, out var existingItem))
                {
                    profile.Items.Insert(insertIndex++, existingItem);
                }
                else if (!existingQualityIds.Contains(definition.Quality))
                {
                    profile.Items.Insert(insertIndex++, new QualityProfileItem242
                    {
                        Quality = definition.Quality,
                        Allowed = genericVrAllowed
                    });
                }
            }

            return true;
        }

        private static bool IsStockVrProfile(QualityProfile242 profile)
        {
            if (profile.Name != "VR" ||
                profile.Cutoff != GenericVrQualityId ||
                profile.Items.Count(item => item.Quality == GenericVrQualityId && item.Allowed) != 1)
            {
                return false;
            }

            var allowedQualities = profile.Items
                .SelectMany(GetLeafItems)
                .Where(item => item.Allowed && item.Quality.HasValue)
                .Select(item => item.Quality.Value)
                .ToList();

            return allowedQualities.Count == 1 && allowedQualities[0] == GenericVrQualityId;
        }

        private static bool ContainsQuality(QualityProfileItem242 item, int qualityId)
        {
            return item.Quality == qualityId ||
                   item.Items?.Any(child => ContainsQuality(child, qualityId)) == true;
        }

        private static bool TryGetEffectiveAllowed(QualityProfileItem242 item, int qualityId, bool parentAllowed, out bool allowed)
        {
            var effectiveAllowed = parentAllowed && item.Allowed;

            if (item.Quality == qualityId)
            {
                allowed = effectiveAllowed;
                return true;
            }

            if (item.Items != null)
            {
                foreach (var child in item.Items)
                {
                    if (TryGetEffectiveAllowed(child, qualityId, effectiveAllowed, out allowed))
                    {
                        return true;
                    }
                }
            }

            allowed = false;
            return false;
        }

        private static IEnumerable<QualityProfileItem242> GetLeafItems(QualityProfileItem242 item)
        {
            if (item.Items == null || !item.Items.Any())
            {
                yield return item;
                yield break;
            }

            foreach (var child in item.Items.SelectMany(GetLeafItems))
            {
                yield return child;
            }
        }
    }

    public class VrQualityDefinition242
    {
        public VrQualityDefinition242()
        {
        }

        public VrQualityDefinition242(int quality, string title)
        {
            Quality = quality;
            Title = title;
        }

        public int Quality { get; set; }
        public string Title { get; set; }
    }

    public class QualityDefinition242
    {
        public int Quality { get; set; }
        public string Title { get; set; }
        public double? MinSize { get; set; }
        public double? MaxSize { get; set; }
        public double? PreferredSize { get; set; }
    }

    public class QualityProfile242
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Cutoff { get; set; }
        public string ItemsJson { get; set; }
        public List<QualityProfileItem242> Items { get; set; }
    }

    public class QualityProfileItem242
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public int? Quality { get; set; }
        public List<QualityProfileItem242> Items { get; set; }
        public bool Allowed { get; set; }
    }
}
