using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIDashboard.Domain.Entities;
using CIDashboard.Domain.MappingProfiles;
using CIDashboard.Domain.Services;
using FakeItEasy;
using FluentAssertions;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoFakeItEasy;
using TeamCitySharp;
using TeamCitySharp.DomainEntities;
using TeamCitySharp.Locators;
// ReSharper disable ObjectCreationAsStatement

namespace CIDashboard.Domain.Tests.Services
{
    [TestFixture]
    public class TeamCityServiceTests
    {
        private IFixture _fixture;
        private ITeamCityClient _teamcityClient;

        [SetUp]
        public void Setup()
        {
            _fixture = new Fixture()
                .Customize(new AutoFakeItEasyCustomization());
            _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        
            Mapper.Initialize(cfg => cfg.AddProfile<TeamCityProfiler>());

            _teamcityClient = A.Fake<ITeamCityClient>();
        }

        [Test]
        public void ParameterlessConstructor_LoginsAsGuest()
        {
            new TeamCityService(_teamcityClient);
            A.CallTo(() => _teamcityClient.ConnectAsGuest()).MustHaveHappened();
        }

        [Test]
        public void ConstructorWithParameters_LoginsWithUserPwd()
        {
            new TeamCityService(_teamcityClient, "user", "pwd");
            A.CallTo(() => _teamcityClient.Connect("user", "pwd")).MustHaveHappened();
        }

        [Test]
        public async Task GetAllBuildConfigs_ReturnsProjectAndBuildTypesCorrectlyMapped_FilteringTheOnesThatTheProjectIsArchived()
        {
            IEnumerable<BuildConfig> buildConfigs = _fixture
                .Build<BuildConfig>()
                .CreateMany();

            IEnumerable<BuildConfig> enumerable = buildConfigs as IList<BuildConfig> ?? buildConfigs.ToList();
            List<CiBuildConfig> expectedResult = enumerable.Select(
                b => new CiBuildConfig
                {
                    CiSource = CiSource.TeamCity,
                    Id = b.Id,
                    Name = b.Name,
                    Url = b.WebUrl,
                    ProjectName = b.ProjectName
                })
                .ToList();

            A.CallTo(() => _teamcityClient.BuildConfigs.All())
                .Returns(enumerable.ToList());

            foreach(BuildConfig buildConfig in enumerable)
            {
                Project project = _fixture
                    .Build<Project>()
                    .With(p => p.Id, buildConfig.ProjectId)
                    .Create();
                A.CallTo(() => _teamcityClient.Projects.ById(buildConfig.ProjectId))
                    .Returns(project);

                if(project.Archived)
                    expectedResult.Remove(expectedResult.First(b => b.Id == buildConfig.Id));
            }

            TeamCityService teamCityService = new TeamCityService(_teamcityClient);
            IEnumerable<CiBuildConfig> result = await teamCityService.GetAllBuildConfigs();

            result.ShouldBeEquivalentTo(expectedResult);     
        }

        [TestCase("SUCCESS", CiBuildResultStatus.Success)]
        [TestCase("FAILURE", CiBuildResultStatus.Failure)]
        public async Task LastBuildResult_ReturnsInfoCorrectlyMapped(string status, CiBuildResultStatus resultStatus)
        {
            string buildId = _fixture.Create<string>();
            Build build = _fixture
                .Build<Build>()
                .With(b => b.Status, status)
                .Create();

            A.CallTo(() => _teamcityClient.Builds.LastBuildByBuildConfigId(buildId))
                .Returns(build);

            A.CallTo(() => _teamcityClient.Builds.ByBuildId(build.Id))
                .Returns(build);

            TeamCityService teamCityService = new TeamCityService(_teamcityClient);
            CiBuildResult result = await teamCityService.LastBuildResult(buildId);

            CiBuildResult expectedResult = new CiBuildResult
            {
                CiSource = CiSource.TeamCity,
                Id = build.Id,
                BuildId = build.BuildType.Id,
                BuildName = build.BuildType.Name,
                Url = build.WebUrl,
                FinishDate = build.FinishDate,
                StartDate = build.StartDate,
                Version = build.Number,
                Status = resultStatus
            };

            result.ShouldBeEquivalentTo(expectedResult);
        }

        [Test]
        public async Task LastBuildResult_ReturnsStatisticsCorrectlyMapped()
        {
            string buildId = _fixture.Create<string>();
            Build build = _fixture
                .Build<Build>()
                .With(b => b.Status, "SUCCESS")
                .Create();

            A.CallTo(() => _teamcityClient.Builds.LastBuildByBuildConfigId(buildId))
                .Returns(build);

            A.CallTo(() => _teamcityClient.Builds.ByBuildId(build.Id))
                .Returns(build);

            List<Property> stats = new List<Property>
            {
                new Property{Name = "PassedTestCount", Value = "1"},
                new Property{Name = "FailedTestCount", Value = "2"},
                new Property{Name = "IgnoredTestCount", Value = "3"},
                new Property{Name = "CodeCoverageAbsSCovered", Value = "4"},
                new Property{Name = "CodeCoverageAbsSTotal", Value = "5"}
            };
            A.CallTo(() => _teamcityClient.Statistics.GetByBuildId(build.Id))
                .Returns(stats);

            TeamCityService teamCityService = new TeamCityService(_teamcityClient);
            CiBuildResult result = await teamCityService.LastBuildResult(buildId);

            CiBuildResult expectedResult = new CiBuildResult
            {
                CiSource = CiSource.TeamCity,
                Id = build.Id,
                BuildId = build.BuildType.Id,
                BuildName = build.BuildType.Name,
                Url = build.WebUrl,
                FinishDate = build.FinishDate,
                StartDate = build.StartDate,
                Version = build.Number,
                Status = CiBuildResultStatus.Success,
                NumberTestPassed = 1,
                NumberTestFailed = 2,
                NumberTestIgnored = 3,
                NumberStatementsCovered = 4,
                NumberStatementsTotal = 5
            };

            result.ShouldBeEquivalentTo(expectedResult);
        }

        [Test]
        public async Task LastBuildResult_WhenBuildIsRunning_ReturnsRunningBuildStatus()
        {
            string buildId = _fixture.Create<string>();
            Build build = _fixture
                .Build<Build>()
                .Create();

            A.CallTo(() => _teamcityClient.Builds.LastBuildByBuildConfigId(buildId))
                .Returns(build);

            A.CallTo(() => _teamcityClient.Builds.ByBuildId(build.Id))
                .Returns(build);

            A.CallTo(() => 
                _teamcityClient.Builds.ByBuildLocator(A<BuildLocator>.Ignored))
                .Returns(_fixture.Build<Build>().CreateMany().ToList());

            TeamCityService teamCityService = new TeamCityService(_teamcityClient);
            CiBuildResult result = await teamCityService.LastBuildResult(buildId);

            CiBuildResult expectedResult = new CiBuildResult
            {
                CiSource = CiSource.TeamCity,
                Id = build.Id,
                BuildId = build.BuildType.Id,
                BuildName = build.BuildType.Name,
                Url = build.WebUrl,
                FinishDate = build.FinishDate,
                StartDate = build.StartDate,
                Version = build.Number,
                Status = CiBuildResultStatus.Running
            };

            result.ShouldBeEquivalentTo(expectedResult);
        }
    }
}
