using System;
using System.ComponentModel;

namespace CIDashboard.Domain.Entities
{
    public enum CiBuildResultStatus
    {
        [Description("")]
        Unknown = 0,

        [Description("Success")]
        Success = 1,

        [Description("Failure")]
        Failure = 2,

        [Description("Building")]
        Running = 3
    }

    public class CiBuildResult
    {
        public CiSource CiSource { get; set; }

        public string Id { get; set; }

        public string BuildId { get; set; }

        public string BuildName { get; set; }

        public string Version { get; set; }

        public CiBuildResultStatus Status { get; set; }

        public string Url { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime FinishDate { get; set; }

        public int NumberTestPassed { get; set; } //PassedTestCount

        public int NumberTestIgnored { get; set; } //IgnoredTestCount

        public int NumberTestFailed { get; set; } //FailedTestCount

        public int NumberTestTotal
        {
            get
            {
                return NumberTestPassed + NumberTestFailed + NumberTestIgnored;
            }
        }

        public int NumberStatementsCovered { get; set; } //CodeCoverageAbsSCovered

        public int NumberStatementsTotal { get; set; } //CodeCoverageAbsSTotal

        public double CodeCoverage
        {
            get
            {
                return NumberStatementsTotal == 0
                    ? 0
                    : Math.Round(
                        (NumberStatementsCovered / (double)NumberStatementsTotal) * 100,
                        2);
            }
        }
    }
}
