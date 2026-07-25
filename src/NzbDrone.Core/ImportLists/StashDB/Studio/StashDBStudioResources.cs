using System.Collections.Generic;
using Newtonsoft.Json;

namespace NzbDrone.Core.ImportLists.StashDB.Studio
{
    public class QueryStudioSceneQuery
    {
        private QueryStudioSceneQueryVariables _variables;
        private string _query;

        public QueryStudioSceneQuery(int page, int pageSize, List<string> studios, FilterType tagFilter, bool includeSceneTags, bool onlyFavoriteStudios, SceneSort sort, string afterDate)
        {
            var tagsQuery = includeSceneTags
                ? @"
                             tags {
                               id
                             }"
                : string.Empty;

            _query = $@"query Scenes($input: SceneQueryInput!) {{
                         queryScenes(input: $input) {{
                           scenes {{
                             id
                             title
                             release_date{tagsQuery}
                           }}
                           count
                         }}
                        }}";
            _variables = new QueryStudioSceneQueryVariables(page, pageSize, studios, tagFilter, onlyFavoriteStudios, sort, afterDate);
        }

        public string Query
        {
            get
            {
                return _query;
            }
        }

        public string Variables
        {
            get
            {
                return JsonConvert.SerializeObject(_variables);
            }
        }

        public void SetPage(int page)
        {
            _variables.Input.page = page;
        }
    }

    public class QueryStudioSceneQueryVariables : QuerySceneQueryVariablesBase
    {
        public QueryStudioSceneQueryVariables(int page, int pageSize, List<string> studios, FilterType tagFilter, bool onlyFavoritePerformers, SceneSort sort, string afterDate)
            : base(page, pageSize, sort, afterDate)
        {
            Input.studios = new FilterType(FilterModifier.INCLUDES, studios);

            if (tagFilter != null)
            {
                Input.tags = tagFilter;
            }

            if (onlyFavoritePerformers)
            {
                Input.favorites = FavoriteFilter.PERFORMER;
            }

            Input.sort = sort;
        }
    }
}
