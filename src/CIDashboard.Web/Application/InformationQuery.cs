using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using CIDashboard.Data.Interfaces;
using CIDashboard.Domain.Entities;
using CIDashboard.Domain.Services;
using CIDashboard.Web.Application.Interfaces;
using CIDashboard.Web.Models;
using Serilog;

namespace CIDashboard.Web.Application
{
    public class InformationQuery : IInformationQuery
    {
        private static readonly ILogger Logger = Log.ForContext<InformationQuery>();

        public ICiDashboardService CiDashboardService { get; set; }
    
        public ICiServerService CiServerService { get; set; }

        public async Task<IEnumerable<Project>> GetUserProjectsAndBuildConfigs(string username)
        {
            try
            {
                IEnumerable<Data.Entities.Project> userProjects = await CiDashboardService.GetProjects(username);
                IEnumerable<Project> mappedUserProjects = Mapper.Map<IEnumerable<Data.Entities.Project>, IEnumerable<Project>>(userProjects);

                return mappedUserProjects;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error requesting all project for {username}", username);
            }

            return new List<Project>();
        }

        public async Task<IEnumerable<BuildConfig>> GetAllProjectBuildConfigs()
        {
            try
            {
                IEnumerable<CiBuildConfig> allProjectBuilds = await CiServerService.GetAllBuildConfigs();
                IEnumerable<BuildConfig> mappedBuilds = Mapper.Map<IEnumerable<CiBuildConfig>, IEnumerable<BuildConfig>>(allProjectBuilds);

                return mappedBuilds;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error requesting all project builds...");
            }

            return new List<BuildConfig>();
        }

        public async Task<Build> GetLastBuildResult(string buildId)
        {
            try
            {
                CiBuildResult lastBuildResult = await CiServerService.LastBuildResult(buildId);
                Build mappedBuild = Mapper.Map<CiBuildResult, Build>(lastBuildResult);

                return mappedBuild;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error getting last build result for {buildId}...", buildId);
            }

            return null;
        }
    }
}