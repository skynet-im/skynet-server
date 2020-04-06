using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Skynet.Server.Services
{
    internal interface IFirebaseService
    {
        Task<string> SendAsync(string token);
    }
}
