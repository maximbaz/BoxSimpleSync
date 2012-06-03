using System.Threading.Tasks;
using BoxSimpleSync.API.Model;

namespace BoxSimpleSync.API.Request
{
    public sealed class Folders
    {
        #region Static Fields and Constants

        private const string Url = "https://api.box.com/2.0/folders/{0}";

        #endregion

        #region Fields

        private readonly AuthInfo authInfo;

        #endregion

        #region Constructors and Destructor

        public Folders(AuthInfo authInfo) {
            this.authInfo = authInfo;
        }

        #endregion

        #region Public Methods

        public async Task<Folder> GetInfo(string id) {
            return JsonParse.Folder(await HttpRequest.Get(string.Format(Url, id), authInfo.Token));
        }

        public async Task<Folder> Create(string name, string parent) {
            return JsonParse.Folder(await HttpRequest.Post(string.Format(Url, parent), string.Format("{{\"name\":\"{0}\"}}", name), authInfo.Token));
        }

        #endregion
    }
}