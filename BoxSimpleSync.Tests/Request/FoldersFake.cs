﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BoxSimpleSync.API.Interfaces;
using BoxSimpleSync.API.Model;
using BoxSimpleSync.Tests.Helpers;

namespace BoxSimpleSync.Tests.Request
{
    internal class FoldersFake : IFolders
    {
        #region Fields

        private readonly ServerMap map;

        #endregion

        #region Constructors and Destructor

        public FoldersFake(ServerMap map) {
            this.map = map;
        }

        #endregion

        #region IFolders Members

        public string AuthToken { get; set; }

        public Task<Folder> GetInfo(string id) {
            return Task.Run(() => map.Folders[id] as Folder);
        }

        public Task<Folder> Create(string name, string parentId) {
            return Task.Run(() => {
                if (!map.Folders.ContainsKey(parentId)) {
                    throw new InvalidOperationException("folder with id = [" + parentId + "] doesn't exist");
                }

                var folder = new TestFolder {
                    Id = Guid.NewGuid().ToString(),
                    Items = new List<Item>(),
                    Name = name,
                    FullPath = map.Folders[parentId].FullPath + "\\" + name
                };

                map.Folders.Add(folder.Id, folder);
                map.Folders[parentId].Items.Add(folder);
                Directory.CreateDirectory(folder.FullPath);

                return folder as Folder;
            });
        }

        public Task Delete(string id) {
            return Task.Run(() => {
                var folderPath = map.Folders[id].FullPath;

                foreach (var file in from f in map.Files
                                     where f.Value.Name.Contains(folderPath)
                                     select f.Key) {
                    map.Files.Remove(file);
                }

                foreach (var file in from f in map.Folders
                                     where f.Value.FullPath.Contains(folderPath)
                                     select f.Key) {
                    map.Folders.Remove(file);
                }

                Directory.Delete(folderPath, true);
            });
        }

        #endregion
    }
}