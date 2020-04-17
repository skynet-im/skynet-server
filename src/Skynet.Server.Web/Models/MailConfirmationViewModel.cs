using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skynet.Server.Web.Models
{
    public class MailConfirmationViewModel
    {
        public MailConfirmationViewModel(string mailAddress, string token)
        {
            MailAddress = mailAddress;
            Token = token;
        }

        public string MailAddress { get; }
        public string Token { get; }
    }
}
