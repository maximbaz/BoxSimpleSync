using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace BoxSimpleSync.API
{
    internal sealed class FileUploader
    {
        #region Static Fields and Constants

        private const string Url = "http://upload.box.net/api/1.0/upload/{0}/{1}";

        #endregion

        #region Fields

        private readonly string boundary;
        private readonly AuthInfo authInfo;

        #endregion

        #region Constructors and Destructor

        public FileUploader(AuthInfo authInfo) {
            this.authInfo = authInfo;
            boundary = Guid.NewGuid().ToString().Replace("-", string.Empty);
        }

        #endregion

        #region Public Methods

        public void Upload(string[] filePaths, string folderId, Action onComplete) {
            byte[] buffer;

            using (var resultStream = new MemoryStream()) {
                buffer = AssembleFilesBlock(filePaths);
                resultStream.Write(buffer, 0, buffer.Length);

                buffer = GetFormattedBoundary(true);
                resultStream.Write(buffer, 0, buffer.Length);

                resultStream.Flush();
                buffer = resultStream.ToArray();
            }

            var myRequest = CreateRequest(buffer.Length, folderId);

            var writer = myRequest.GetRequestStream();

            var state = new State {
                Writer = writer,
                Request = myRequest
            };

            AsyncCallback uploadCompleted = result => {
                UploadCompleted(result);
                onComplete();
            };

            writer.BeginWrite(buffer, 0, buffer.Length, uploadCompleted, state);
        }

        #endregion

        #region Protected And Private Methods

        private static byte[] AssembleFile(string filePath) {
            byte[] buffer;

            using (var resultStream = new MemoryStream()) {
                var content = string.Format("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"{2}",
                                            Guid.NewGuid(),
                                            Path.GetFileName(filePath), 
                                            Environment.NewLine);
                buffer = Encoding.ASCII.GetBytes(content);
                resultStream.Write(buffer, 0, buffer.Length);

                content = "Content-Type: application/octet-stream" + Environment.NewLine + Environment.NewLine;
                buffer = Encoding.ASCII.GetBytes(content);
                resultStream.Write(buffer, 0, buffer.Length);

                buffer = File.ReadAllBytes(filePath);
                resultStream.Write(buffer, 0, buffer.Length);

                buffer = Encoding.ASCII.GetBytes(Environment.NewLine);
                resultStream.Write(buffer, 0, buffer.Length);

                resultStream.Flush();

                buffer = resultStream.ToArray();
            }

            return buffer;
        }

        private static void UploadCompleted(IAsyncResult asyncResult) {
            var state = (State) asyncResult.AsyncState;
            var stopExecution = false;

            try {
                state.Writer.EndWrite(asyncResult);
            }
            catch (Exception) {
                stopExecution = true;
            }
            finally {
                state.Writer.Close();
                state.Writer.Dispose();
            }

            if (stopExecution)
                return;

            var myHttpWebResponse = (HttpWebResponse) state.Request.GetResponse();

            using (var responseStream = myHttpWebResponse.GetResponseStream()) {
                if (responseStream != null) {
                    new StreamReader(responseStream);
                }
            }

            myHttpWebResponse.Close();
            ((IDisposable) myHttpWebResponse).Dispose();
        }

        private HttpWebRequest CreateRequest(long contentLength, string folderId) {
            var webRequest = (HttpWebRequest) WebRequest.Create(string.Format(Url, authInfo.Token, folderId));

            webRequest.Method = "POST";
            webRequest.AllowWriteStreamBuffering = true;
            webRequest.ContentType = string.Concat("multipart/form-data;boundary=", boundary);
            webRequest.Headers.Add("Accept-Encoding", "gzip,deflate");
            webRequest.Headers.Add("Accept-Charset", "ISO-8859-1");
            webRequest.ContentLength = contentLength;

            return webRequest;
        }

        private byte[] AssembleFilesBlock(IEnumerable<string> filePaths) {
            byte[] buffer;

            using (var resultStream = new MemoryStream()) {
                foreach (var t in filePaths) {
                    buffer = GetFormattedBoundary(false);
                    resultStream.Write(buffer, 0, buffer.Length);

                    buffer = AssembleFile(t);
                    resultStream.Write(buffer, 0, buffer.Length);
                }

                resultStream.Flush();
                buffer = resultStream.ToArray();
            }

            return buffer;
        }

        private byte[] GetFormattedBoundary(bool isEndBoundary) {
            var template = isEndBoundary ? "--{0}--{1}" : "--{0}{1}";

            return Encoding.ASCII.GetBytes(string.Format(template, boundary, Environment.NewLine));
        }

        #endregion

        #region Nested type: State

        private struct State
        {
            #region Properties and Indexers

            public Stream Writer { get; set; }
            public HttpWebRequest Request { get; set; }

            #endregion
        }

        #endregion
    }
}