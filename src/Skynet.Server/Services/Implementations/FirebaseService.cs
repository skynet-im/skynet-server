using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Skynet.Server.Services.Implementations
{
    internal sealed class FirebaseService : IFirebaseService
    {
        private readonly FirebaseMessaging messaging;

        public FirebaseService(IOptions<Configuration.FcmOptions> options)
        {
            string path = options.Value.ServiceAccountFilePath;
            if (!string.IsNullOrWhiteSpace(path))
            {
                FirebaseApp app = FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(path)
                });
                messaging = FirebaseMessaging.GetMessaging(app);
            }
        }

        public Task<string> SendAsync(string token)
        {
            return messaging?.SendAsync(new Message
            {
                Android = new AndroidConfig
                {
                    Priority = Priority.High
                },
                Data = new Dictionary<string, string> { { "Action", "FetchMessages" } },
                Token = token
            }) ?? Task.FromResult(string.Empty);
        }
    }
}
