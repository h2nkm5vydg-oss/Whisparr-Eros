using Newtonsoft.Json;

namespace NzbDrone.Core.ImportLists.StashDB.Studio
{
    public class QueryTagsSceneQuery
    {
        private QueryTagsSceneQueryVariables _variables;
        private string _query;

        public QueryTagsSceneQuery(int page, int pageSize, FilterType tagFilter, bool includeSceneTags, SceneSort sort, string afterDate)
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
            _variables = new QueryTagsSceneQueryVariables(page, pageSize, tagFilter, sort, afterDate);
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

    public class QueryTagsSceneQueryVariables : QuerySceneQueryVariablesBase
    {
        public QueryTagsSceneQueryVariables(int page, int pageSize, FilterType tagFilter, SceneSort sort, string afterDate)
            : base(page, pageSize, sort, afterDate)
        {
            if (tagFilter != null)
            {
                Input.tags = tagFilter;
            }

            Input.sort = sort;
        }
    }
}
