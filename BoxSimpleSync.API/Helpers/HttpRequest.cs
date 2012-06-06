using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace BoxSimpleSync.API.Helpers
{
    public static class HttpRequest
    {
        #region Static Fields and Constants

        private const string ApiKey = "eu3dj9zermgofty4fi52qq1t0gfy0tih";

        #endregion

        #region Public and Internal Methods

        public static Task<string> Get(string url, string authToken) {
            var client = new WebClient();
            if (authToken == null)
                return client.DownloadStringTaskAsync(new Uri(url + "&api_key=" + ApiKey));
            client.Headers.Add(AuthHeader(authToken));
            return client.DownloadStringTaskAsync(url);
        }

        public static Task<string> Post(string url, string data, string authToken) {
            var client = new WebClient();
            client.Headers.Add("Content-Type:application/x-www-form-urlencoded");
            client.Headers.Add(authToken != null ? AuthHeader(authToken) : "Host:www.box.net");
            return client.UploadStringTaskAsync(new Uri(url), "POST", data);
        }

        public static Task DownloadFile(string url, string location, string authToken) {
            var client = new WebClient();
            client.Headers.Add(AuthHeader(authToken));
            return client.DownloadFileTaskAsync(new Uri(url), location);
        }

        public static async Task<string> UploadFiles(string url, string boundary, byte[] buffer, string authToken) {
            var request = CreateUploadRequest(url, boundary, buffer.Length, authToken);

            using (var requestStream = await request.GetRequestStreamAsync()) {
                await requestStream.WriteAsync(buffer, 0, buffer.Length);
                await requestStream.FlushAsync();

                using (var response = await request.GetResponseAsync()) {
                    using (var responseStream = response.GetResponseStream()) {
                        if (responseStream == null)
                            throw new NullReferenceException("responseStream");

                        using (var responseReader = new StreamReader(responseStream)) {
                            return await responseReader.ReadToEndAsync();
                        }
                    }
                }
            }
        }

        public static async Task Delete(string url, string authToken) {
            var request = (HttpWebRequest) WebRequest.Create(url);
            request.Method = "DELETE";
            request.Headers.Add(AuthHeader(authToken));
            await request.GetResponseAsync();
        }

        #endregion

        #region Protected And Private Methods

        private static string AuthHeader(string authToken) {
            return string.Format("Authorization: BoxAuth api_key={0}&auth_token={1}", ApiKey, authToken);
        }

        private static HttpWebRequest CreateUploadRequest(string url, string boundary, long contentLength, string authToken) {
            var webRequest = (HttpWebRequest) WebRequest.Create(url);

            webRequest.Method = "POST";
            webRequest.AllowWriteStreamBuffering = true;
            webRequest.ContentType = string.Concat("multipart/form-data;boundary=", boundary);
            webRequest.Headers.Add("Accept-Encoding", "gzip,deflate");
            webRequest.Headers.Add("Accept-Charset", "ISO-8859-1");
            webRequest.Headers.Add(AuthHeader(authToken));
            webRequest.ContentLength = contentLength;

            return webRequest;
        }

        #endregion
    }
}