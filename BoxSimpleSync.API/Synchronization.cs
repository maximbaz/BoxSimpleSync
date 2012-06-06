using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BoxSimpleSync.API.Helpers;
using BoxSimpleSync.API.Model;
using BoxSimpleSync.API.Request;
using File = BoxSimpleSync.API.Model.File;

namespace BoxSimpleSync.API
{
    public class Synchronization
    {
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
            if (api == null || UserIsLogOff) {
                var authToken = await Authentication.Login("user", "password");
                api = new Api(authToken);
            }
            await BuildFoldersPairs(paths);
            await BuildFoldersTrees();
            await LookupChanges();
        }

        #endregion

        #region Protected and Private Properties and Indexers

        protected bool UserIsLogOff {
            get {
                // Todo: it's not working solution
                try {
                    api.Folders.GetInfo("0");
                    return false;
                }
                catch (Exception) {
                    return true;
                }
            }
        }

        #endregion

        #region Protected And Private Methods

        private static void ProcessServerFiles(Pair<Folder> foldersPair, out ProcessFilesResult filesTo) {
            var localFiles = Directory.GetFiles(foldersPair.Local).ToList();
            filesTo = new ProcessFilesResult();

            foreach (File serverFile in foldersPair.Server.Items.Where(x => x.Type == "file")) {
                var localFile = foldersPair.Local + "\\" + serverFile.Name;
                var index = localFiles.IndexOf(localFile);
                var filesPair = new Pair<File>(serverFile, localFile);
                var fileWas = new FilesComparison(filesPair);

                if (index > -1) {
                    if (fileWas.PreviousStateIsUnknown) {
                        if (!fileWas.Updated) {
                            FilesComparison.Save(localFile);
                        } else {
                            filesTo.ResolveConflict.Add(filesPair);
                        }
                    } else if (fileWas.UpdatedOnServer) {
                        filesTo.Download.Add(filesPair);
                    } else if (fileWas.UpdatedOnLocal) {
                        filesTo.Upload.Add(localFile);
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

        private static void ProcessRestOfLocalFiles(ProcessFilesResult filesTo) {
            foreach (var localFile in filesTo.Process) {
                var fileWas = new FilesComparison(new Pair<File>(null, localFile));

                if (fileWas.DeletedOnServer) {
                    System.IO.File.Delete(localFile);
                    FilesComparison.Delete(localFile);
                } else {
                    filesTo.Upload.Add(localFile);
                }
            }

            filesTo.Process.Clear();
        }

        private static void ResolveConflicts(ProcessFilesResult filesTo) {
            foreach (var file in filesTo.ResolveConflict) {
                var newName = string.Format("{0}\\{1} [Conflicted Copy {2}]{3}",
                                            Path.GetDirectoryName(file.Local),
                                            Path.GetFileNameWithoutExtension(file.Local),
                                            DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"),
                                            Path.GetExtension(file.Local));
                System.IO.File.Move(file.Local, newName);
                filesTo.Download.Add(file);
                filesTo.Upload.Add(newName);
            }
            filesTo.ResolveConflict.Clear();
        }

        private async Task BuildFoldersTrees() {
            foreach (var foldersPair in foldersPairs) {
                await BuildFolderPair(foldersPair.Server);
            }
        }

        private async Task BuildFolderPair(Folder serverFolder) {
            var items = new List<Item>();

            foreach (var item in serverFolder.Items) {
                if (item.Type == "file") {
                    items.Add(await api.Files.GetInfo(item.Id));
                    continue;
                }
                var folder = await api.Folders.GetInfo(item.Id);
                await BuildFolderPair(folder);
                items.Add(folder);
            }

            serverFolder.Items = items;
        }

        private async Task BuildFoldersPairs(IEnumerable<Pair<string>> paths) {
            foldersPairs.Clear();

            foreach (var s in paths) {
                if (!Directory.Exists(s.Local))
                    throw new ApplicationException(s.Local + " doesn't exist");

                var serverFolder = await api.Folders.GetInfo("0");
                foreach (var path in s.Server.Split('/')) {
                    var id = (from c in serverFolder.Items where c.Name == path select c.Id).SingleOrDefault();
                    serverFolder = await (id != null ? api.Folders.GetInfo(id) : api.Folders.Create(path, serverFolder.Id));
                }
                foldersPairs.Add(new Pair<Folder>(serverFolder, s.Local));
            }
        }

        private async Task LookupChanges() {
            foreach (var foldersPair in foldersPairs) {
                await SyncFolders(foldersPair);
            }
        }

        private async Task SyncFolders(Pair<Folder> foldersPair) {
            await SyncFiles(foldersPair);
            return;

            var localFolders = Directory.GetDirectories(foldersPair.Local).ToList();

            foreach (Folder serverFolder in foldersPair.Server.Items.Where(x => x.Type == "folder")) {
                var index = localFolders.IndexOf(serverFolder.Name);
                if (index > -1) {
                    await SyncFolders(new Pair<Folder>(serverFolder, foldersPair.Local + "\\" + serverFolder.Name));
                } else {
                    // Todo: download folder and it's content
                }
                localFolders.RemoveAt(index);
            }

            foreach (var localFolder in localFolders) {
                // Todo: upload folder and it's content
            }
        }

        private async Task SyncFiles(Pair<Folder> foldersPair) {
            ProcessFilesResult filesTo;
            ProcessServerFiles(foldersPair, out filesTo);
            ProcessRestOfLocalFiles(filesTo);
            ResolveConflicts(filesTo);
            await UploadFiles(foldersPair, filesTo);
            await DownloadFiles(filesTo);
            await DeleteFiles(filesTo);
        }

        private async Task DeleteFiles(ProcessFilesResult filesTo) {
            foreach (var file in filesTo.DeleteOnServer) {
                await api.Files.Delete(file.Server.Id);
                FilesComparison.Delete(file.Local);
            }

            filesTo.DeleteOnServer.Clear();
        }

        private async Task DownloadFiles(ProcessFilesResult filesTo) {
            foreach (var file in filesTo.Download) {
                await api.Files.Download(file.Server.Id, file.Local);
                FilesComparison.Save(file.Local);
            }

            filesTo.Download.Clear();
        }

        private async Task UploadFiles(Pair<Folder> foldersPair, ProcessFilesResult filesTo) {
            if (filesTo.Upload.Count < 1)
                return;

            await api.Files.Upload(filesTo.Upload, foldersPair.Server.Id);
            foreach (var file in filesTo.Upload) {
                FilesComparison.Save(file);
            }

            filesTo.Upload.Clear();
        }

        #endregion

        #region Nested type: ProcessFilesResult

        private class ProcessFilesResult
        {
            #region Constructors and Destructor

            public ProcessFilesResult() {
                Upload = new List<string>();
                Download = new List<Pair<File>>();
                DeleteOnServer = new List<Pair<File>>();
                ResolveConflict = new List<Pair<File>>();
                Process = new List<string>();
            }

            #endregion

            #region Public and Internal Properties and Indexers

            public List<Pair<File>> ResolveConflict { get; private set; }
            public List<Pair<File>> Download { get; private set; }
            public List<string> Upload { get; private set; }
            public List<Pair<File>> DeleteOnServer { get; private set; }
            public List<string> Process { get; private set; }

            #endregion
        }

        #endregion
    }
}