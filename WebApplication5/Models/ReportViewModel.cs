using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class ReportViewModel
    {
        public List<MaterialIssueReportViewModel> DetailedReports { get; set; }
        public List<MaterialSummaryViewModel> SummaryReports { get; set; }
    }
}