using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIDashboard.Web.Application.Interfaces;
using CIDashboard.Web.Models;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Serilog;

namespace CIDashboard.Web.Hubs
{
    [Authorize]
    [HubName("ciDashboardHub")]
    public class CiDashboardHub : Hub<ICiDashboardHub>
    {
        private static readonly ILogger Logger = Log.ForContext<CiDashboardHub>();
        private readonly IConnectionsManager _connectionsManager;
        private readonly IInformationQuery _infoQuery;
        private readonly IRefreshInformation _refreshInfo;
        private readonly ICommandProcessor _commandProcessor;

        public CiDashboardHub(IConnectionsManager connectionsManager, ICommandProcessor commandProcessor, IInformationQuery infoQuery, IRefreshInformation refreshInfo)
        {
            _connectionsManager = connectionsManager;
            _commandProcessor = commandProcessor;
            _infoQuery = infoQuery;
            _refreshInfo = refreshInfo;
        }

        public override async Task OnConnected()
        {
            string username = Context.User.Identity.Name;
            string connectionId = Context.ConnectionId;

            Logger.Debug("OnConnected for {username} and {connectionId}", username, connectionId);

            await GetUserProjectsAndBuildsAndSendTheInfoToClient(username, connectionId);
            await _refreshInfo.SendRefreshBuildResults(connectionId);

            await base.OnConnected();
        }

        public override async Task OnDisconnected(bool stopCalled)
        {
            string connectionId = Context.ConnectionId;
            Logger.Debug("OnDisconnected for {connectionId}", connectionId);

            await _connectionsManager.RemoveAllBuildConfigs(connectionId);

            await base.OnDisconnected(stopCalled);
        }

        public override async Task OnReconnected()
        {
            string username = Context.User.Identity.Name;
            string connectionId = Context.ConnectionId;

            Logger.Debug("OnReconnected for {username} and {connectionId}", username, connectionId);

            await GetUserProjectsAndBuildsAndSendTheInfoToClient(username, connectionId);

            await base.OnReconnected();
        }

        public async Task RequestRefresh()
        {
            string connectionId = Context.ConnectionId;
            await _refreshInfo.SendRefreshBuildResults(connectionId);
        }

        public async Task RequestAllProjectBuilds()
        {
            string connectionId = Context.ConnectionId;
            await RequestAllProjectBuilds(connectionId);
        }

        public async Task AddNewProject(Project project)
        {
            string username = Context.User.Identity.Name;
            string connectionId = Context.ConnectionId;
            Project projectCreated = await _commandProcessor.AddNewProject(username, project);

            if (projectCreated != null)
                Clients.Client(connectionId)
                    .SendUpdatedProject(new ProjectUpdated { OldId = project.Id, Project = projectCreated });
        }

        public async Task UpdateProjectName(int projectId, string projectName)
        {
            string connectionId = Context.ConnectionId;
            bool updated = await _commandProcessor.UpdateProjectName(projectId, projectName);
            if (updated)
                await SendSuccessMessage(connectionId, string.Format("Project {0} updated.", projectName));
        }

        public async Task RemoveProject(int projectId)
        {
            string connectionId = Context.ConnectionId;
            Project projectRemoved = await _commandProcessor.RemoveProject(projectId);
            if(projectRemoved != null)
            {
                foreach(BuildConfig buildConfig in projectRemoved.Builds)
                {
                    await _connectionsManager.RemoveBuildConfig(connectionId, buildConfig);
                }
                await SendSuccessMessage(connectionId, "Project removed");
            }
        }

        public async Task AddBuildToProject(int projectId, BuildConfig build)
        {
            string connectionId = Context.ConnectionId;
            BuildConfig buildCreated = await _commandProcessor.AddBuildConfigToProject(projectId, build);
            if (buildCreated != null)
            {
                await _connectionsManager.UpdateBuildConfigs(connectionId, new List<BuildConfig> { buildCreated });
                Clients.Client(connectionId)
                    .SendUpdatedBuild(new BuildConfigUpdated { OldId = build.Id, Build = buildCreated });
            }
        }

        public async Task RemoveBuild(int buildId)
        {
            string connectionId = Context.ConnectionId;
            BuildConfig buildConfigRemoved = await _commandProcessor.RemoveBuildConfig(buildId);
            if (buildConfigRemoved != null)
            {
                await _connectionsManager.RemoveBuildConfig(connectionId, buildConfigRemoved);
                await SendSuccessMessage(connectionId, "Build removed");
            }
        }

        public async Task UpdateBuildConfigExternalId(int buildId, string buildName, string externalId)
        {
            string connectionId = Context.ConnectionId;
            bool updated = await _commandProcessor.UpdateBuildConfigExternalId(buildId, buildName, externalId);
            if(updated)
            {
                await _connectionsManager.UpdateBuildConfigs(connectionId, new[] { new BuildConfig { CiExternalId = externalId } });
                await SendSuccessMessage(connectionId, $"Build {buildName} updated.");
            }
        }

        private async Task GetUserProjectsAndBuildsAndSendTheInfoToClient(string username, string connectionId)
        {
            IEnumerable<Project> userProjects = await _infoQuery.GetUserProjectsAndBuildConfigs(username);
            IEnumerable<Project> enumerable = userProjects as Project[] ?? userProjects.ToArray();
            List<BuildConfig> buildCiIds = enumerable
                .SelectMany(p => p.Builds
                    .Where(b => !string.IsNullOrEmpty(b.CiExternalId))
                    .ToList())
                .ToList();

            await _connectionsManager.AddBuildConfigs(connectionId, buildCiIds);

            Logger.Debug("Start retrieving builds for {user} and {connectionId}", username, connectionId);

            Clients.Client(connectionId).SendProjectsAndBuildConfigs(enumerable);
            await SendInfoMessage(connectionId, "Your builds are being retrieved");
        }

        private async Task RequestAllProjectBuilds(string connectionId)
        {
            try
            {
                IEnumerable<BuildConfig> allProjectBuilds = await _infoQuery.GetAllProjectBuildConfigs();

                Clients.Client(connectionId).SendProjectBuilds(allProjectBuilds);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error requesting all project builds...");
            }
        }

        private async Task SendInfoMessage(string connectionId, string message)
        {
            await Task.Run(() => Clients.Client(connectionId).SendMessage(new FeedbackMessage { Status = "Info", Message = message }));
        }

        private async Task SendSuccessMessage(string connectionId, string message)
        {
            await Task.Run(() => Clients.Client(connectionId).SendMessage(new FeedbackMessage { Status = "Success", Message = message }));
        }

        // ReSharper disable once UnusedMember.Local
        private async Task SendErrorMessage(string connectionId, string message)
        {
            await Task.Run(() => Clients.Client(connectionId).SendMessage(new FeedbackMessage { Status = "Error", Message = message }));
        }
    }
}