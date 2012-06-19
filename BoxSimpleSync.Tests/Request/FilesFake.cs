using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BoxSimpleSync.API.Interfaces;
using BoxSimpleSync.API.Model;
using BoxSimpleSync.Tests.Helpers;
using File = BoxSimpleSync.API.Model.File;
using IOFile = System.IO.File;

namespace BoxSimpleSync.Tests.Request
{
    internal class FilesFake : IFiles
    {
        #region Fields

        private readonly string localPath;
        private readonly ServerMap map;
        private readonly string serverPath;

        #endregion

        #region Constructors and Destructor

        public FilesFake(ServerMap map, string localPath, string serverPath) {
            this.map = map;
            this.localPath = localPath;
            this.serverPath = serverPath;
        }

        #endregion

        #region IFiles Members

        public string AuthToken { get; set; }

        public Task<List<File>> Upload(List<string> filePaths, string folderId) {
            return Task.Run(() => {
                var files = new List<File>();

                foreach (var path in filePaths) {
                    var file = new TestFile {
                        Id = Guid.NewGuid().ToString(),
                        Name = Path.GetFileName(path),
                        Sha1 = File.ComputeSha1(path),
                        FullPath = path.Replace(localPath, serverPath)
                    };

                    files.Add(file);
                    map.Files[file.Id] = file;
                    if (!map.Folders.ContainsKey(folderId)) {
                        throw new InvalidOperationException("folder with id = [" + folderId + "] doesn't exist");
                    }

                    map.Folders[folderId].Items.Add(file);
                    IOFile.Copy(path, file.FullPath, true);
                }

                return files;
            });
        }

        public Task Download(string fileId, string location) {
            return Task.Run(() => IOFile.Copy(map.Files[fileId].FullPath, location, true));
        }

        public Task<File> GetInfo(string id) {
            return Task.Run(() => map.Files[id] as File);
        }

        public Task Delete(string id) {
            return Task.Run(() => {
                IOFile.Delete(map.Files[id].FullPath);
                FindAndRemove(map.Folders["0"].Items, map.Files[id]);
                map.Files.Remove(id);
            });
        }

        private static bool FindAndRemove(ICollection<Item> items, Item itemToRemove) {
            return !items.Remove(itemToRemove) && items.Where(f => f is Folder).Cast<Folder>().Any(item => FindAndRemove(item.Items, itemToRemove));
        }

        #endregion
    }
}