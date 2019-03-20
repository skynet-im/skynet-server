using FcmSharp.Requests;
using FcmSharp.Settings;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FcmSharpClient = FcmSharp.FcmClient;

namespace SkynetServer.Network.Fcm
{
    internal class FcmClient : IDisposable
    {
        private readonly FcmSharpClient client;

        public FcmClient()
        {
            FcmClientSettings settings = FileBasedFcmClientSettings.CreateFromFile("firebase-service-account.json");
            client = new FcmSharpClient(settings);
        }

        public Task SendAsync(string token)
        {
            FcmMessage message = new FcmMessage()
            {
                Message = new Message
                {
                    AndroidConfig = new AndroidConfig
                    {
                        Priority = AndroidMessagePriorityEnum.HIGH
                    },
                    Token = token
                }
            };

            return client.SendAsync(message);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    client.Dispose();
                }

                disposedValue = true;
            }
        }

        // ~FcmClient() {
        //   Dispose(false);
        // }

        public void Dispose()
        {
            Dispose(true);
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
