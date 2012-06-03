using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using BoxSimpleSync.API;
using BoxSimpleSync.API.Model;
using File = BoxSimpleSync.API.Model.File;
using IOFile = System.IO.File;

namespace BoxSimpleSync.UI
{
    public class Synchronization
    {
        #region Fields

        private readonly Api api;
        private readonly List<ItemsPair<Folder>> foldersPairs = new List<ItemsPair<Folder>>();

        #endregion

        #region Constructors and Destructor

        public Synchronization(User user) {
            api = new Api(user);
            // Hack: Sertificate is not valid on server
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
        }

        #endregion

        #region Public Methods

        public async Task Start(IEnumerable<PathsPair> paths) {
            try {
                await api.Authentication.Login();
                await BuildFoldersPairs(paths);
                await BuildFoldersTrees();
                await LookupChanges();
            }
            catch (Exception e) {
                MessageBox.Show(e.Message);
            }
        }

        #endregion

        #region Protected And Private Methods

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

        private async Task BuildFoldersPairs(IEnumerable<PathsPair> paths) {
            foreach (var s in paths) {
                if (!Directory.Exists(s.Local))
                    throw new ApplicationException(s.Local + " doesn't exist");

                var serverFolder = await api.Folders.GetInfo("0");
                foreach (var path in s.Server.Split('/')) {
                    var id = (from c in serverFolder.Items where c.Name == path select c.Id).SingleOrDefault();
                    serverFolder = await (id != null ? api.Folders.GetInfo(id) : api.Folders.Create(path, serverFolder.Id));
                }
                foldersPairs.Add(new ItemsPair<Folder>(serverFolder, s.Local));
            }
        }

        private async Task LookupChanges() {
            foreach (var foldersPair in foldersPairs) {
                await SyncFolders(foldersPair);
            }
        }

        private async Task SyncFolders(ItemsPair<Folder> foldersPair) {
            await SyncFiles(foldersPair);
            return;

            var localFolders = Directory.GetDirectories(foldersPair.Local).ToList();

            foreach (Folder serverFolder in foldersPair.Server.Items.Where(x => x.Type == "folder")) {
                var index = localFolders.IndexOf(serverFolder.Name);
                if (index > -1) {
                    await SyncFolders(new ItemsPair<Folder>(serverFolder, foldersPair.Local + "\\" + serverFolder.Name));
                }
                else {
                    // Todo: download folder and it's content
                }
                localFolders.RemoveAt(index);
            }

            foreach (var localFolder in localFolders) {
                // Todo: upload folder and it's content
            }
        }

        private async Task SyncFiles(ItemsPair<Folder> foldersPair) {
            var localFiles = Directory.GetFiles(foldersPair.Local).ToList();
            var filesToUpload = new List<string>();

            await api.Files.Upload(localFiles, foldersPair.Server.Id);
            return;

            foreach (File serverFile in foldersPair.Server.Items.Where(x => x.Type == "file")) {
                var localFile = foldersPair.Local + "\\" + serverFile.Name;
                var index = localFiles.IndexOf(localFile);
                var filesPair = new ItemsPair<File>(serverFile, localFile);
                var fileWas = new FilesComparison(filesPair);

                if (index > -1) {
                    if (fileWas.PreviousStateIsUnknown) {
                        if (!fileWas.Updated)
                        {
                            // save serverFile to db
                            continue;
                        }
                        else {
                            // conflict
                        }
                    } else if (fileWas.UpdatedOnServer) {
                        await api.Files.Download(serverFile.Id, localFile);
                        // update in db
                    } else if (fileWas.UpdatedOnLocal) {
                        filesToUpload.Add(localFile);
                    } else {
                        // conflict
                    }
                    localFiles.RemoveAt(index);
                } else {
                    if (fileWas.DeletedOnLocal) {
                        // delete on server
                    } else if (fileWas.CreatedOnServer) {
                        await api.Files.Download(serverFile.Id, localFile);
                    }
                }
            }

            foreach (var localFile in localFiles.Select(file => foldersPair.Local + "\\" + file)) {
                var fileWas = new FilesComparison(new ItemsPair<File>(null, localFile));

                if (fileWas.DeletedOnServer) {
                    IOFile.Delete(localFile);
                    // delete from db
                } else {
                    filesToUpload.Add(localFile);
                }
            }

            if(filesToUpload.Count > 0) {
                await api.Files.Upload(filesToUpload, foldersPair.Server.Id);
            }
        }

        #endregion
    }
}