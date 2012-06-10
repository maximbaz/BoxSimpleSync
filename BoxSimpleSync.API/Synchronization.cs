using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BoxSimpleSync.API.Comparisons;
using BoxSimpleSync.API.Model;
using BoxSimpleSync.API.Request;
using File = BoxSimpleSync.API.Model.File;
using IOFile = System.IO.File;

namespace BoxSimpleSync.API
{
    public class Synchronization
    {
        #region Events

        public event Action Authenticating;
        public event Action Deleting;
        public event Action Done;
        public event Action Downloading;
        public event Action Error;
        public event Action Preparing;
        public event Action Synchronizing;
        public event Action Uploading;

        #endregion

        #region Fields

        private readonly List<Pair<Folder>> foldersPairs = new List<Pair<Folder>>();
        private Api api;

        #endregion

        #region Constructors and Destructor

        public Synchronization() {
            // Hack: Sertificate is not valid on server
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
        }

        #endregion

        #region Public and Internal Methods

        public async Task Start(IEnumerable<Pair<string>> paths) {
            try {
                if (api == null || await UserIsLogOff()) {
                    Fire(Authenticating);
                    var authToken = await Authentication.Login("user", "password");
                    api = new Api(authToken);
                }

                Fire(Preparing);
                await BuildFoldersPairs(paths);
                await BuildFoldersTrees();

                Fire(Synchronizing);
                var foldersTo = new ProcessResult<Folder>();
                var filesTo = new ProcessResult<File>();
                await LookupChanges(foldersTo, filesTo);

                Fire(Uploading);
                await UploadFiles(filesTo);

                Fire(Downloading);
                await DownloadFiles(filesTo);

                Fire(Deleting);
                await DeleteFiles(filesTo);
                await DeleteFolders(foldersTo);

                Fire(Done);
            }

            catch (Exception e) {
// ReSharper disable UnusedVariable
                Fire(Error);
                var msg = e.Message;
// ReSharper restore UnusedVariable
            }
        }

        #endregion

        #region Protected And Private Methods

        private async Task<bool> UserIsLogOff() {
            try {
                await api.Folders.GetInfo("0");
                return false;
            }
            catch (WebException e) {
                if (((HttpWebResponse) e.Response).StatusCode == HttpStatusCode.Unauthorized) {
                    return true;
                }
                throw;
            }
        }

        private static void Fire(Action handler) {
            if (handler != null) {
                handler();
            }
        }

        private async Task BuildFoldersTrees() {
            foreach (var foldersPair in foldersPairs) {
                await BuildFolderTree(foldersPair.Server);
            }
        }

        private async Task BuildFolderTree(Folder serverFolder) {
            var items = new List<Item>();

            foreach (var item in serverFolder.Items) {
                if (item.Type == "file") {
                    items.Add(await api.Files.GetInfo(item.Id));
                    continue;
                }
                var folder = await api.Folders.GetInfo(item.Id);
                await BuildFolderTree(folder);
                items.Add(folder);
            }

            serverFolder.Items = items;
        }

        private async Task BuildFoldersPairs(IEnumerable<Pair<string>> paths) {
            foldersPairs.Clear();

            foreach (var s in paths) {
                if (!Directory.Exists(s.Local))
                    Directory.CreateDirectory(s.Local);

                var serverFolder = await api.Folders.GetInfo("0");
                foreach (var path in s.Server.Split('/')) {
                    var id = (from c in serverFolder.Items where c.Name == path select c.Id).SingleOrDefault();
                    serverFolder = await (id != null ? api.Folders.GetInfo(id) : api.Folders.Create(path, serverFolder.Id));
                }
                foldersPairs.Add(new Pair<Folder>(serverFolder, s.Local));
            }
        }

        private async Task LookupChanges(ProcessResult<Folder> foldersTo, ProcessResult<File> filesTo) {
            foreach (var foldersPair in foldersPairs) {
                await SyncFolders(foldersPair, foldersTo, filesTo);
            }
        }

        private async Task SyncFolders(Pair<Folder> foldersPair, ProcessResult<Folder> foldersTo, ProcessResult<File> filesTo) {
            SyncFiles(foldersPair, filesTo);
            await ProcessServerFolders(foldersPair, foldersTo, filesTo);
            await ProcessRestOfLocalFolders(foldersTo, filesTo, foldersPair.Server.Id);
        }

        private static void SyncFiles(Pair<Folder> foldersPair, ProcessResult<File> filesTo) {
            ProcessServerFiles(foldersPair, filesTo);
            ProcessRestOfLocalFiles(filesTo, foldersPair.Server.Id);
            ResolveConflicts(filesTo, foldersPair.Server.Id);
        }

        private async Task ProcessServerFolders(Pair<Folder> foldersPair, ProcessResult<Folder> foldersTo, ProcessResult<File> filesTo) {
            var localFolders = Directory.GetDirectories(foldersPair.Local).ToList();

            foreach (Folder serverFolder in foldersPair.Server.Items.Where(x => x.Type == "folder")) {
                var localFolder = foldersPair.Local + "\\" + serverFolder.Name;
                var pair = new Pair<Folder>(serverFolder, localFolder);
                var folderWas = new FoldersComparison(pair);

                var index = localFolders.IndexOf(localFolder);
                if (index > -1) {
                    if (folderWas.PreviousStateIsUnknown) {
                        FoldersComparison.Save(localFolder);
                    }
                    localFolders.RemoveAt(index);
                } else {
                    if (folderWas.CreatedOnServer) {
                        Directory.CreateDirectory(localFolder);
                        FoldersComparison.Save(localFolder);
                    } else {
                        foldersTo.DeleteOnServer.Add(pair);
                        continue;
                    }
                }
                await SyncFolders(pair, foldersTo, filesTo);
            }

            foldersTo.Process.AddRange(localFolders);
        }

        private static void ProcessServerFiles(Pair<Folder> foldersPair, ProcessResult<File> filesTo) {
            var localFiles = Directory.GetFiles(foldersPair.Local).ToList();

            foreach (File serverFile in foldersPair.Server.Items.Where(x => x.Type == "file")) {
                var localFile = foldersPair.Local + "\\" + serverFile.Name;
                var index = localFiles.IndexOf(localFile);
                var filesPair = new Pair<File>(serverFile, localFile);
                var fileWas = new FilesComparison(filesPair);

                if (index > -1) {
                    if (fileWas.PreviousStateIsUnknown) {
                        if (!fileWas.Updated) {
                            FilesComparison.Save(localFile, File.ComputeSha1(localFile));
                        } else {
                            filesTo.ResolveConflict.Add(filesPair);
                        }
                    } else if (fileWas.UpdatedOnServer) {
                        filesTo.Download.Add(filesPair);
                    } else if (fileWas.UpdatedOnLocal) {
                        filesTo.Upload.Add(localFile, foldersPair.Server.Id);
                    } else if (fileWas.Updated) {
                        filesTo.ResolveConflict.Add(filesPair);
                    }
                    localFiles.RemoveAt(index);
                } else {
                    if (fileWas.CreatedOnServer) {
                        filesTo.Download.Add(filesPair);
                    } else if (fileWas.DeletedOnLocal) {
                        filesTo.DeleteOnServer.Add(filesPair);
                    } else {
                        filesTo.ResolveConflict.Add(filesPair);
                    }
                }
            }

            filesTo.Process.AddRange(localFiles);
        }

        private static void ProcessRestOfLocalFiles(ProcessResult<File> filesTo, string folderId) {
            foreach (var localFile in filesTo.Process) {
                var fileWas = new FilesComparison(new Pair<File>(null, localFile));

                if (fileWas.DeletedOnServer) {
                    IOFile.Delete(localFile);
                    FilesComparison.Remove(localFile);
                } else {
                    filesTo.Upload.Add(localFile, folderId);
                }
            }

            filesTo.Process.Clear();
        }

        private async Task ProcessRestOfLocalFolders(ProcessResult<Folder> foldersTo, ProcessResult<File> filesTo, string folderId) {
            var foldersToProcess = foldersTo.Process;
            foldersTo.Process = new List<string>();

            foreach (var localFolder in foldersToProcess) {
                var folderWas = new FoldersComparison(new Pair<Folder>(null, localFolder));
                if (folderWas.DeletedOnServer) {
                    Directory.Delete(localFolder, true);
                    FoldersComparison.Remove(localFolder);
                } else {
                    var folderName = localFolder.Split(new[] {Path.DirectorySeparatorChar}, StringSplitOptions.RemoveEmptyEntries).Last();
                    var serverFolder = await api.Folders.Create(folderName, folderId);
                    FoldersComparison.Save(localFolder);
                    await SyncFolders(new Pair<Folder>(serverFolder, localFolder), foldersTo, filesTo);
                }
            }
        }

        private static void ResolveConflicts(ProcessResult<File> filesTo, string folderId) {
            foreach (var file in filesTo.ResolveConflict) {
                var newName = string.Format("{0}\\{1} [Conflicted Copy {2}]{3}",
                                            Path.GetDirectoryName(file.Local),
                                            Path.GetFileNameWithoutExtension(file.Local),
                                            DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"),
                                            Path.GetExtension(file.Local));
                IOFile.Move(file.Local, newName);
                filesTo.Download.Add(file);
                filesTo.Upload.Add(newName, folderId);
            }
            filesTo.ResolveConflict.Clear();
        }

        private async Task DeleteFiles(ProcessResult<File> filesTo) {
            foreach (var file in filesTo.DeleteOnServer) {
                await api.Files.Delete(file.Server.Id);
                FilesComparison.Remove(file.Local);
            }

            filesTo.DeleteOnServer.Clear();
        }

        private async Task DeleteFolders(ProcessResult<Folder> foldersTo) {
            foreach (var folder in foldersTo.DeleteOnServer) {
                await api.Folders.Delete(folder.Server.Id);
                FoldersComparison.Remove(folder.Local);
            }

            foldersTo.DeleteOnServer.Clear();
        }

        private async Task DownloadFiles(ProcessResult<File> filesTo) {
            foreach (var file in filesTo.Download) {
                await api.Files.Download(file.Server.Id, file.Local);
                FilesComparison.Save(file.Local, File.ComputeSha1(file.Local));
            }

            filesTo.Download.Clear();
        }

        private async Task UploadFiles(ProcessResult<File> filesTo) {
            if (filesTo.Upload.Count < 1)
                return;

            foreach (var files in filesTo.Upload) {
                await api.Files.Upload(files.Value, files.Key);
            }

            foreach (var file in filesTo.Upload.SelectMany(files => files.Value)) {
                FilesComparison.Save(file, File.ComputeSha1(file));
            }

            filesTo.Upload.Clear();
        }

        #endregion

        #region Nested type: ProcessResult

        private class ProcessResult<T> where T : Item
        {
            #region Constructors and Destructor

            public ProcessResult() {
                Upload = new UploadInfo();
                Download = new List<Pair<T>>();
                DeleteOnServer = new List<Pair<T>>();
                ResolveConflict = new List<Pair<T>>();
                Process = new List<string>();
            }

            #endregion

            #region Public and Internal Properties and Indexers

            public List<Pair<T>> ResolveConflict { get; private set; }
            public List<Pair<T>> Download { get; private set; }
            public UploadInfo Upload { get; private set; }
            public List<Pair<T>> DeleteOnServer { get; private set; }
            public List<string> Process { get; set; }

            #endregion
        }

        #endregion

        #region Nested type: UploadInfo

        private class UploadInfo : Dictionary<string, List<string>>
        {
            #region Public and Internal Methods

            public void Add(string path, string parentId) {
                if (!ContainsKey(parentId)) {
                    this[parentId] = new List<string> {path};
                } else {
                    this[parentId].Add(path);
                }
            }

            #endregion
        }

        #endregion
    }
}