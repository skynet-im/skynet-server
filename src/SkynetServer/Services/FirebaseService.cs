using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Services
{
    internal class FirebaseService
    {
        private readonly FirebaseMessaging messaging;

        public FirebaseService()
        {
            FirebaseApp app = FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile("firebase-service-account.json")
            });
            messaging = FirebaseMessaging.GetMessaging(app);
        }

        public Task<string> SendAsync(string token)
        {
            return messaging.SendAsync(new Message
            {
                Android = new AndroidConfig
                {
                    Priority = Priority.High
                },
                Data = new Dictionary<string, string> { { "Action", "FetchMessages" } },
                Token = token
            });
        }
    }
}
