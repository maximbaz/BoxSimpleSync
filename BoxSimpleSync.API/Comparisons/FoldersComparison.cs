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
            RemoveByPattern<MiniItem>(item, Collection);
            FilesComparison.RemoveByPattern(item);
        }

        public static void Save(string folder) {
            Save(Get<MiniItem>(folder, Collection) ?? new MiniItem {FullPath = folder}, Collection);
        }

        #endregion
    }
}