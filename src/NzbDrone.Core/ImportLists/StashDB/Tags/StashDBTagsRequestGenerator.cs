using System.Collections.Generic;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.ImportLists.StashDB.Studio
{
    public class StashDBTagsRequestGenerator : IImportListRequestGenerator
    {
        public StashDBTagsRequestGenerator(int pageSize, int maxResultsPerQuery)
        {
            _pageSize = pageSize;
            _maxResultsPerQuery = maxResultsPerQuery;
        }

        private readonly int _pageSize;
        private readonly int _maxResultsPerQuery;
        public StashDBTagsSettings Settings { get; set; }
        public IHttpClient HttpClient { get; set; }
        public IHttpRequestBuilderFactory RequestBuilder { get; set; }
        public Logger Logger { get; set; }
        public virtual ImportListPageableRequestChain GetMovies()
        {
            var pageableRequests = new ImportListPageableRequestChain();

            pageableRequests.Add(GetSceneRequest());

            return pageableRequests;
        }

        private IEnumerable<ImportListRequest> GetSceneRequest()
        {
            var parameterLog = string.Empty;

            var includedTags = StashDBTagFilter.ParseIds(Settings.IncludedTags);
            if (includedTags.Count > 0)
            {
                parameterLog += $"\r\n Included Tags: {includedTags.Join(",")}";
            }

            var excludedTags = StashDBTagFilter.ParseIds(Settings.ExcludedTags);
            if (excludedTags.Count > 0)
            {
                parameterLog += $"\r\n Excluded Tags: {excludedTags.Join(",")}";
            }

            Logger.Info($"Importing StashDB scenes for tags: {parameterLog}");

            var tagFilter = StashDBTagFilter.GetServerFilter(Settings);
            var querySceneQuery = new QueryTagsSceneQuery(
                1,
                _pageSize,
                tagFilter,
                StashDBTagFilter.HasCombinedFilters(Settings),
                (SceneSort)Settings.Sort,
                Settings.AfterDate);

            var requestBuilder = RequestBuilder
                                        .Create()
                                        .SetHeader("ApiKey", Settings.ApiKey)
                                        .AddQueryParam("query", querySceneQuery.Query)
                                        .AddQueryParam("variables", querySceneQuery.Variables);

            var jsonResponse = JsonConvert.DeserializeObject<QueryScenesResult>(HttpClient.Execute(requestBuilder.Build()).Content);

            var pages = StashDBTagFilter.GetPageCount(jsonResponse.Data.QueryScenes.Count, _pageSize, _maxResultsPerQuery);

            var requests = new List<ImportListRequest>();

            for (var pageNumber = 1; pageNumber <= pages; pageNumber++)
            {
                querySceneQuery.SetPage(pageNumber);

                requestBuilder.AddQueryParam("variables", querySceneQuery.Variables, true);

                var request = requestBuilder.Build();

                Logger.Debug($"Importing StashDB scenes from {request.Url}");

                requests.Add(new ImportListRequest(request));
            }

            return requests;
        }
    }
}
