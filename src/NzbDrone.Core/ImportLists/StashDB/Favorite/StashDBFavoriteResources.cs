using Newtonsoft.Json;

namespace NzbDrone.Core.ImportLists.StashDB.Favorite
{
    public class QueryFavoriteSceneQuery
    {
        private QueryFavoriteSceneQueryVariables _variables;
        private string _query;

        public QueryFavoriteSceneQuery(int page, int pageSize, FavoriteFilter filter, FilterType tagFilter, bool includeSceneTags, SceneSort sort, string afterDate)
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
            _variables = new QueryFavoriteSceneQueryVariables(page, pageSize, filter, tagFilter, sort, afterDate);
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

    public class QueryFavoriteSceneQueryVariables : QuerySceneQueryVariablesBase
    {
        public QueryFavoriteSceneQueryVariables(int page, int pageSize, FavoriteFilter filter, FilterType tagFilter, SceneSort sort, string dateAfter)
            : base(page, pageSize, sort, dateAfter)
        {
            Input.favorites = filter;

            if (tagFilter != null)
            {
                Input.tags = tagFilter;
            }
        }
    }
}
