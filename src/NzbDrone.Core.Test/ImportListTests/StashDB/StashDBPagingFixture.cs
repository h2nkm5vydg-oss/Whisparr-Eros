using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NLog;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.ImportLists.ImportListMovies;
using NzbDrone.Core.ImportLists.StashDB;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ImportListTests.StashDB
{
    [TestFixture]
    public class StashDBPagingFixture : CoreTest
    {
        [Test]
        public void should_continue_short_filtered_pages_and_trim_to_final_limit()
        {
            var subject = CreateSubject(
                CreateMovies(60, "first"),
                CreateMovies(60, "second"),
                CreateMovies(60, "unused"));

            var result = subject.Fetch();

            result.Movies.Should().HaveCount(100);
            subject.FetchCount.Should().Be(2);
            subject.CandidateResultLimit.Should().Be(1000);
        }

        [Test]
        public void should_return_available_results_when_candidates_are_exhausted()
        {
            var subject = CreateSubject(
                CreateMovies(30, "first"),
                new List<ImportListMovie>());

            var result = subject.Fetch();

            result.Movies.Should().HaveCount(30);
            subject.FetchCount.Should().Be(2);
        }

        [Test]
        public void connection_test_should_continue_after_an_entirely_excluded_page()
        {
            var subject = CreateSubject(
                new List<ImportListMovie>(),
                CreateMovies(1, "second"));

            var result = subject.Test();

            result.Errors.Should().BeEmpty();
            subject.FetchCount.Should().Be(2);
        }

        [Test]
        public void should_not_expand_candidate_limit_without_combined_filters()
        {
            var subject = CreateSubject(CreateMovies(1, "first"));
            var settings = (TestStashDBSettings)subject.Definition.Settings;
            settings.ExcludedTags = null;

            subject.CandidateResultLimit.Should().Be(100);
        }

        [Test]
        public void should_cap_requested_results_at_global_candidate_limit()
        {
            var subject = CreateSubject(CreateMovies(1, "first"));
            var settings = (TestStashDBSettings)subject.Definition.Settings;
            settings.Limit = 2000;

            subject.CandidateResultLimit.Should().Be(1000);
        }

        private TestStashDBImport CreateSubject(params IList<ImportListMovie>[] pages)
        {
            var settings = new TestStashDBSettings
            {
                ApiKey = "api-key",
                Limit = 100,
                IncludedTags = "included",
                ExcludedTags = "excluded"
            };

            var subject = new TestStashDBImport(
                Mocker.Resolve<IHttpClient>(),
                Mocker.Resolve<IImportListStatusService>(),
                Mocker.Resolve<IConfigService>(),
                Mocker.Resolve<IParsingService>(),
                TestLogger,
                pages);

            subject.Definition = new ImportListDefinition
            {
                Id = 1,
                Name = "Test StashDB",
                Settings = settings
            };

            return subject;
        }

        private static IList<ImportListMovie> CreateMovies(int count, string prefix)
        {
            return Enumerable.Range(0, count)
                .Select(index => new ImportListMovie
                {
                    StashId = $"{prefix}-{index}",
                    Title = $"{prefix} {index}"
                })
                .ToList();
        }
    }

    public class TestStashDBSettings : StashDBSettingsBase<TestStashDBSettings>, IStashDBTagFilterSettings
    {
        public string IncludedTags { get; set; }
        public string ExcludedTags { get; set; }
    }

    public class TestStashDBImport : StashDBImportBase<TestStashDBSettings>
    {
        private readonly IList<IList<ImportListMovie>> _pages;

        public TestStashDBImport(
            IHttpClient httpClient,
            IImportListStatusService importListStatusService,
            IConfigService configService,
            IParsingService parsingService,
            Logger logger,
            IList<IList<ImportListMovie>> pages)
            : base(httpClient, importListStatusService, configService, parsingService, logger)
        {
            _pages = pages;
        }

        public override string Name => "Test StashDB";
        public int FetchCount { get; private set; }
        public int CandidateResultLimit => MaxCandidateResults;

        public override IImportListRequestGenerator GetRequestGenerator()
        {
            return new TestStashDBRequestGenerator(_pages.Count);
        }

        protected override IList<ImportListMovie> FetchPage(ImportListRequest request, IParseImportListResponse parser)
        {
            return _pages[FetchCount++];
        }
    }

    public class TestStashDBRequestGenerator : IImportListRequestGenerator
    {
        private readonly int _pageCount;

        public TestStashDBRequestGenerator(int pageCount)
        {
            _pageCount = pageCount;
        }

        public ImportListPageableRequestChain GetMovies()
        {
            var requests = Enumerable.Range(1, _pageCount)
                .Select(page => new ImportListRequest(new HttpRequest($"https://stashdb.org/graphql?page={page}")))
                .ToList();

            var chain = new ImportListPageableRequestChain();
            chain.Add(requests);

            return chain;
        }
    }
}
