using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skynet.Server.Web.Models
{
    public class ConfirmationResult
    {
        public string Title { get; set; }
        public string Header { get; set; }
        public string Content { get; set; }
    }
}
