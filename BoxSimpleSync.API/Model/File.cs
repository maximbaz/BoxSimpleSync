using System;
using System.Security.Cryptography;
using IOFile = System.IO.File;

namespace BoxSimpleSync.API.Model
{
    public class File : Item
    {
        #region Constructors and Destructor

        public File() {
            Type = "file";
        }

        #endregion

        #region Public and Internal Properties and Indexers

        public string Sha1 { get; set; }

        #endregion

        #region Public and Internal Methods

        public static string ComputeSha1(string file) {
            string result;
            using (var stream = IOFile.OpenRead(file)) {
                var hash = new SHA1Managed().ComputeHash(stream);
                result = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
            }
            return result;
        }

        #endregion
    }
}