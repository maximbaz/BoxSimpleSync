using System;
using System.Collections.Generic;
using System.IO;
using BoxSimpleSync.API;
using BoxSimpleSync.API.Model;
using BoxSimpleSync.Tests.Helpers;
using BoxSimpleSync.Tests.Request;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nito.AsyncEx.UnitTests;
using File = BoxSimpleSync.API.Model.File;
using IOFile = System.IO.File;

namespace BoxSimpleSync.Tests
{
    [AsyncTestClass]
    public class SynchronizationTests
    {
        private const string Folder = "__TESTS__";
        private const string Server = Folder + "\\Server";
        private const string Local = Folder + "\\Local";
        private ServerMap map;
        private List<Pair<string>> paths;
            
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

            paths = new List<Pair<string>> { new Pair<string>("", Local) };
        }

        [TestCleanup]
        public void CleanupFolders() {
            Directory.Delete(Folder, true);
        }

        [TestMethod]
        public async void CanSyncEmptyFolders() {
            var sync = Init();

            await sync.Start(paths);

            Assert.AreEqual(0, map.Files.Count);
            Assert.AreEqual(0, Directory.GetFiles(Local).Length);
            Assert.AreEqual(1, map.Folders.Count);
            Assert.AreEqual(0, Directory.GetDirectories(Local).Length);
            Assert.AreEqual(0, map.Folders["0"].Items.Count);
        }

        [TestMethod]
        public async void CanDownloadNewFiles() {
            var sync = Init();
            var files = new[] {Server + "\\file1.abc", Server + "\\file2.def", Server + "\\file3.ghik"};
            foreach (var file in CreateFiles(files)) {
                map.Files.Add(file.Id, file);
                map.Folders["0"].Items.Add(file);
            }

            await sync.Start(paths);

            Assert.AreEqual(3, map.Files.Count);
            Assert.AreEqual(3, Directory.GetFiles(Local).Length);
            Assert.AreEqual(1, map.Folders.Count);
            Assert.AreEqual(0, Directory.GetDirectories(Local).Length);
            Assert.AreEqual(0, map.Folders["0"].Items.Count);
        }

        private static IEnumerable<TestFile> CreateFiles(params string[] files) {
            var result = new List<TestFile>();

            foreach (var file in files) {
                using(var stream = IOFile.CreateText(file)) {
                    stream.Write(Guid.NewGuid().ToString());
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

        private Synchronization Init() {
            var api = new Api(null, new FilesFake(map, Local, Server), new FoldersFake(map));
            return new Synchronization(api, new FilesComparisonTest(), new FoldersComparisonTest());
        }
    }
}
