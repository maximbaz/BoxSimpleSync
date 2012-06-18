using BoxSimpleSync.API.Interfaces;

namespace BoxSimpleSync.API
{
    public class Api
    {
        #region Constructors and Destructor

        public Api(string authToken, IFiles files, IFolders folders) {
            Files = files;
            Folders = folders;
            RefreshAuthToken(authToken);
        }

        #endregion

        #region Public and Internal Properties and Indexers

        public IFiles Files { get; private set; }
        public IFolders Folders { get; private set; }

        #endregion

        public void RefreshAuthToken(string authToken) {
            Files.AuthToken = authToken;
            Folders.AuthToken = authToken;
        }
    }
}