using System.Collections.Generic;
using System.Threading.Tasks;
using BoxSimpleSync.API.Model;

namespace BoxSimpleSync.API.Interfaces
{
    public interface IFiles {
        string AuthToken { get; set; }
        Task<List<File>> Upload(List<string> filePaths, string folderId);
        Task Download(string fileId, string location);
        Task<File> GetInfo(string id);
        Task Delete(string id);
    }
}