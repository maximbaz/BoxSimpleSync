using System;
using System.Net;

namespace BoxSimpleSync.API
{
    public static class HttpRequest
    {
        private const string ApiKey = "eu3dj9zermgofty4fi52qq1t0gfy0tih";
        
        public static void Get(string url, DownloadStringCompletedEventHandler requestCompleted)
        {
            var client = new WebClient();
            client.DownloadStringAsync(new Uri(url + "&api_key=" + ApiKey));
            client.DownloadStringCompleted += requestCompleted;
        }

        public static void Post(string url, string data, UploadStringCompletedEventHandler requestCompleted) {
            var client = CreateWebClient();
            client.Headers.Add("Content-Type:application/x-www-form-urlencoded");
            client.UploadStringAsync(new Uri(url), "POST", data);
            client.UploadStringCompleted += requestCompleted;
        }

        public static void Post(string url, string data, string authToken, UploadStringCompletedEventHandler requestCompleted) {
            var client = CreateWebClient();
            client.Headers.Add("Content-Type:application/x-www-form-urlencoded");
            client.Headers.Add(string.Format("Authorization: BoxAuth api_key={0}&auth_token={1}", ApiKey, authToken));
            client.UploadStringAsync(new Uri(url), "POST", data);
            client.UploadStringCompleted += requestCompleted;
        }

        private static WebClient CreateWebClient() {
            var client = new WebClient();
            client.Headers.Add("Host:www.box.net");
            return client;
        }
    }
}