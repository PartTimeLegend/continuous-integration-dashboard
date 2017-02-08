using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using CIDashboard.Domain.Entities;
using CIDashboard.Domain.MappingProfiles;
using FluentAssertions;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoFakeItEasy;
using TeamCitySharp.DomainEntities;

namespace CIDashboard.Domain.Tests.MappingProfiles
{
    [TestFixture]
    public class TeamCityProfilerTests
    {
        private IFixture _fixture;

        [SetUp]
        public void Setup()
        {
            _fixture = new Fixture()
                .Customize(new AutoFakeItEasyCustomization());
            _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            
            Mapper.Initialize(cfg => cfg.AddProfile<TeamCityProfiler>());
        }

        [Test]
        public void MapsProjetsAndBuildTypesCorrectly()
        {
            IEnumerable<Project> projects = _fixture
                .Build<Project>()
                .CreateMany();

            IEnumerable<Project> enumerable = projects as IList<Project> ?? projects.ToList();
            IEnumerable<CiProject> mappedResult = Mapper.Map<IEnumerable<Project>, IEnumerable<CiProject>>(enumerable);

            IEnumerable<CiProject> expectedResult = enumerable.Select(
                p => new CiProject
                {
                    CiSource = CiSource.TeamCity,
                    Id = p.Id,
                    Name = p.Name,
                    Url = p.WebUrl,
                    BuildConfigs = p.BuildTypes.BuildType.Select(
                        b => new CiBuildConfig
                        {
                            CiSource = CiSource.TeamCity,
                            Id = b.Id,
                            Name = b.Name,
                            Url = b.WebUrl,
                            ProjectName = b.ProjectName
                        })
                });

            mappedResult.ShouldBeEquivalentTo(expectedResult);
        }

        [TestCase("SUCCESS", CiBuildResultStatus.Success)]
        [TestCase("FAILURE", CiBuildResultStatus.Failure)]
        public void MapsBuildsCorrectly(string status, CiBuildResultStatus resultStatus)
        {
            Build build = _fixture
                .Build<Build>()
                .With(b => b.Status, status)
                .Create();

            CiBuildResult mappedResult = Mapper.Map<Build, CiBuildResult>(build);

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

            mappedResult.ShouldBeEquivalentTo(expectedResult);
        }
    }
}
