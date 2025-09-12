using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication5.Models
{
    public class HODViewRequestsViewModel
    {
        public List<Request> PendingRequests { get; set; }
        public List<Request> OngoingRequests { get; set; }
        public List<Request> ApprovedRequests { get; set; }
        public List<Request> RejectedRequests { get; set; }
    }
}