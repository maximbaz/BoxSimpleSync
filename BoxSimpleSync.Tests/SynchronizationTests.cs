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

            AssertFoldersAreSame(Server, Local);
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
            const int foldersToDelete = 2;
            for (var i = 0; i < foldersToDelete; i++) {
                IOFile.Delete(Local + "\\" + FilesList[i]);
            }

            await sync.Start(paths);

            AssertFoldersAreSame(Server, Local, FilesList.Count - foldersToDelete);
        }

        [TestMethod]
        public async void Can_Delete_Files_On_Local() {
            // upload some files to server and sync
            var sync = Init();
            CreateLocalFiles(FilesList);
            await sync.Start(paths);
            AssertFoldersAreSame(Server, Local, FilesList.Count);

            // delete files on server
            const int foldersToDelete = 2;
            for (var i = 0; i < foldersToDelete; i++) {
                var fileToRemove = (from f in map.Files where f.Value.Name == Path.GetFileName(FilesList[i]) select f.Key).Single();
                await api.Files.Delete(fileToRemove);
            }

            await sync.Start(paths);

            AssertFoldersAreSame(Server, Local, FilesList.Count - foldersToDelete);
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
        public async void Resolve_Conflicts_If_Local_And_Server_Have_Changes() {
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

        [TestMethod]
        public async void Can_Sync_When_All_Sync_Situations_Appeared_In_One_Time() {
            // upload some files to server
            var sync = Init();
            CreateLocalFiles(FilesList);
            await sync.Start(paths);
            AssertFoldersAreSame(Server, Local, FilesList.Count);

            // add new files on local, one will conflict with new file on server
            CreateLocalFiles(new List<string> {"file7.txt", "file8.jpeg"});

            // add new files on server
            CreateServerFiles(new List<string> {"file7.txt", "file9.doc"});

            // rewrite files on local, some also will be rewritten on server
            const int filesToRewrite = 4;
            CreateLocalFiles(FilesList.Take(filesToRewrite));

            // rewrite files on server
            for (var i = 0; i < filesToRewrite; i++) {
                await api.Files.Delete((from f in map.Files where f.Value.Name == FilesList[i] select f.Key).Single());
            }

            CreateServerFiles(from f in FilesList.Skip(filesToRewrite / 2).Take(filesToRewrite) select f.Replace(Local, Server));

            // delete file on server

            await sync.Start(paths);

            AssertFoldersAreSame(Server, Local, FilesList.Count + 4 + filesToRewrite / 2);
        }

        #endregion

        #region Folders API

        [TestMethod]
        public async void Can_Upload_Empty_Folders()
        {
            var sync = Init();
            CreateLocalFolders(FoldersList);

            await sync.Start(paths);

            AssertFoldersAreSame(Server, Local);
        }

        #endregion

        #region Protected and Private Properties and Indexers

        private static List<string> FilesList {
            get { return new List<string> {"file1.txt", "file2.rtf", "file3.exe", "file4.docx", "file5.exe", "file6.diff"}; }
        }

        private static List<string> FoldersList {
            get { return new List<string> {"folder1", "folder5", "folder1\\folder2", "folder1\\folder3", "folder1\\folder3\\folder4"}; }
        }

        #endregion

        #region Protected And Private Methods

        private static void CreateLocalFiles(IEnumerable<string> files) {
            CreateFiles(from f in files select Local + "\\" + f);
        }

        private void CreateServerFiles(IEnumerable<string> files) {
            foreach (var file in CreateFiles(from f in files select Server + "\\" + f)) {
                map.Files.Add(file.Id, file);
                map.Folders["0"].Items.Add(file);
            }
        }

        private static List<TestFile> CreateFiles(IEnumerable<string> files) {
            var result = new List<TestFile>();
            var random = new Random();

            foreach (var file in files) {
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
                    FullPath = folder
                });
            }

            return result;
        }

        private Synchronization Init() {
            api = new Api(null, new FilesFake(map, Local, Server), new FoldersFake(map));
            return new Synchronization(api, new FilesComparison(TestFilesCollection), new FoldersComparison(TestFoldersCollection));
        }

        private static void AssertFoldersAreSame(string first, string second, params int[] exceptedFilesCount) {
            var filesInFirst = Directory.GetFiles(first);
            var filesInSecond = Directory.GetFiles(second);
            Assert.AreEqual(filesInFirst.Length, filesInSecond.Length);
            Assert.AreEqual(filesInFirst.Length, exceptedFilesCount.Length > 0 ? exceptedFilesCount[0] : 0);

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

            foreach (var folder in foldersInFirst) {
                Assert.IsTrue(foldersInSecond.Contains(folder));
                AssertFoldersAreSame(Path.Combine(first, folder), Path.Combine(second, folder), exceptedFilesCount.Skip(1).ToArray());
            }
        }

        #endregion
    }
}

// ReSharper restore InconsistentNaming