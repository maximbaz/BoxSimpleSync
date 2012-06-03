using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BoxSimpleSync.API.Model;
using File = BoxSimpleSync.API.Model.File;
using IOFile = System.IO.File;

namespace BoxSimpleSync.API.Request
{
    public sealed class Files
    {
        #region Static Fields and Constants

        private const string UploadUrl = "http://upload.box.net/api/1.0/upload/{0}/{1}";
        private const string DownloadUrl = "https://api.box.com/2.0/files/{0}/data";
        private const string InfoUrl = "https://api.box.com/2.0/files/{0}";

        #endregion

        #region Fields

        private readonly string boundary;
        private readonly AuthInfo authInfo;

        #endregion

        #region Constructors and Destructor

        public Files(AuthInfo authInfo) {
            this.authInfo = authInfo;
            boundary = Guid.NewGuid().ToString().Replace("-", string.Empty);
        }

        #endregion

        #region Public Methods

        public async Task Upload(List<string> filePaths, string folderId) {
            foreach (var filesBlock in await AssembleFilesBlock(filePaths)) {
                using (var resultStream = new MemoryStream()) {
                    var formattedBoundary = GetFormattedBoundary(true);

                    await resultStream.WriteAsync(filesBlock, 0, filesBlock.Length);
                    await resultStream.WriteAsync(formattedBoundary, 0, formattedBoundary.Length);

                    await resultStream.FlushAsync();
                    var result = resultStream.ToArray();
                    var request = CreateRequest(result.Length, folderId);

                    using (var requestStream = await request.GetRequestStreamAsync()) {
                        await requestStream.WriteAsync(result, 0, result.Length);
                        await requestStream.FlushAsync();

                        using (var response = await request.GetResponseAsync()) {
                            using (var responseStream = response.GetResponseStream()) {
                                if (responseStream == null)
                                    continue;

                                using (var responseReader = new StreamReader(responseStream)) {
                                    await responseReader.ReadToEndAsync();
                                }
                            }
                        }
                    }
                }
            }
        }

        public async Task Download(string fileId, string location) {
            await HttpRequest.DownloadFile(string.Format(DownloadUrl, fileId), location, authInfo.Token);
        }

        public async Task<File> GetInfo(string id) {
            return JsonParse.File(await HttpRequest.Get(string.Format(InfoUrl, id), authInfo.Token));
        }

        #endregion

        #region Protected And Private Methods

        private static async Task<byte[]> AssembleFile(string filePath) {
            byte[] buffer;

            using (var resultStream = new MemoryStream()) {
                var content = string.Format("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"{2}",
                                            Guid.NewGuid(),
                                            Path.GetFileName(filePath), 
                                            Environment.NewLine);
                buffer = Encoding.ASCII.GetBytes(content);
                await resultStream.WriteAsync(buffer, 0, buffer.Length);

                content = "Content-Type: application/octet-stream" + Environment.NewLine + Environment.NewLine;
                buffer = Encoding.ASCII.GetBytes(content);
                await resultStream.WriteAsync(buffer, 0, buffer.Length);

                buffer = IOFile.ReadAllBytes(filePath);
                await resultStream.WriteAsync(buffer, 0, buffer.Length);

                buffer = Encoding.ASCII.GetBytes(Environment.NewLine);
                await resultStream.WriteAsync(buffer, 0, buffer.Length);

                await resultStream.FlushAsync();

                buffer = resultStream.ToArray();
            }

            return buffer;
        }
        
        private HttpWebRequest CreateRequest(long contentLength, string folderId) {
            var webRequest = (HttpWebRequest) WebRequest.Create(string.Format(UploadUrl, authInfo.Token, folderId));

            webRequest.Method = "POST";
            webRequest.AllowWriteStreamBuffering = true;
            webRequest.ContentType = string.Concat("multipart/form-data;boundary=", boundary);
            webRequest.Headers.Add("Accept-Encoding", "gzip,deflate");
            webRequest.Headers.Add("Accept-Charset", "ISO-8859-1");
            webRequest.ContentLength = contentLength;

            return webRequest;
        }

        private async Task<IEnumerable<byte[]>> AssembleFilesBlock(IEnumerable<string> filePaths) {
            var result = new List<byte[]>();
            var resultStream = new MemoryStream();
            var endBoundaryLength = GetFormattedBoundary(true).Length;

            foreach (var t in filePaths) {
                var formattedBoundary = GetFormattedBoundary(false);
                var file = await AssembleFile(t);

                if (resultStream.Length + formattedBoundary.Length + file.Length + endBoundaryLength >= int.MaxValue) {
                    await resultStream.FlushAsync();
                    result.Add(resultStream.ToArray());
                    resultStream.Dispose();
                    resultStream = new MemoryStream();
                }
                
                await resultStream.WriteAsync(formattedBoundary, 0, formattedBoundary.Length);
                await resultStream.WriteAsync(file, 0, file.Length);
            }
            await resultStream.FlushAsync();
            result.Add(resultStream.ToArray());
            resultStream.Dispose();
            return result;
        }

        private byte[] GetFormattedBoundary(bool isEndBoundary) {
            var template = isEndBoundary ? "--{0}--{1}" : "--{0}{1}";
            return Encoding.ASCII.GetBytes(string.Format(template, boundary, Environment.NewLine));
        }

        #endregion
    }
}