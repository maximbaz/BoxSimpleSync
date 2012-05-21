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
        public Events Events { get; set; }

        #endregion

        #region Constructors and Destructor

        public Api(User user) {
            Authentication = new Authentication(user);
            Files = new Files(user.AuthInfo);
            Folders = new Folders(user.AuthInfo);
            Events = new Events(user.AuthInfo);
        }

        #endregion
    }
}