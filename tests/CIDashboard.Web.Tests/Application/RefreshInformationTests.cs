﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIDashboard.Data.Entities;
using CIDashboard.Web.Application;
using CIDashboard.Web.Application.Interfaces;
using CIDashboard.Web.MappingProfiles;
using FakeItEasy;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoFakeItEasy;
// ReSharper disable ObjectCreationAsStatement

namespace CIDashboard.Web.Tests.Application
{
    [TestFixture]
    public class RefreshInformationTests
    {
        private IFixture _fixture;
        private IConnectionsManager _connectionsManager;
        private IInformationQuery _infoQuery;

        [SetUp]
        public void Setup()
        {
            new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ViewModelProfilers>();
            });
            _fixture = new Fixture()
                .Customize(new AutoFakeItEasyCustomization());

            _connectionsManager = A.Fake<IConnectionsManager>();
            _infoQuery = A.Fake<IInformationQuery>();
        }

        [Test]
        public async Task SendRefreshBuildResults_WithNullConnectionId_QueriesGetLastBuildResultForAllBuilds()
        {
            IEnumerable<BuildConfig> buildsProj1 = _fixture
                .Build<BuildConfig>()
                .Without(p => p.Project)
                .CreateMany();
            List<string> buildsIds1 = buildsProj1.Select(b => b.CiExternalId)
                .ToList();
            IEnumerable<BuildConfig> buildsProj2 = _fixture
                .Build<BuildConfig>()
                .Without(p => p.Project)
                .CreateMany();
            List<string> buildsIds2 = buildsProj2.Select(b => b.CiExternalId)
                .ToList();

            List<string> buildsIds = new List<string>();
            buildsIds.AddRange(buildsIds1);
            buildsIds.AddRange(buildsIds2);

            RefreshInformation refreshInfo = new RefreshInformation
            {
                ConnectionsManager = _connectionsManager,
                InfoQuery = _infoQuery
            };

            ConcurrentDictionary<string, List<string>> buildsPerConnId = new ConcurrentDictionary<string, List<string>>();
            buildsPerConnId.AddOrUpdate(_fixture.Create<string>(), buildsIds1, (oldkey, oldvalue) => buildsIds1);
            buildsPerConnId.AddOrUpdate(_fixture.Create<string>(), buildsIds2, (oldkey, oldvalue) => buildsIds1);
            A.CallTo(() => _connectionsManager.BuildsPerConnId)
                .Returns(buildsPerConnId);

            ConcurrentDictionary<string, string> buildsToBeRefreshed = new ConcurrentDictionary<string, string>();
            Parallel.ForEach(buildsIds, build => buildsToBeRefreshed.TryAdd(build, build));
            A.CallTo(() => _connectionsManager.BuildsToBeRefreshed)
                .Returns(buildsToBeRefreshed);

            await refreshInfo.SendRefreshBuildResults(null);

            foreach (string buildsId in buildsIds)
            {
                A.CallTo(() => _infoQuery.GetLastBuildResult(buildsId))
                    .MustHaveHappened();
            }
        }

        [Test]
        public async Task SendRefreshBuildResults_WithConnectionId_QueriesGetLastBuildResult_OnlyForSpecificConnectionIdBuilds()
        {
            IEnumerable<BuildConfig> buildsProj1 = _fixture
                .Build<BuildConfig>()
                .Without(p => p.Project)
                .CreateMany();
            List<string> buildsIds1 = buildsProj1.Select(b => b.CiExternalId)
                .ToList();
            IEnumerable<BuildConfig> buildsProj2 = _fixture
                 .Build<BuildConfig>()
                 .Without(p => p.Project)
                 .CreateMany();
            List<string> buildsIds2 = buildsProj2.Select(b => b.CiExternalId)
                .ToList();

            List<string> buildsIds = new List<string>();
            buildsIds.AddRange(buildsIds1);
            buildsIds.AddRange(buildsIds2);

            string connectionId = _fixture.Create<string>();

            RefreshInformation refreshInfo = new RefreshInformation
            {
                ConnectionsManager = _connectionsManager,
                InfoQuery = _infoQuery
            };

            ConcurrentDictionary<string, List<string>> buildsPerConnId = new ConcurrentDictionary<string, List<string>>();
            buildsPerConnId.AddOrUpdate(connectionId, buildsIds1, (oldkey, oldvalue) => buildsIds1);
            buildsPerConnId.AddOrUpdate(_fixture.Create<string>(), buildsIds2, (oldkey, oldvalue) => buildsIds1);
            A.CallTo(() => _connectionsManager.BuildsPerConnId)
                .Returns(buildsPerConnId);

            ConcurrentDictionary<string, string> buildsToBeRefreshed = new ConcurrentDictionary<string, string>();
            Parallel.ForEach(buildsIds, build => buildsToBeRefreshed.TryAdd(build, build));
            A.CallTo(() => _connectionsManager.BuildsToBeRefreshed)
                .Returns(buildsToBeRefreshed);

            await refreshInfo.SendRefreshBuildResults(connectionId);

            foreach (string buildsId in buildsIds1)
            {
                A.CallTo(() => _infoQuery.GetLastBuildResult(buildsId))
                    .MustHaveHappened();
            }

            foreach (string buildsId in buildsIds2)
            {
                A.CallTo(() => _infoQuery.GetLastBuildResult(buildsId))
                    .MustNotHaveHappened();
            }
        }
    }
}
