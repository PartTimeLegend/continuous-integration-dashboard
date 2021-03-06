﻿using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using CIDashboard.Data.Entities;
using CIDashboard.Data.Interfaces;

namespace CIDashboard.Data
{
    public class CiDashboardService : ICiDashboardService
    {
        public ICiDashboardContextFactory CtxFactory { get; set; }

        public async Task<IEnumerable<Project>> GetProjects(string username)
        {
            using (ICiDashboardContext context = CtxFactory.Create())
            {
                return await context.Projects
                    .Where(p => p.User == username)
                    .Include(p => p.BuildConfigs)
                    .OrderBy(p => p.Order)
                    .ToListAsync();
            }
        }

        public Task<Project> AddProject(string username, Project project)
        {
            return Task.Run(
                () =>
                {
                    using(ICiDashboardContext context = CtxFactory.Create())
                    {
                        project.User = username;
                        context.Projects.Add(project);

                        context.SaveChanges();
                    }

                    return project;
                });
        }

        public async Task<bool> UpdateProjectName(int projectId, string projectName)
        {
            using (ICiDashboardContext context = CtxFactory.Create())
            {
                Project project = await context.Projects
                    .Where(p => p.Id == projectId)
                    .FirstOrDefaultAsync();

                if(project == null) 
                    return false;

                project.Name = projectName;
                context.SaveChanges();
                return true;
            }
        }

        public async Task<bool> UpdateProjectOrder(int projectId, int position)
        {
            using (ICiDashboardContext context = CtxFactory.Create())
            {
                Project project = await context.Projects
                    .Where(p => p.Id == projectId)
                    .FirstOrDefaultAsync();

                if (project == null)
                    return false;

                project.Order = position;
                context.SaveChanges();
                return true;
            }
        }

        public async Task<Project> RemoveProject(int projectId)
        {
            using (ICiDashboardContext context = CtxFactory.Create())
            {
                Project project = await context.Projects
                    .Where(p => p.Id == projectId)
                    .FirstOrDefaultAsync();

                if (project == null)
                    return null;

                context.Projects.Remove(project);
                context.SaveChanges();
                return project;
            }
        }

        public async Task<BuildConfig> AddBuildConfigToProject(int projectId, BuildConfig buildConfig)
        {
            using (ICiDashboardContext context = CtxFactory.Create())
            {
                Project project = await context.Projects
                    .Where(p => p.Id == projectId)
                    .FirstOrDefaultAsync();

                if (project == null)
                    return null;

                buildConfig.ProjectId = projectId;
                context.BuildConfigs.Add(buildConfig);

                context.SaveChanges();
            }

            return buildConfig;
        }

        public async Task<BuildConfig> RemoveBuildConfig(int buildId)
        {
            using (ICiDashboardContext context = CtxFactory.Create())
            {
                BuildConfig build = await context.BuildConfigs
                    .Where(p => p.Id == buildId)
                    .FirstOrDefaultAsync();

                if (build == null)
                    return null;

                context.BuildConfigs.Remove(build);
                context.SaveChanges();
                return build;
            }
        }

        public async Task<bool> UpdateBuildConfigExternalId(int buildId, string buildName, string externalId)
        {
            using (ICiDashboardContext context = CtxFactory.Create())
            {
                BuildConfig build = await context.BuildConfigs
                    .Where(p => p.Id == buildId)
                    .FirstOrDefaultAsync();

                if (build == null)
                    return false;

                build.Name = buildName;
                build.CiExternalId = externalId;
                context.SaveChanges();
                return true;
            }
        }

        public async Task<bool> UpdateBuildConfigOrder(int buildId, int position)
        {
            using (ICiDashboardContext context = CtxFactory.Create())
            {
                BuildConfig build = await context.BuildConfigs
                    .Where(p => p.Id == buildId)
                    .FirstOrDefaultAsync();

                if (build == null)
                    return false;

                build.Order = position;
                context.SaveChanges();
                return true;
            }
        }
    }
}
