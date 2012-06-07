using System.Linq;
using BoxSimpleSync.API.Helpers;
using BoxSimpleSync.API.Model;

namespace BoxSimpleSync.API.Comparisons
{
    public class FilesComparison : ItemsComparison<File>
    {
        #region Static Fields and Constants

        private const string Collection = "files";

        #endregion

        #region Constructors and Destructor

        public FilesComparison(Pair<File> files) : base(files, Collection) {}

        #endregion

        #region Public and Internal Properties and Indexers

        public bool Updated {
            get { return !LocalIdenticalToServer; }
        }

        public bool UpdatedOnLocal {
            get { return !LocalIdenticalToServer && ServerIdenticalToDb; }
        }

        public bool UpdatedOnServer {
            get { return !ServerIdenticalToDb && LocalIdenticalToDb; }
        }

        public bool PreviousStateIsUnknown {
            get { return !ExistsInDb; }
        }

        #endregion

        #region Public and Internal Methods

        public static void Remove(string item) {
            Remove(item, Collection);
        }

        public static void Save(string folder, string sha1) {
            Save(folder, sha1, Collection);
        }

        #endregion

        #region Protected and Private Properties and Indexers

        protected bool LocalIdenticalToServer {
            get { return File.ComputeSha1(Items.Local) == Items.Server.Sha1; }
        }

        protected bool LocalIdenticalToDb {
            get {
                var miniFile = (from f in Query where f.FullPath == Items.Local select f).Single();
                return File.ComputeSha1(Items.Local) == miniFile.Sha1;
            }
        }

        #endregion
    }
}