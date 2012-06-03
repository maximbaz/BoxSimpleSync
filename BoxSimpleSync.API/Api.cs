using BoxSimpleSync.API.Model;
using BoxSimpleSync.API.Request;

namespace BoxSimpleSync.API
{
    public class Api
    {
        #region Properties and Indexers

        public Authentication Authentication { get; private set; }
        public Files Files { get; private set; }
        public Folders Folders { get; private set; }

        #endregion

        #region Constructors and Destructor

        public Api(User user) {
            Authentication = new Authentication(user);
            Files = new Files(user.AuthInfo);
            Folders = new Folders(user.AuthInfo);
        }

        #endregion
    }
}