using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoxSimpleSync.API.Helpers;
using BoxSimpleSync.API.Interfaces;
using File = BoxSimpleSync.API.Model.File;
using IOFile = System.IO.File;

namespace BoxSimpleSync.API.Request
{
    public sealed class Files : IFiles
    {
        #region Static Fields and Constants

        private const string BaseUrl = "https://api.box.com/2.0";
        private const string FileUrl = BaseUrl + "/files/{0}";
        private const string DownloadUrl = FileUrl + "/data";
        private const string UploadUrl = "https://upload.box.com/api/2.0/files/data"; // Todo: change url when API will be ready

        #endregion

        #region Fields

        private readonly string boundary;

        #endregion

        #region Constructors and Destructor

        public Files() {
            boundary = Guid.NewGuid().ToString().Replace("-", string.Empty);
        }

        #endregion

        #region Public and Internal Properties and Indexers

        public string AuthToken { get; set; }

        #endregion

        #region Public and Internal Methods

        public async Task<List<File>> Upload(List<string> filePaths, string folderId) {
            var result = new List<File>();
            var stopBoundary = GetFormattedBoundary(true);
            var folderIdBlock = AssembleString("folder_id", folderId);

            foreach (var filesBlock in await AssembleFilesBlock(filePaths, folderIdBlock.Merge(stopBoundary))) {
                result.AddRange(JsonParse.FilesList(await HttpRequest.UploadFiles(UploadUrl, boundary, filesBlock, AuthToken)));
            }

            return result;
        }

        public async Task Download(string fileId, string location) {
            await HttpRequest.DownloadFile(string.Format(DownloadUrl, fileId), location, AuthToken);
        }

        public async Task<File> GetInfo(string id) {
            return JsonParse.File(await HttpRequest.Get(string.Format(FileUrl, id), AuthToken));
        }

        public Task Delete(string id) {
            return HttpRequest.Delete(string.Format(FileUrl, id), AuthToken);
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
                buffer = Encoding.UTF8.GetBytes(content);
                await resultStream.WriteAsync(buffer, 0, buffer.Length);

                content = "Content-Type: application/octet-stream" + Environment.NewLine + Environment.NewLine;
                buffer = Encoding.UTF8.GetBytes(content);
                await resultStream.WriteAsync(buffer, 0, buffer.Length);

                buffer = IOFile.ReadAllBytes(filePath);
                await resultStream.WriteAsync(buffer, 0, buffer.Length);

                buffer = Encoding.UTF8.GetBytes(Environment.NewLine);
                await resultStream.WriteAsync(buffer, 0, buffer.Length);

                await resultStream.FlushAsync();

                buffer = resultStream.ToArray();
            }

            return buffer;
        }

        private async Task<IEnumerable<byte[]>> AssembleFilesBlock(ICollection<string> filePaths, byte[] additionalData) {
            const int assembleFileExtraBytes = 134;
            var result = new List<byte[]>();

            var formattedBoundary = GetFormattedBoundary(false);
            var needSize = (from f in filePaths select new FileInfo(f).Length).Sum() +
                           (from f in filePaths select Encoding.UTF8.GetByteCount(Path.GetFileName(f))).Sum() +
                           (formattedBoundary.Length + assembleFileExtraBytes) * filePaths.Count +
                           additionalData.Length;
            var memoryStream = new MemoryStream((int) Math.Min(needSize, int.MaxValue));

            var streamWillExceedCapacity = new Func<MemoryStream, byte[], bool>((stream, file) => stream.Length + formattedBoundary.Length + file.Length + additionalData.Length > stream.Capacity);

            // Todo: get rid of this when API will be fixed
            var numberOfFilesCanBeUploadedPerSingleRequest = 20;
            var getStreamData = new Func<MemoryStream, byte[]>(stream => stream.Length == stream.Capacity ? stream.GetBuffer() : stream.ToArray());
            // <------------------------------------------------------------

            foreach (var t in filePaths) {
                var file = await AssembleFile(t);

                if (streamWillExceedCapacity(memoryStream, file) || numberOfFilesCanBeUploadedPerSingleRequest < 1) {
                    await memoryStream.WriteAsync(additionalData, 0, additionalData.Length);
                    await memoryStream.FlushAsync();
                    result.Add(getStreamData(memoryStream));
                    memoryStream.Dispose();
                    memoryStream = new MemoryStream((int) Math.Min(needSize, int.MaxValue));
                    numberOfFilesCanBeUploadedPerSingleRequest = 20;
                }
                await memoryStream.WriteAsync(formattedBoundary, 0, formattedBoundary.Length);
                await memoryStream.WriteAsync(file, 0, file.Length);

                needSize -= formattedBoundary.Length + file.Length;
                --numberOfFilesCanBeUploadedPerSingleRequest;
            }
            await memoryStream.WriteAsync(additionalData, 0, additionalData.Length);
            await memoryStream.FlushAsync();

            result.Add(getStreamData(memoryStream));
            memoryStream.Dispose();
            return result;
        }

        private byte[] AssembleString(string name, string value) {
            var builder = new StringBuilder();

            builder.AppendFormat("Content-Disposition: form-data; name=\"{0}\"{1}", name, Environment.NewLine);
            builder.AppendLine();
            builder.AppendLine(value);

            var assembledString = Encoding.UTF8.GetBytes(builder.ToString());
            var formattedBoundary = GetFormattedBoundary(false);

            return formattedBoundary.Merge(assembledString);
        }

        private byte[] GetFormattedBoundary(bool isEndBoundary) {
            var template = isEndBoundary ? "--{0}--{1}" : "--{0}{1}";
            return Encoding.UTF8.GetBytes(string.Format(template, boundary, Environment.NewLine));
        }

        #endregion
    }
}