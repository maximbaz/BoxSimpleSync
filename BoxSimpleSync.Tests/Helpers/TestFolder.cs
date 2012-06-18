using BoxSimpleSync.API.Model;

namespace BoxSimpleSync.Tests.Helpers
{
    internal class TestFolder : Folder
    {
        #region Public and Internal Properties and Indexers

        public string FullPath { get; set; }

        #endregion
    }

    internal class TestFile : File
    {
        #region Public and Internal Properties and Indexers

        public string FullPath { get; set; }

        #endregion
    }
}