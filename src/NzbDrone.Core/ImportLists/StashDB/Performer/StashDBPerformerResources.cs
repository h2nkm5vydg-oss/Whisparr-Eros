using System.Collections.Generic;
using Newtonsoft.Json;

namespace NzbDrone.Core.ImportLists.StashDB.Performer
{
    public class QueryPerformerSceneQuery
    {
        private QueryPerformerSceneQueryVariables _variables;
        private string _query;

        public QueryPerformerSceneQuery(int page, int pageSize, List<string> performers, List<string> studios, FilterModifier studiosFilter, FilterType tagFilter, bool includeSceneTags, bool onlyFavoriteStudios, SceneSort sort, string afterDate)
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
            _variables = new QueryPerformerSceneQueryVariables(page, pageSize, performers, studios, studiosFilter, tagFilter, onlyFavoriteStudios, sort, afterDate);
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

    public class QueryPerformerSceneQueryVariables : QuerySceneQueryVariablesBase
    {
        public QueryPerformerSceneQueryVariables(int page, int pageSize, List<string> performers, List<string> studios, FilterModifier studiosFilter, FilterType tagFilter, bool onlyFavoriteStudios, SceneSort sort, string afterDate)
            : base(page, pageSize, sort, afterDate)
        {
            Input.performers = new FilterType(FilterModifier.INCLUDES, performers);
            if (studios.Count > 0)
            {
                Input.studios = new FilterType(studiosFilter, studios);
            }

            if (tagFilter != null)
            {
                Input.tags = tagFilter;
            }

            if (onlyFavoriteStudios)
            {
                Input.favorites = FavoriteFilter.STUDIO;
            }
        }
    }
}
