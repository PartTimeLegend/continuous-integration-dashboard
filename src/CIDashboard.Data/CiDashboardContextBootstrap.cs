using System.Linq;
using CIDashboard.Data.Interfaces;

namespace CIDashboard.Data
{
    public class CiDashboardContextBootstrap : ICiDashboardContextBootstrap
    {
        private readonly ICiDashboardContextFactory _factory;

        public CiDashboardContextBootstrap(ICiDashboardContextFactory factory)
        {
            _factory = factory;
        }

        public void InitiateDatabase()
        {
            using (ICiDashboardContext ctx = _factory.Create())
            {
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                ctx.Projects.FirstOrDefault();
            }
        }
    }
}
