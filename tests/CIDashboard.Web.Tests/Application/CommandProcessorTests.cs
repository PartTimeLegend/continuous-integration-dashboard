using System.Threading.Tasks;
using AutoMapper;
using CIDashboard.Data.Interfaces;
using CIDashboard.Web.Application;
using CIDashboard.Web.MappingProfiles;
using CIDashboard.Web.Models;
using FakeItEasy;
using FluentAssertions;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoFakeItEasy;
// ReSharper disable ObjectCreationAsStatement

namespace CIDashboard.Web.Tests.Application
{
    [TestFixture]
    public class CommandProcessorTests
    {
        private IFixture _fixture;
        private ICiDashboardService _ciDashboardService;

        [SetUp]
        public void Setup()
        {
            new MapperConfiguration(cfg =>
            { cfg.AddProfile<ViewModelProfilers>();
            });
               
            _fixture = new Fixture()
                .Customize(new AutoFakeItEasyCustomization());

            _ciDashboardService = A.Fake<ICiDashboardService>();
        }

        [Test]
        public async Task AddNewProject_ReturnsCreatedProject()
        {
            string username = _fixture.Create<string>();
            Project project = new Project { Name = _fixture.Create<string>() };

            CommandProcessor commandController = new CommandProcessor();
            commandController.CiDashboardService = _ciDashboardService;

            Project result = await commandController.AddNewProject(username, project);

            A.CallTo(() => _ciDashboardService.AddProject(username, 
                A<Data.Entities.Project>.That.Matches(p => p.Name == project.Name)))
                .MustHaveHappened();

            result.Should()
                .NotBeNull()
                .And.BeOfType<Project>();
        }

        [Test]
        public async Task UpdateProjectName_ReturnsTrueOnSuccess()
        {
            Project project = _fixture.Create<Project>();

            CommandProcessor commandController = new CommandProcessor();
            commandController.CiDashboardService = _ciDashboardService;

            bool result = await commandController.UpdateProjectName(project.Id, project.Name);

            A.CallTo(() => _ciDashboardService.UpdateProjectName(project.Id, project.Name))
                .MustHaveHappened();

            result.Should()
                .BeTrue();
        }

        [Test]
        public async Task UpdateProjectOrder_ReturnsTrueOnSuccess()
        {
            Project project = _fixture.Create<Project>();

            CommandProcessor commandController = new CommandProcessor();
            commandController.CiDashboardService = _ciDashboardService;

            int newPosition = _fixture.Create<int>();
            bool result = await commandController.UpdateProjectOrder(project.Id, newPosition);

            A.CallTo(() => _ciDashboardService.UpdateProjectOrder(project.Id, newPosition))
                .MustHaveHappened();

            result.Should()
                .BeTrue();
        }

        [Test]
        public async Task RemoveProject_ReturnsRemovedProject()
        {
            int projectId = _fixture.Create<int>();

            CommandProcessor commandController = new CommandProcessor();
            commandController.CiDashboardService = _ciDashboardService;

            Project result = await commandController.RemoveProject(projectId);

            A.CallTo(() => _ciDashboardService.RemoveProject(projectId))
                .MustHaveHappened();

            result.Should()
                .NotBeNull()
                .And.BeOfType<Project>();
        }

        [Test]
        public async Task AddBuildToProject_ReturnsCreatedBuild()
        {
            int projectId = _fixture.Create<int>();
            BuildConfig build = _fixture.Create<BuildConfig>();

            CommandProcessor commandController = new CommandProcessor();
            commandController.CiDashboardService = _ciDashboardService;

            BuildConfig result = await commandController.AddBuildConfigToProject(projectId, build);

            A.CallTo(() => _ciDashboardService
                .AddBuildConfigToProject(projectId, A<Data.Entities.BuildConfig>.That.Matches(p => p.Name == build.Name)))
                .MustHaveHappened();

            result.Should()
                .NotBeNull()
                .And.BeOfType<BuildConfig>();
        }

        [Test]
        public async Task RemoveBuildConfig_ReturnsRemovedBuildConfig()
        {
            int buidlId = _fixture.Create<int>();

            CommandProcessor commandController = new CommandProcessor();
            commandController.CiDashboardService = _ciDashboardService;

            BuildConfig result = await commandController.RemoveBuildConfig(buidlId);

            A.CallTo(() => _ciDashboardService.RemoveBuildConfig(buidlId))
                .MustHaveHappened();

            result.Should()
                .NotBeNull()
                .And.BeOfType<BuildConfig>();
        }

        [Test]
        public async Task UpdateBuildConfigNameAndExternalId_ReturnsTrueOnSuccess()
        {
            BuildConfig build = _fixture.Create<BuildConfig>();

            CommandProcessor commandController = new CommandProcessor();
            commandController.CiDashboardService = _ciDashboardService;

            bool result = await commandController.UpdateBuildConfigExternalId(build.Id, build.Name, build.CiExternalId);

            A.CallTo(() => _ciDashboardService.UpdateBuildConfigExternalId(build.Id, build.Name, build.CiExternalId))
                .MustHaveHappened();

            result.Should()
                .BeTrue();
        }

        [Test]
        public async Task UpdateBuildConfigOrder_ReturnsTrueOnSuccess()
        {
            BuildConfig build = _fixture.Create<BuildConfig>();

            CommandProcessor commandController = new CommandProcessor();
            commandController.CiDashboardService = _ciDashboardService;

            int newPosition = _fixture.Create<int>();
            bool result = await commandController.UpdateBuildConfigOrder(build.Id, newPosition);

            A.CallTo(() => _ciDashboardService.UpdateBuildConfigOrder(build.Id, newPosition))
                .MustHaveHappened();

            result.Should()
                .BeTrue();
        }
    }
}
