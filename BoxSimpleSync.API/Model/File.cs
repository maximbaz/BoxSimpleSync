using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace BoxSimpleSync.API.Model
{
    [DebuggerDisplay("Id = {Id}, Name = {Name}, Sha1 = {Sha1}")]
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
            using (var stream = System.IO.File.OpenRead(file)) {
                var hash = new SHA1Managed().ComputeHash(stream);
                result = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
            }
            return result;
        }

        #endregion
    }
}