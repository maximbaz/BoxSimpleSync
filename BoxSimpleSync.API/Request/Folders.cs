using System.Threading.Tasks;
using BoxSimpleSync.API.Helpers;
using BoxSimpleSync.API.Model;

namespace BoxSimpleSync.API.Request
{
    public sealed class Folders
    {
        #region Static Fields and Constants

        private const string Url = "https://api.box.com/2.0/folders/{0}";

        #endregion

        #region Fields

        private readonly string authToken;

        #endregion

        #region Constructors and Destructor

        public Folders(string authToken) {
            this.authToken = authToken;
        }

        #endregion

        #region Public and Internal Methods

        public async Task<Folder> GetInfo(string id) {
            return JsonParse.Folder(await HttpRequest.Get(string.Format(Url, id), authToken));
        }

        public async Task<Folder> Create(string name, string parent) {
            return JsonParse.Folder(await HttpRequest.Post(string.Format(Url, parent), string.Format("{{\"name\":\"{0}\"}}", name), authToken));
        }

        #endregion
    }
}