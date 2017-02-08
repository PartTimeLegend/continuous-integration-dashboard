using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CIDashboard.Domain.Entities;
using Serilog;
using TeamCitySharp;
using TeamCitySharp.DomainEntities;
using TeamCitySharp.Locators;

namespace CIDashboard.Domain.Services
{
    public class TeamCityService : ICiServerService
    {
        private static readonly ILogger Logger = Log.ForContext<TeamCityService>();

        private readonly ITeamCityClient _client;

        public TeamCityService(ITeamCityClient client)
        {
            Logger.Debug("Connecting to TeamCity as guest");
            _client = client;
            _client.ConnectAsGuest();
        }
        
        public TeamCityService(ITeamCityClient client, string username, string password)
        {
            Logger.Debug("Connecting to TeamCity with username {username}", username);
            _client = client; 
            _client.Connect(username, password);
        }

        public CiSource Source
        {
            get
            {
                return CiSource.TeamCity;
            }
        }

        public async Task<IEnumerable<CiBuildConfig>> GetAllBuildConfigs()
        {
            Logger.Debug("Retrieving from TeamCity all BuildConfigs");
            List<BuildConfig> buildConfigs = await Task.Run(() => _client.BuildConfigs.All());

            IEnumerable<string> projectsIds = buildConfigs.Select(b => b.ProjectId).Distinct();
            List<Task<Project>> projectsToRetrieve = projectsIds
                .Select(GetProjectDetails)
                .ToList();

            Project[] projects = await Task.WhenAll(projectsToRetrieve);
            foreach(Project project in projects.Where(project => project.Archived)) 
            {
                buildConfigs.RemoveAll(b => b.ProjectId == project.Id);
            }

            IEnumerable<CiBuildConfig> mappedBuilds = Mapper.Map<IEnumerable<BuildConfig>, IEnumerable<CiBuildConfig>>(buildConfigs);

            Logger.Debug("Retrieved from TeamCity all BuildConfigs");
            return mappedBuilds;
        }

        public async Task<CiBuildResult> LastBuildResult(string buildId)
        {
            // get last run build
            Logger.Debug("Retrieving from TeamCity last build for {buildId}", buildId);
            Build build = await GetLastBuild(buildId);
            if(build == null)
            {
                return null;
            }

            // get build run details
            Logger.Debug("Retrieving from TeamCity build details for {buildId}", build.Id);
            Build buildDetails = await GetBuildDetails(build);

            CiBuildResult mappedBuild = Mapper.Map<Build, CiBuildResult>(buildDetails);

            // get build run statistics
            Logger.Debug("Retrieving from TeamCity build statistics for {buildId}", build.Id);
            List<Property> buildStats = await GetBuildStatistics(build);
            if (buildStats != null)
            {
                Dictionary<string, string> dict = buildStats.ToDictionary(item => item.Name, item => item.Value);
                int value;
                if (dict.ContainsKey("PassedTestCount") && int.TryParse(dict["PassedTestCount"], out value))
                    mappedBuild.NumberTestPassed = value;
                if (dict.ContainsKey("FailedTestCount") && int.TryParse(dict["FailedTestCount"], out value))
                    mappedBuild.NumberTestFailed = value;
                if (dict.ContainsKey("IgnoredTestCount") && int.TryParse(dict["IgnoredTestCount"], out value))
                    mappedBuild.NumberTestIgnored = value;

                if (dict.ContainsKey("CodeCoverageAbsSCovered") && int.TryParse(dict["CodeCoverageAbsSCovered"], out value))
                    mappedBuild.NumberStatementsCovered = value;
                if (dict.ContainsKey("CodeCoverageAbsSTotal") && int.TryParse(dict["CodeCoverageAbsSTotal"], out value))
                    mappedBuild.NumberStatementsTotal = value;
            }

            // check if a build for it is running
            if (await IsRunningABuild(buildId))
                mappedBuild.Status = CiBuildResultStatus.Running;

            // TODO: check if a build for it is in queued
            return mappedBuild;
        }

        private async Task<List<Property>> GetBuildStatistics(Build build)
        {
            List<Property> buildStats = await Task.Run(
                () =>
                {
                    try
                    {
                        return _client.Statistics.GetByBuildId(build.Id);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Error retrieving from TeamCity build statistics for {build.Id}", build.Id);
                    }

                    return null;
                });
            return buildStats;
        }

        private async Task<Build> GetBuildDetails(Build build)
        {
            Build buildDetails = await Task.Run(
                () =>
                {
                    try
                    {
                        return _client.Builds.ByBuildId(build.Id);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Error retrieving from TeamCity build details for {build.Id}", build.Id);
                    }

                    return null;
                });
            return buildDetails;
        }

        private async Task<Build> GetLastBuild(string buildId)
        {
            Build build = await Task.Run(
                () =>
                {
                    try
                    {
                        return _client.Builds.LastBuildByBuildConfigId(buildId);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Error retrieving from TeamCity last build for {buildId}", buildId);
                    }

                    return null;
                });
            return build;
        }

        private async Task<bool> IsRunningABuild(string buildId)
        {
            Logger.Debug("Retrieving from TeamCity if {buildId} is running", buildId);
            return await Task.Run(() =>
            {
                try
                {
                    return _client
                        .Builds
                        .ByBuildLocator(BuildLocator.WithDimensions(BuildTypeLocator.WithId(buildId), running: true))
                        .Count > 0;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error retrieving from TeamCity if {buildId} is running", buildId);
                }

                return false;
            });
        }

        private async Task<Project> GetProjectDetails(string projectId)
        {
            Project projectDetails = await Task.Run(
                () =>
                {
                    try
                    {
                        return _client.Projects.ById(projectId);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Error retrieving from TeamCity project details for {projectId}", projectId);
                    }

                    return null;
                });
            return projectDetails;
        }

    }
}
