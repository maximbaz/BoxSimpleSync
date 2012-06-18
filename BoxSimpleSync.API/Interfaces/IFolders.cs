using System.Threading.Tasks;
using BoxSimpleSync.API.Model;

namespace BoxSimpleSync.API.Interfaces
{
    public interface IFolders
    {
        #region Public and Internal Properties and Indexers

        string AuthToken { get; set; }

        #endregion

        #region Public and Internal Methods

        Task<Folder> GetInfo(string id);
        Task<Folder> Create(string name, string parentId);
        Task Delete(string id);

        #endregion
    }
}