using BoxSimpleSync.API.Model;

namespace BoxSimpleSync.API.Comparisons
{
    public class FoldersComparison : ItemsComparison<Folder>
    {
        #region Static Fields and Constants

        protected const string Collection = "folders";

        #endregion

        #region Constructors and Destructor

        public FoldersComparison() : base(Collection) {}

        #endregion

        #region Public and Internal Properties and Indexers

        public bool DeletedOnLocal {
            get { return !CreatedOnServer; }
        }

        public bool CreatedOnServer {
            get { return CreatedOnServer<MiniItem>(); }
        }

        public bool DeletedOnServer {
            get { return DeletedOnServer<MiniItem>(); }
        }

        public bool PreviousStateIsUnknown {
            get { return PreviousStateIsUnknown<MiniItem>(); }
        }

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