using System.Linq;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.ImportLists.StashDB;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ImportListTests.StashDB
{
    [TestFixture]
    public class StashDBParserFixture : CoreTest
    {
        [Test]
        public void should_exclude_matching_scenes_case_insensitively()
        {
            var subject = new StashDBParser(new[] { "excluded-tag" });
            var response = CreateResponse(
                """
                {
                  "data": {
                    "queryScenes": {
                      "count": 3,
                      "scenes": [
                        {
                          "id": "excluded-scene",
                          "title": "Excluded",
                          "release_date": "2025-01-01",
                          "tags": [{ "id": "EXCLUDED-TAG" }]
                        },
                        {
                          "id": "included-scene",
                          "title": "Included",
                          "release_date": "2025-01-02",
                          "tags": [{ "id": "other-tag" }]
                        },
                        {
                          "id": "tagless-scene",
                          "title": "Tagless",
                          "release_date": "2025-01-03",
                          "tags": []
                        }
                      ]
                    }
                  }
                }
                """);

            var result = subject.ParseResponse(response);

            result.Select(movie => movie.StashId).Should().Equal("included-scene", "tagless-scene");
        }

        [Test]
        public void should_keep_all_scenes_when_no_excluded_tags_are_set()
        {
            var subject = new StashDBParser();
            var response = CreateResponse(
                """
                {
                  "data": {
                    "queryScenes": {
                      "count": 1,
                      "scenes": [
                        {
                          "id": "scene",
                          "title": "Scene",
                          "release_date": "2025-01-01",
                          "tags": [{ "id": "tag" }]
                        }
                      ]
                    }
                  }
                }
                """);

            subject.ParseResponse(response).Should().ContainSingle();
        }

        private static ImportListResponse CreateResponse(string content)
        {
            var request = new HttpRequest("https://stashdb.org/graphql");
            var response = new HttpResponse(request, new HttpHeader(), Encoding.UTF8.GetBytes(content));

            return new ImportListResponse(new ImportListRequest(request), response);
        }
    }
}
