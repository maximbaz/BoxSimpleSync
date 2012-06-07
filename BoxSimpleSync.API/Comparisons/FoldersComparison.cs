using BoxSimpleSync.API.Model;

namespace BoxSimpleSync.API.Comparisons
{
    public class FoldersComparison : ItemsComparison<Folder>
    {
        #region Static Fields and Constants

        private const string Collection = "folders";

        #endregion

        #region Constructors and Destructor

        public FoldersComparison(Pair<Folder> items) : base(items, Collection) {}

        #endregion

        #region Public and Internal Methods

        public static void Remove(string item) {
            Remove(item, Collection);
        }

        public static void Save(string folder, string sha1) {
            Save(folder, sha1, Collection);
        }

        #endregion
    }
}