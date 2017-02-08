using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CIDashboard.Web.Application.Interfaces;
using CIDashboard.Web.Hubs;
using CIDashboard.Web.Models;
using Microsoft.AspNet.SignalR;
using Serilog;

namespace CIDashboard.Web.Application
{
    public class RefreshInformation : IRefreshInformation
    {
        private static readonly ILogger Logger = Log.ForContext<RefreshInformation>();

        public IInformationQuery InfoQuery { get; set; }

        public IConnectionsManager ConnectionsManager { get; set; }

        public async Task SendRefreshBuildResults(string connectionId)
        {
            try
            {
                IHubContext hubContext = GlobalHost.ConnectionManager.GetHubContext<CiDashboardHub>();
                
                ICollection<string> connectionIdToRefresh = string.IsNullOrEmpty(connectionId)
                    ? ConnectionsManager.BuildsPerConnId.Keys
                    : new List<string> { connectionId };

                Parallel.ForEach(connectionIdToRefresh,
                    connId =>
                    {
                        Logger.Debug("Refreshing build result for {connectionId}", connId);
                        hubContext.Clients.Client(connId)
                            .SendMessage(new FeedbackMessage { Status = "Info", Message = "Your builds are being refreshed" });
                        hubContext.Clients.Client(connId).StartRefresh(new RefreshStatus { Status = "start" });
                    });

                ICollection<string> buildsToRefresh = string.IsNullOrEmpty(connectionId)
                    ? ConnectionsManager.BuildsToBeRefreshed.Keys
                    : ConnectionsManager.BuildsPerConnId[connectionId];
                List<Task> buildsToRetrieve = buildsToRefresh
                    .Select(buildId => GetLastBuildResult(hubContext, connectionId, buildId))
                    .ToList();

                await Task.WhenAll(buildsToRetrieve);
                hubContext.Clients.All.StopRefresh(new RefreshStatus { Status = "stop" });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error refreshing builds...");
            }
        }

        // only needed because only Hangfire pro supports async calls
        public void SendRefreshBuildResultsSync()
        {
            SendRefreshBuildResults(null).Wait();
        }

        private async Task GetLastBuildResult(IHubContext hubContext, string connectionId, string buildId)
        {
            try
            {
                Build lastBuildResult = await InfoQuery.GetLastBuildResult(buildId);

                IEnumerable<string> connIds = string.IsNullOrEmpty(connectionId)
                    ? ConnectionsManager.BuildsPerConnId.Where(b => b.Value.Contains(lastBuildResult.CiExternalId)).Select(d => d.Key)
                    : new List<string> { connectionId };

                foreach (string connId in connIds)
                {
                    Logger.Debug(
                        "Sending build result for {buildId} to {connectionId}",
                        lastBuildResult.CiExternalId,
                        connId);
                    hubContext.Clients.Client(connId)
                        .SendBuildResult(lastBuildResult);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error getting last build result for {buildId}...", buildId);
            }
        }
    }
}