using BoxSimpleSync.API.Request;

namespace BoxSimpleSync.API
{
    public class Api
    {
        #region Constructors and Destructor

        public Api(string authToken) {
            Files = new Files(authToken);
            Folders = new Folders(authToken);
        }

        #endregion

        #region Public and Internal Properties and Indexers

        public Files Files { get; private set; }
        public Folders Folders { get; private set; }

        #endregion
    }
}