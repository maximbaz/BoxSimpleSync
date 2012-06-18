using System.Threading.Tasks;
using BoxSimpleSync.API.Helpers;
using BoxSimpleSync.API.Interfaces;
using BoxSimpleSync.API.Model;

namespace BoxSimpleSync.API.Request
{
    public sealed class Folders : IFolders
    {
        #region Static Fields and Constants

        private const string Url = "https://api.box.com/2.0/folders/{0}";

        #endregion

        #region Public and Internal Properties and Indexers

        public string AuthToken { get; set; }

        #endregion

        #region Public and Internal Methods

        public async Task<Folder> GetInfo(string id) {
            return JsonParse.Folder(await HttpRequest.Get(string.Format(Url, id), AuthToken));
        }

        public async Task<Folder> Create(string name, string parentId) {
            return JsonParse.Folder(await HttpRequest.Post(string.Format(Url, parentId), string.Format("{{\"name\":\"{0}\"}}", name), AuthToken));
        }

        public Task Delete(string id) {
            return HttpRequest.Delete(string.Format(Url, id), AuthToken);
        }

        #endregion
    }
}