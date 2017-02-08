using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIDashboard.Web.Application;
using CIDashboard.Web.MappingProfiles;
using CIDashboard.Web.Models;
using FluentAssertions;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoFakeItEasy;
// ReSharper disable ObjectCreationAsStatement

namespace CIDashboard.Web.Tests.Application
{
    [TestFixture]
    public class ConnectionsManagerTests
    {
        private IFixture _fixture;

        [SetUp]
        public void Setup()
        {
            new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ViewModelProfilers>();
            });
            
            _fixture = new Fixture()
                .Customize(new AutoFakeItEasyCustomization());

            // clean any existing info in static dictionaties
            ConnectionsManager connManager = new ConnectionsManager();
            ICollection<string> connIds = connManager.BuildsPerConnId.Keys;
            foreach(string connId in connIds)
            {
                connManager.RemoveAllBuildConfigs(connId).Wait(); 
            }
        }

        [Test]
        public async Task AddBuildConfigs_AddsConnectionInfoStaticDictionaries()
        {
            string connectionId = _fixture.Create<string>();
            List<BuildConfig> builds = _fixture
                .Build<BuildConfig>()
                .CreateMany()
                .ToList();
            List<string> buildsIds = builds.Select(b => b.CiExternalId)
                .ToList();

            ConnectionsManager connManager = new ConnectionsManager();
            await connManager.AddBuildConfigs(connectionId, builds);

            connManager.BuildsPerConnId.ContainsKey(connectionId).Should().BeTrue();
            connManager.BuildsPerConnId[connectionId].ShouldAllBeEquivalentTo(buildsIds);
            connManager.BuildsToBeRefreshed.Should().ContainKeys(buildsIds.ToArray());
        }

        [Test]
        public async Task AddBuildConfigs_DontDuplicateBuildsToBeRefreshed()
        {
            string connectionId = _fixture.Create<string>();
            List<BuildConfig> builds = _fixture
                .Build<BuildConfig>()
                .CreateMany()
                .ToList();
            List<string> buildsIds = builds.Select(b => b.CiExternalId)
                .ToList();

            ConnectionsManager connManager = new ConnectionsManager();
            await connManager.AddBuildConfigs(_fixture.Create<string>(), new[] { builds.First() });

            await connManager.AddBuildConfigs(connectionId, builds);

            connManager.BuildsPerConnId.ContainsKey(connectionId).Should().BeTrue();
            connManager.BuildsPerConnId[connectionId].ShouldAllBeEquivalentTo(buildsIds);
            connManager.BuildsToBeRefreshed.Should().ContainKeys(buildsIds.ToArray());
        }

        [Test]
        public async Task AddBuildConfigs_WhenConnectionAlreadyExists_RefreshsBuildConfigs()
        {
            string connectionId = _fixture.Create<string>();
            List<BuildConfig> builds = _fixture
                .Build<BuildConfig>()
                .CreateMany()
                .ToList();
            List<string> buildsIds = builds.Select(b => b.CiExternalId)
                .ToList();
            List<BuildConfig> olderBuilds = _fixture
                .Build<BuildConfig>()
                .CreateMany()
                .ToList();
            List<string> olderBuildsIds = olderBuilds
                .Select(b => b.CiExternalId)
                .ToList();

            ConnectionsManager connManager = new ConnectionsManager();
            await connManager.AddBuildConfigs(connectionId, olderBuilds);
            connManager.BuildsPerConnId.ContainsKey(connectionId).Should().BeTrue();
            connManager.BuildsPerConnId[connectionId].ShouldAllBeEquivalentTo(olderBuildsIds);

            await connManager.AddBuildConfigs(connectionId, builds);

            connManager.BuildsPerConnId.ContainsKey(connectionId).Should().BeTrue();
            connManager.BuildsPerConnId[connectionId].ShouldAllBeEquivalentTo(buildsIds);
        }

        [Test]
        public async Task UpdateBuildConfigs_DontDuplicateBuildsToBeRefreshed()
        {
            string connectionId = _fixture.Create<string>();
            List<BuildConfig> builds = _fixture
                .Build<BuildConfig>()
                .CreateMany()
                .ToList();

            List<BuildConfig> otherBuilds = _fixture
                .Build<BuildConfig>()
                .CreateMany()
                .ToList();
            otherBuilds.Add(builds.First());

            List<string> buildsIds = builds.Select(b => b.CiExternalId).ToList();
            buildsIds.AddRange(otherBuilds.Select(b => b.CiExternalId));
            buildsIds = buildsIds.Distinct().ToList();

            ConnectionsManager connManager = new ConnectionsManager();
            await connManager.AddBuildConfigs(connectionId, builds);

            await connManager.UpdateBuildConfigs(connectionId, otherBuilds);

            connManager.BuildsPerConnId.ContainsKey(connectionId).Should().BeTrue();
            connManager.BuildsPerConnId[connectionId].ShouldAllBeEquivalentTo(buildsIds);
            connManager.BuildsToBeRefreshed.Should().ContainKeys(buildsIds.ToArray());
        }

        [Test]
        public async Task RemoveAllBuildConfigs_ShouldRemoveAllBuildsForConnectionId()
        {
            string connectionId = _fixture.Create<string>();
            IEnumerable<BuildConfig> builds = _fixture
                .Build<BuildConfig>()
                .CreateMany();

            ConnectionsManager connManager = new ConnectionsManager();
            await connManager.AddBuildConfigs(connectionId, builds);

            connManager.BuildsPerConnId.Keys.Should()
                .Contain(connectionId);

            await connManager.RemoveAllBuildConfigs(connectionId);

            connManager.BuildsPerConnId.Keys.Should().BeEmpty();
            connManager.BuildsToBeRefreshed.Keys.Should().BeEmpty();
        }

        [Test]
        public async Task RemoveAllBuildConfigs_WhenOtherConnectionIdsAreUsingIt_ShouldNotRemoveTheBuild()
        {
            BuildConfig duplicateBuild = _fixture.Create<BuildConfig>();
            string duplicateBuildId = duplicateBuild.CiExternalId;

            string connectionId = _fixture.Create<string>();
            List<BuildConfig> builds = _fixture
                .Build<BuildConfig>()
                .CreateMany()
                .ToList();
            builds.Add(duplicateBuild);

            string otherConnectionId = _fixture.Create<string>();
            List<BuildConfig> otherBuilds = _fixture
                .Build<BuildConfig>()
                .CreateMany()
                .ToList();
            otherBuilds.Add(duplicateBuild);
            List<string> otherBuildsIds = otherBuilds
                .Select(b => b.CiExternalId)
                .ToList();

            ConnectionsManager connManager = new ConnectionsManager();
            await connManager.AddBuildConfigs(connectionId, builds);
            await connManager.AddBuildConfigs(otherConnectionId, otherBuilds);

            await connManager.RemoveAllBuildConfigs(connectionId);

            connManager.BuildsPerConnId.Keys.Count.Should().Be(1);
            connManager.BuildsToBeRefreshed.Should().ContainKeys(otherBuildsIds.ToArray());
            connManager.BuildsToBeRefreshed.Should().ContainKey(duplicateBuildId);
        }

        [Test]
        public async Task RemoveBuildConfig_ShouldRemoveTheBuild()
        {
            string connectionId = _fixture.Create<string>();
            List<BuildConfig> builds = _fixture
                .Build<BuildConfig>()
                .CreateMany()
                .ToList();

            ConnectionsManager connManager = new ConnectionsManager();
            await connManager.AddBuildConfigs(connectionId, builds);

            connManager.BuildsPerConnId.Keys.Should()
                .Contain(connectionId);

            await connManager.RemoveBuildConfig(connectionId, builds.First());

            connManager.BuildsPerConnId.Keys.Should().Contain(connectionId);
            connManager.BuildsPerConnId[connectionId].Count.Should()
                .Be(builds.Count() - 1);

            connManager.BuildsToBeRefreshed.Keys.Count.Should()
                .Be(builds.Count() - 1);
        }

        [Test]
        public async Task RemoveBuildConfig_WhenOtherConnectionIdsAreUsingIt_ShouldNotRemoveTheBuild()
        {
            BuildConfig duplicateBuild = _fixture.Create<BuildConfig>();

            string connectionId = _fixture.Create<string>();
            List<BuildConfig> builds = _fixture
                .Build<BuildConfig>()
                .CreateMany()
                .ToList();
            builds.Add(duplicateBuild);

            string otherConnectionId = _fixture.Create<string>();
            List<BuildConfig> otherBuilds = _fixture
                .Build<BuildConfig>()
                .CreateMany()
                .ToList();
            otherBuilds.Add(duplicateBuild);

            ConnectionsManager connManager = new ConnectionsManager();
            await connManager.AddBuildConfigs(connectionId, builds);
            await connManager.AddBuildConfigs(otherConnectionId, otherBuilds);

            BuildConfig removeBuild = builds.First();
            await connManager.RemoveBuildConfig(connectionId, removeBuild);

            connManager.BuildsPerConnId.Keys.Count.Should().Be(2);
            connManager.BuildsToBeRefreshed.Should().NotContainKey(removeBuild.CiExternalId);
        }
    }
}
