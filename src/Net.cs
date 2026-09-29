using System;
using System.Net.Http;

namespace RcmGui
{
    internal static class Net
    {
        public static readonly HttpClient Client = CreateClient();

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("RcmGui/1.0"); // GitHub-API verlangt einen User-Agent
            return client;
        }
    }
}
