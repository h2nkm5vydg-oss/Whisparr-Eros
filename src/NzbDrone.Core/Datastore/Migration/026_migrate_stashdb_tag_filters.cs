using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.ImportLists.StashDB;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(026)]
    public class migrate_stashdb_tag_filters : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.WithConnection((conn, tran) =>
            {
                var importLists = new List<(int Id, JsonObject Settings)>();
                using (var selectCommand = conn.CreateCommand())
                {
                    selectCommand.Transaction = tran;
                    selectCommand.CommandText = "SELECT \"Id\", \"Settings\" FROM \"ImportLists\" WHERE \"Implementation\" LIKE 'StashDB%'";

                    using (var reader = selectCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader.IsDBNull(1))
                            {
                                continue;
                            }

                            var settings = JsonNode.Parse(reader.GetString(1)) as JsonObject;
                            if (settings != null)
                            {
                                importLists.Add((reader.GetInt32(0), settings));
                            }
                        }
                    }
                }

                foreach (var (id, settings) in importLists)
                {
                    var hasTags = settings.TryGetPropertyValue("tags", out var tagsNode);
                    if (!hasTags && !settings.ContainsKey("tagsFilter"))
                    {
                        continue;
                    }

                    var targetField = IsExcluded(settings["tagsFilter"]) ? "excludedTags" : "includedTags";
                    if (hasTags && !settings.ContainsKey(targetField))
                    {
                        settings[targetField] = tagsNode?.DeepClone();
                    }

                    settings.Remove("tags");
                    settings.Remove("tagsFilter");

                    using (var updateCommand = conn.CreateCommand())
                    {
                        updateCommand.Transaction = tran;
                        updateCommand.CommandText = "UPDATE \"ImportLists\" SET \"Settings\" = @settings WHERE \"Id\" = @id";

                        var settingsParameter = updateCommand.CreateParameter();
                        settingsParameter.ParameterName = "@settings";
                        settingsParameter.Value = settings.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                        updateCommand.Parameters.Add(settingsParameter);

                        var idParameter = updateCommand.CreateParameter();
                        idParameter.ParameterName = "@id";
                        idParameter.Value = id;
                        updateCommand.Parameters.Add(idParameter);

                        updateCommand.ExecuteNonQuery();
                    }
                }
            });
        }

        private static bool IsExcluded(JsonNode filterNode)
        {
            if (filterNode is not JsonValue value)
            {
                return false;
            }

            if (value.TryGetValue<int>(out var intValue))
            {
                return intValue == (int)FilterModifier.EXCLUDES;
            }

            return value.TryGetValue<string>(out var stringValue) &&
                   Enum.TryParse<FilterModifier>(stringValue, true, out var filter) &&
                   filter == FilterModifier.EXCLUDES;
        }
    }
}
