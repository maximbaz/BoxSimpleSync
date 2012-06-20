using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BoxSimpleSync.API;
using BoxSimpleSync.API.Comparisons;
using BoxSimpleSync.API.Helpers;
using BoxSimpleSync.API.Model;
using BoxSimpleSync.Tests.Helpers;
using BoxSimpleSync.Tests.Request;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nito.AsyncEx.UnitTests;
using File = BoxSimpleSync.API.Model.File;
using IOFile = System.IO.File;

// ReSharper disable InconsistentNaming

namespace BoxSimpleSync.Tests
{
    [AsyncTestClass]
    public class SynchronizationTests
    {
        #region Static Fields and Constants

        private const string Folder = "__TESTS__";
        private const string Server = Folder + "\\Server";
        private const string Local = Folder + "\\Local";
        private const string TestFilesCollection = "files_test";
        private const string TestFoldersCollection = "folders_test";

        #endregion

        #region Fields

        private Api api;
        private ServerMap map;
        private List<Pair<string>> paths;

        #endregion

        #region Public and Internal Methods

        [TestInitialize]
        public void InitFolders() {
            Directory.CreateDirectory(Folder);
            Directory.CreateDirectory(Server);
            Directory.CreateDirectory(Local);

            map = new ServerMap();
            map.Folders.Add("0", new TestFolder {
                FullPath = Server,
                Id = "0",
                Items = new List<Item>(),
                Name = "All files"
            });

            paths = new List<Pair<string>> {new Pair<string>("", Local)};
        }

        [TestCleanup]
        public void CleanupFolders() {
            Directory.Delete(Folder, true);
            Db.Clear(TestFilesCollection);
            Db.Clear(TestFoldersCollection);
        }

        #endregion

        #region Files API

        [TestMethod]
        public async void Can_Sync_Empty_Folders() {
            var sync = Init();

            await sync.Start(paths);

            AssertFoldersAreSame(Server, Local, 0);
        }

        [TestMethod]
        public async void Can_Download_New_Files() {
            var sync = Init();
            CreateServerFiles(FilesList);

            await sync.Start(paths);

            AssertFoldersAreSame(Server, Local, FilesList.Count);
        }

        [TestMethod]
        public async void Can_Upload_New_Files() {
            var sync = Init();
            CreateLocalFiles(FilesList);

            await sync.Start(paths);

            AssertFoldersAreSame(Server, Local, FilesList.Count);
        }

        [TestMethod]
        public async void Can_Delete_Files_On_Server() {
            // upload some files to server and sync
            var sync = Init();
            CreateLocalFiles(FilesList);
            await sync.Start(paths);
            AssertFoldersAreSame(Server, Local, FilesList.Count);

            // delete files on local
            const int filesToDelete = 2;
            for (var i = 0; i < filesToDelete; i++) {
                IOFile.Delete(Local + "\\" + FilesList[i]);
            }

            await sync.Start(paths);

            AssertFoldersAreSame(Server, Local, FilesList.Count - filesToDelete);
        }

        [TestMethod]
        public async void Can_Delete_Files_On_Local() {
            // upload some files to server and sync
            var sync = Init();
            CreateLocalFiles(FilesList);
            await sync.Start(paths);
            AssertFoldersAreSame(Server, Local, FilesList.Count);

            // delete files on server
            const int filesToDelete = 2;
            for (var i = 0; i < filesToDelete; i++) {
                var fileToRemove = (from f in map.Files where f.Value.Name == Path.GetFileName(FilesList[i]) select f.Key).Single();
                await api.Files.Delete(fileToRemove);
            }

            await sync.Start(paths);

            AssertFoldersAreSame(Server, Local, FilesList.Count - filesToDelete);
        }

        [TestMethod]
        public async void Upload_New_Files_If_Only_Local_Has_Changes() {
            // upload some files to server
            var sync = Init();
            CreateLocalFiles(FilesList);
            await sync.Start(paths);
            AssertFoldersAreSame(Server, Local, FilesList.Count);

            // rewrite files on local
            CreateLocalFiles(FilesList);

            await sync.Start(paths);

            AssertFoldersAreSame(Server, Local, FilesList.Count);
        }

        [TestMethod]
        public async void Download_New_Files_If_Only_Server_Has_Changes() {
            // upload some files to server
            var sync = Init();
            CreateLocalFiles(FilesList);
            await sync.Start(paths);
            AssertFoldersAreSame(Server, Local, FilesList.Count);

            // rewrite files on server
            map.Files.Clear();
            map.Folders["0"].Items.Clear();
            CreateServerFiles(FilesList);

            await sync.Start(paths);

            AssertFoldersAreSame(Server, Local, FilesList.Count);
        }

        [TestMethod]
        public async void Resolve_Basic_File_Conflicts_If_Local_And_Server_Have_Changes() {
            // upload some files to server
            var sync = Init();
            CreateLocalFiles(FilesList);
            await sync.Start(paths);
            AssertFoldersAreSame(Server, Local, FilesList.Count);

            // rewrite files on local
            CreateLocalFiles(FilesList);

            // rewrite files on server
            map.Files.Clear();
            map.Folders["0"].Items.Clear();
            CreateServerFiles(FilesList);

            await sync.Start(paths);

            AssertFoldersAreSame(Server, Local, FilesList.Count * 2);
        }

        #endregion

        #region Folders API

        [TestMethod]
        public async void Can_Upload_Empty_Folders()
        {
            var sync = Init();
            CreateLocalFolders(FoldersList);

            await sync.Start(paths);

            AssertFoldersAreSame(Server, Local, 0);
        }

        [TestMethod]
        public async void Can_Upload_New_Files_In_Folders()
        {
            var sync = Init();
            CreateLocalFiles(FilesInFoldersList);

            await sync.Start(paths);

            AssertFoldersAreSame(Server, Local, FilesList.Count);
        }

        [TestMethod]
        public async void Can_Download_New_Files_In_Folders()
        {
            var sync = Init();
            CreateServerFiles(FilesInFoldersList);

            await sync.Start(paths);

            AssertFoldersAreSame(Server, Local, 6);
        }

        [TestMethod]
        public async void Can_Delete_Files_In_Folders_On_Server()
        {
            // upload some files to server and sync
            var sync = Init();
            CreateLocalFiles(FilesInFoldersList);
            await sync.Start(paths);
            AssertFoldersAreSame(Server, Local, FilesList.Count);

            // delete files on local
            const int filesToDelete = 4;
            for (var i = 0; i < filesToDelete; i++)
            {
                IOFile.Delete(Local + "\\" + FilesInFoldersList[i]);
            }

            await sync.Start(paths);

            AssertFoldersAreSame(Server, Local, FilesList.Count, FilesList.Count - filesToDelete, FilesList.Count);
        }

        [TestMethod]
        public async void Can_Delete_Whole_Folders_On_Server()
        {
            // upload some files to server and sync
            var sync = Init();
            CreateLocalFiles(FilesInFoldersList);
            await sync.Start(paths);
            AssertFoldersAreSame(Server, Local, FilesList.Count);

            // delete folders on local
            const int foldersToDelete = 4;
            for (var i = 1; i < foldersToDelete; i++) {
                Directory.Delete(Local + "\\" + FoldersList[i], true);
            }

            await sync.Start(paths);

            AssertFoldersAreSame(Server, Local, FilesList.Count);
        }

        [TestMethod]
        public async void Can_Delete_Files_In_Folders_On_Local()
        {
            // upload some files to server and sync
            var sync = Init();
            CreateLocalFiles(FilesInFoldersList);
            await sync.Start(paths);
            AssertFoldersAreSame(Server, Local, FilesList.Count);

            // delete files on server
            const int filesToDelete = 4;
            for (var i = 0; i < filesToDelete; i++)
            {
                var fileToRemove = (from f in map.Files where f.Value.FullPath == Server + "\\" + FilesInFoldersList[i] select f.Key).Single();
                await api.Files.Delete(fileToRemove);
            }

            await sync.Start(paths);

            AssertFoldersAreSame(Server, Local, FilesList.Count, FilesList.Count - filesToDelete, FilesList.Count);
        }

        [TestMethod]
        public async void Can_Delete_Whole_Folders_On_Local()
        {
            // upload some files to server and sync
            var sync = Init();
            CreateLocalFiles(FilesInFoldersList);
            await sync.Start(paths);
            AssertFoldersAreSame(Server, Local, FilesList.Count);

            // delete folders on server
            const int foldersToDelete = 4;
            for (var i = 1; i < foldersToDelete; i++)
            {
                var folderToDelete = (from f in map.Folders where f.Value.FullPath == Server + "\\" + FoldersList[i] select f.Key).Single();
                await api.Folders.Delete(folderToDelete);
            }

            await sync.Start(paths);

            AssertFoldersAreSame(Server, Local, FilesList.Count);
        }

        [TestMethod]
        public async void Upload_New_Files_In_Folders_If_Only_Local_Has_Changes()
        {
            // upload some files to server
            var sync = Init();
            CreateLocalFiles(FilesInFoldersList);
            await sync.Start(paths);
            AssertFoldersAreSame(Server, Local, FilesList.Count);

            // rewrite files on local
            CreateLocalFiles(FilesInFoldersList);

            await sync.Start(paths);

            AssertFoldersAreSame(Server, Local, FilesList.Count);
        }

        [TestMethod]
        public async void Download_New_Files_In_Folders_If_Only_Server_Has_Changes()
        {
            // upload some files to server
            var sync = Init();
            CreateLocalFiles(FilesInFoldersList);
            await sync.Start(paths);
            AssertFoldersAreSame(Server, Local, FilesList.Count);

            // rewrite files on server
            map.Files.Clear();
            map.Folders.Clear();
            map.Folders.Add("0", new TestFolder
            {
                FullPath = Server,
                Id = "0",
                Items = new List<Item>(),
                Name = "All files"
            });
            
            CreateServerFiles(FilesInFoldersList);

            await sync.Start(paths);

            AssertFoldersAreSame(Server, Local, FilesList.Count);
        }

        [TestMethod]
        public async void Resolve_Basic_File_Conflicts_In_Folders_If_Local_And_Server_Have_Changes()
        {
            // upload some files to server
            var sync = Init();
            CreateLocalFiles(FilesInFoldersList);
            await sync.Start(paths);
            AssertFoldersAreSame(Server, Local, FilesList.Count);

            // rewrite files on local
            CreateLocalFiles(FilesInFoldersList);

            // rewrite files on server
            map.Files.Clear();
            map.Folders.Clear();
            map.Folders.Add("0", new TestFolder
            {
                FullPath = Server,
                Id = "0",
                Items = new List<Item>(),
                Name = "All files"
            });
            CreateServerFiles(FilesInFoldersList);

            await sync.Start(paths);

            AssertFoldersAreSame(Server, Local, FilesList.Count * 2);
        }

        [TestMethod]
        public async void Resolve_Local_Folder_Changes_Server_Folder_Was_Deleted()
        {
            // upload some files to server
            var sync = Init();
            CreateLocalFiles(FilesInFoldersList);
            await sync.Start(paths);
            AssertFoldersAreSame(Server, Local, FilesList.Count);

            // add new file on local
            CreateLocalFiles(new List<string> { "folder1\\folder3\\folder4\\file7.new" });

            // delete folder on server
            await api.Folders.Delete((from d in map.Folders where d.Value.Name == "folder4" select d).Single().Key);

            await sync.Start(paths);

            AssertFoldersAreSame(Server, Local, FilesList.Count, FilesList.Count, FilesList.Count, FilesList.Count, 1, FilesList.Count);
        }

        #endregion

        #region Protected and Private Properties and Indexers

        private static List<string> FilesList {
            get { return new List<string> {"file1.txt", "file2.rtf", "file3.exe", "file4.docx", "file5.exe", "file6.diff"}; }
        }

        private static List<string> FoldersList {
            get { return new List<string> { "folder1", "folder5", "folder1\\folder2", "folder1\\folder3", "folder1\\folder3\\folder4", "" }; }
        }

        private static List<string> FilesInFoldersList {
            get { return (from d in FoldersList from f in FilesList select d + "\\" + f).ToList(); }
        }

        #endregion

        #region Protected And Private Methods

        private static void CreateLocalFiles(IEnumerable<string> files) {
            CreateFiles(from f in files select Local + "\\" + f);
        }

        private void CreateServerFiles(List<string> files) {
            var folders = (from f in files where !f.StartsWith("\\") select Path.GetDirectoryName(f)).Distinct();
            CreateServerFolders(from f in folders where !string.IsNullOrEmpty(f) select f);

            foreach (var file in CreateFiles(from f in files select Server + "\\" + f.TrimStart('\\'))) {
                map.Files.Add(file.Id, file);
                var parent = (from d in map.Folders
                              where d.Value.FullPath == Path.GetDirectoryName(file.FullPath)
                              select d.Value).SingleOrDefault() 
                              ?? map.Folders["0"];
                parent.Items.Add(file);
            }
        }

        private static List<TestFile> CreateFiles(IEnumerable<string> files) {
            var result = new List<TestFile>();
            var random = new Random();

            foreach (var file in files) {
                new FileInfo(file).Directory.Create();
                using (var stream = IOFile.CreateText(file)) {
                    for (int i = 0, l = random.Next(1, 100); i < l; i++) {
                        stream.Write(Guid.NewGuid().ToString());
                    }
                }
                result.Add(new TestFile {
                    Id = Guid.NewGuid().ToString(),
                    Name = Path.GetFileName(file),
                    Sha1 = File.ComputeSha1(file),
                    FullPath = file
                });
            }

            return result;
        }

        private static void CreateLocalFolders(IEnumerable<string> folders)
        {
            CreateFolders(from f in folders select Local + "\\" + f);
        }

        private void CreateServerFolders(IEnumerable<string> folders)
        {
            foreach (var folder in CreateFolders(from f in folders select Server + "\\" + f)) {
                var parent = (from f in map.Folders where f.Value.FullPath == Path.GetDirectoryName(folder.FullPath) select f).Single();
                map.Folders.Add(folder.Id, folder);
                map.Folders[parent.Key].Items.Add(folder);
            }
        }

        private static List<TestFolder> CreateFolders(IEnumerable<string> folders)
        {
            var result = new List<TestFolder>();

            foreach (var folder in folders) {
                Directory.CreateDirectory(folder);
                result.Add(new TestFolder
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = Path.GetFileName(folder),
                    Items = new List<Item>(),
                    FullPath = folder.TrimEnd('\\')
                });
            }

            return result;
        }

        private Synchronization Init() {
            api = new Api(null, new FilesFake(map, Local, Server), new FoldersFake(map));
            return new Synchronization(api, new FilesComparison(TestFilesCollection), new FoldersComparison(TestFoldersCollection));
        }

        private static int[] AssertFoldersAreSame(string first, string second, params int[] expectedFilesCount) {
            var filesInFirst = Directory.GetFiles(first);
            var filesInSecond = Directory.GetFiles(second);
            Assert.AreEqual(filesInFirst.Length, filesInSecond.Length);
            Assert.AreEqual(filesInFirst.Length, expectedFilesCount[0]);

            var fileNamesInFirst = filesInFirst.Select(Path.GetFileName).ToList();
            var fileNamesInSecond = filesInSecond.Select(Path.GetFileName).ToList();
            for (var indexInFirst = 0; indexInFirst < fileNamesInFirst.Count; indexInFirst++) {
                var indexInSecond = fileNamesInSecond.IndexOf(fileNamesInSecond[indexInFirst]);
                Assert.IsTrue(indexInSecond > -1);
                Assert.AreEqual(File.ComputeSha1(filesInFirst[indexInFirst]), File.ComputeSha1(filesInSecond[indexInSecond]));
            }

            var foldersInFirst = Directory.GetDirectories(first).Select(Path.GetFileName).ToList();
            var foldersInSecond = Directory.GetDirectories(second).Select(Path.GetFileName).ToList();
            Assert.AreEqual(foldersInFirst.Count, foldersInSecond.Count);

            expectedFilesCount = expectedFilesCount.Length > 1 ? expectedFilesCount.Skip(1).ToArray() : expectedFilesCount;

            foreach (var folder in foldersInFirst) {
                Assert.IsTrue(foldersInSecond.Contains(folder));
                expectedFilesCount = AssertFoldersAreSame(Path.Combine(first, folder), Path.Combine(second, folder), expectedFilesCount);
            }

            return expectedFilesCount;
        }

        #endregion
    }
}

// ReSharper restore InconsistentNaming