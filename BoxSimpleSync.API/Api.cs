using System;
using System.Linq;

namespace BoxSimpleSync.API
{
    public class Api
    {
        #region Public Methods

        public void Authenticate(User user, Action onComplete) {
            new Authentication(user).Login(onComplete);
        }

        public void UploadFiles(string[] fileNames, string folderId, AuthInfo authInfo, Action onComplete) {
            new FileUploader(authInfo).Upload(fileNames, folderId, onComplete);
        }

        public void GetFolderInfo(string id, AuthInfo authInfo, Action<Folder> onComplete) {
            new Folders(authInfo).GetFolderInfo(id, onComplete);
        }

        #endregion
    }
}