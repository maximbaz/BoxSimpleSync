using System;
using System.Net;
using System.Threading.Tasks;

namespace BoxSimpleSync.API
{
    public static class HttpRequest
    {
        #region Static Fields and Constants

        private const string ApiKey = "eu3dj9zermgofty4fi52qq1t0gfy0tih";

        #endregion

        #region Public Methods

        public static Task<string> Get(string url, string authToken) {
            var client = new WebClient();
            if (authToken == null)
                return client.DownloadStringTaskAsync(new Uri(url + "&api_key=" + ApiKey));
            client.Headers.Add(AuthHeader(authToken));
            return client.DownloadStringTaskAsync(url);
        }

        public static Task<string> Post(string url, string data, string authToken) {
            var client = new WebClient();
            client.Headers.Add("Host:www.box.net");
            client.Headers.Add("Content-Type:application/x-www-form-urlencoded");
            if (authToken != null)
                client.Headers.Add(AuthHeader(authToken));
            return client.UploadStringTaskAsync(new Uri(url), "POST", data);
        }

        public static Task DownloadFile(string url, string location, string authToken) {
            var client = new WebClient();
            client.Headers.Add("Host:www.box.net");
            client.Headers.Add(AuthHeader(authToken));
            return client.DownloadFileTaskAsync(new Uri(url), location);
        }

        #endregion

        #region Protected And Private Methods

        private static string AuthHeader(string authToken) {
            return string.Format("Authorization: BoxAuth api_key={0}&auth_token={1}", ApiKey, authToken);
        }

        #endregion
    }
}