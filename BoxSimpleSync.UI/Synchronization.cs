using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BoxSimpleSync.API;
using BoxSimpleSync.API.Model;

namespace BoxSimpleSync.UI
{
    public class Synchronization
    {
        private readonly List<FoldersPair> foldersPairs = new List<FoldersPair>();
        private readonly Api api;

        public Synchronization(User user)
        {
            api = new Api(user);

            // Hack: Sertificate is not valid on server
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
        }

        public async Task Start(IEnumerable<PathsPair> paths) {
            await api.Authentication.Login();
            await BuildFoldersPairs(paths);
            await BuildFoldersTrees();
            LookupChanges();
        }

        private async Task BuildFoldersTrees() {
            foreach (var foldersPair in foldersPairs) {
                await BuildFolderPair(foldersPair.Server);
            }
        }

        private async Task BuildFolderPair(Folder serverFolder) {
            var items = new List<Item>(from i in serverFolder.Items where i.Type != "folder" select i);

            foreach (var item in serverFolder.Items) {
                if (item.Type != "folder")
                    items.Add(item);
                else {
                    var folder = await api.Folders.GetInfo(item.Id);
                    await BuildFolderPair(folder);
                    items.Add(folder);
                }
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
                foldersPairs.Add(new FoldersPair(serverFolder, s.Local));
            }
        }

        private async Task LookupChanges() {
            foreach (var foldersPair in foldersPairs) {
                SyncFolders(foldersPair);
            }
        }

        private async void SyncFolders(FoldersPair foldersPair) {
            if (DirectoryExtensions.GetLastWriteTimestamp(foldersPair.Local) == foldersPair.Server.ModifiedAt)
                return;
            
            SyncFiles(foldersPair);

            var localFolders = Directory.GetDirectories(foldersPair.Local).ToList();
            
            foreach (Folder serverFolder in foldersPair.Server.Items.Where(x => x.Type == "folder")) {
                var i = localFolders.IndexOf(serverFolder.Name);
                if (i > -1) {
                    SyncFolders(new FoldersPair(serverFolder, foldersPair.Local + "\\" + serverFolder.Name));
                    localFolders.RemoveAt(i);
                }
                else {
                    // Todo: download folder and it's content                    
                    Directory.SetLastWriteTime(localFolders.Last(), serverFolder.ModifiedAt);
                }
            }

            foreach (var localFolder in localFolders) {
                // Todo: upload folder and it's content
            }
        }

        private void SyncFiles(FoldersPair foldersPair) {
            var localFiles = Directory.GetFiles(foldersPair.Local).ToList();

            foreach (Folder serverFolder in foldersPair.Server.Items.Where(x => x.Type != "folder"))
            {
                var i = localFiles.IndexOf(serverFolder.Name);
                if (i > -1)
                {
                    // Todo: Upload of download file
                    localFiles.RemoveAt(i);
                }
                else
                {
                    // Todo: download file
                    Directory.SetLastWriteTime(localFiles.Last(), serverFolder.ModifiedAt);
                }
            }

            foreach (var localFile in localFiles)
            {
                // Todo: upload file
            }
        }
    }
}