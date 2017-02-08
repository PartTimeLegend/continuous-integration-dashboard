using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIDashboard.Data.Interfaces;
using CIDashboard.Domain.Entities;
using CIDashboard.Domain.Services;
using CIDashboard.Web.Application;
using CIDashboard.Web.MappingProfiles;
using CIDashboard.Web.Models;
using FakeItEasy;
using FluentAssertions;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoFakeItEasy;
using BuildConfig = CIDashboard.Web.Models.BuildConfig;
using Project = CIDashboard.Data.Entities.Project;
// ReSharper disable ObjectCreationAsStatement

namespace CIDashboard.Web.Tests.Application
{
    [TestFixture]
    public class InformationQueryTests
    {
        private IFixture _fixture;
        private ICiDashboardService _ciDashboardService;
        private ICiServerService _ciServerService;

        [SetUp]
        public void Setup()
        {
            new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ViewModelProfilers>();
            });
            _fixture = new Fixture()
                .Customize(new AutoFakeItEasyCustomization());
            _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

            _ciDashboardService = A.Fake<ICiDashboardService>();
            _ciServerService = A.Fake<ICiServerService>();
        }

        [Test]
        public async Task GetUserProjectsAndBuildConfigs_QueriesForUserProjects_AndReturnsMappedValues()
        {
            string username = _fixture.Create<string>();

            InformationQuery infoQuery = new InformationQuery { CiDashboardService = _ciDashboardService };

            List<Project> projects = _fixture
                .CreateMany<Project>()
                .ToList();

            A.CallTo(() => _ciDashboardService.GetProjects(username))
                .Returns(projects);
            IEnumerable<Models.Project> mappedProjects = Mapper.Map<IEnumerable<Project>, IEnumerable<Models.Project>>(projects);

            IEnumerable<Models.Project> result = await infoQuery.GetUserProjectsAndBuildConfigs(username);

            A.CallTo(() => _ciDashboardService.GetProjects(username)).MustHaveHappened();

            result.ShouldBeEquivalentTo(mappedProjects);
        }

        [Test]
        public async Task GetAllProjectBuildConfigs_QueriesForAllBuildConfigs_AndReturnsMappedValues()
        {
            InformationQuery infoQuery = new InformationQuery { CiServerService = _ciServerService };

            List<CiBuildConfig> builds = _fixture
                .CreateMany<CiBuildConfig>()
                .ToList();
            
            A.CallTo(() => _ciServerService
                .GetAllBuildConfigs())
                .Returns(builds);
            IEnumerable<BuildConfig> mappedBuilds = Mapper.Map<IEnumerable<CiBuildConfig>, IEnumerable<BuildConfig>>(builds);

            IEnumerable<BuildConfig> result = await infoQuery.GetAllProjectBuildConfigs();

            A.CallTo(() => _ciServerService
                .GetAllBuildConfigs())
                .MustHaveHappened();

            result.ShouldBeEquivalentTo(mappedBuilds);
        }

        [Test]
        public async Task GetLastBuildResult_QueriesForLastBuildResult_AndReturnsMappedValues()
        {
            InformationQuery infoQuery = new InformationQuery { CiServerService = _ciServerService };

            CiBuildResult build = _fixture
                .Build<CiBuildResult>()
                .With(p => p.Id, _fixture.Create<int>().ToString())
                .Create();

            A.CallTo(() => _ciServerService
                .LastBuildResult(build.BuildId))
                .Returns(build);
            Build mappedBuild = Mapper.Map<CiBuildResult, Build>(build);

            Build result = await infoQuery.GetLastBuildResult(build.BuildId);

            A.CallTo(() => _ciServerService
                .LastBuildResult(build.BuildId))
                .MustHaveHappened();

            result.ShouldBeEquivalentTo(mappedBuild);
        }
    }
}
