using System.Linq;
using System.Threading.Tasks;
using BoxSimpleSync.API.Model;
using Newtonsoft.Json.Linq;

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
            return ParseFolder(await HttpRequest.Get(string.Format(Url, id), authInfo.Token));
        }

        public async Task<Folder> Create(string name, string parent) {
            return ParseFolder(await HttpRequest.Post(string.Format(Url, parent), string.Format("{{\"name\":\"{0}\"}}", name), authInfo.Token));
        }

        #endregion

        #region Protected And Private Methods

        private static Folder ParseFolder(string json) {
            var jObject = JObject.Parse(json);
            var items = (from r in jObject["item_collection"]["entries"]
                         select new Item {
                             Id = r["id"].ToString(),
                             Name = r["name"].ToString(),
                             Type = r["type"].ToString()
                         }).ToList();

            return new Folder {
                Id = ParseValue(jObject, "id"),
                Name =  ParseValue(jObject, "name"),
                ParentId = ParseValue(jObject, "parent", "id"), 
                CreatedAt = ParseValue(jObject, "created_at"), 
                ModifiedAt = ParseValue(jObject, "modified_at"), 
                Items = items
            };
        }

        private static string ParseValue(JToken obj, params string[] keys) {
            var result = obj;
            foreach (var key in keys) {
                if (result == null || !result.HasValues)
                    return null;
                result = result[key];
            }
            return result == null ? null : result.ToString();
        }

        #endregion
    }
}