using System.Collections.Generic;
using BoxSimpleSync.API.Model;

namespace BoxSimpleSync.Tests.Helpers
{
    internal class ServerMap
    {
        #region Constructors and Destructor

        public ServerMap() {
            Files = new Dictionary<string, TestFile>();
            Folders = new Dictionary<string, TestFolder>();
        }

        #endregion

        #region Public and Internal Properties and Indexers

        public Dictionary<string, TestFile> Files { get; set; }
        public Dictionary<string, TestFolder> Folders { get; set; }

        #endregion
    }
}