using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.ImportLists.ImportListMovies;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.ImportLists.StashDB
{
    public abstract class StashDBImportBase<TSettings> : HttpImportListBase<TSettings>
    where TSettings : StashDBSettingsBase<TSettings>, IStashDBTagFilterSettings, new()
    {
        public StashDBImportBase(IHttpClient httpClient,
                                    IImportListStatusService importListStatusService,
                                    IConfigService configService,
                                    IParsingService parsingService,
                                    Logger logger)
            : base(httpClient, importListStatusService, configService, parsingService, logger)
        {
        }

        public override int PageSize => 100;
        public override bool Enabled => true;
        public override bool EnableAuto => false;
        public override ImportListType ListType => ImportListType.StashDB;
        public override TimeSpan MinRefreshInterval => TimeSpan.FromHours(1);
        protected override bool UsePreGeneratedPages => StashDBTagFilter.HasCombinedFilters(Settings);
        protected override int MaxResultsPerQuery => Math.Min(Settings.Limit, MaxNumResultsPerQuery);
        protected int MaxCandidateResults => UsePreGeneratedPages ? MaxNumResultsPerQuery : MaxResultsPerQuery;

        public override IParseImportListResponse GetParser()
        {
            return new StashDBParser(StashDBTagFilter.ParseIds(Settings.ExcludedTags));
        }

        protected override List<ImportListMovie> CleanupListItems(IEnumerable<ImportListMovie> listMovies)
        {
            return base.CleanupListItems(listMovies)
                .Take(MaxResultsPerQuery)
                .ToList();
        }
    }
}
