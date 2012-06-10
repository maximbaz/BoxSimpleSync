using System.Linq;
using BoxSimpleSync.API.Helpers;
using BoxSimpleSync.API.Model;

namespace BoxSimpleSync.API.Comparisons
{
    public abstract class ItemsComparison<T> where T : Item
    {
        #region Fields

        protected readonly Pair<T> Items;
        private readonly string collection;

        #endregion

        #region Constructors and Destructor

        protected ItemsComparison(Pair<T> items, string collection) {
            Items = items;
            this.collection = collection;
        }

        #endregion

        #region Public and Internal Properties and Indexers

        public bool CreatedOnServer {
            get { return !ExistsInDb; }
        }

        public bool DeletedOnServer {
            get { return ExistsInDb; }
        }

        public bool PreviousStateIsUnknown {
            get { return !ExistsInDb; }
        }

        #endregion

        #region Protected and Private Properties and Indexers

        protected bool ExistsInDb {
            get { return (from f in Query<MiniItem>() where f.FullPath == Items.Local select f).Any(); }
        }

        #endregion

        #region Protected And Private Methods

        protected virtual IQueryable<TMini> Query<TMini>() where TMini : MiniItem {
            return QueryTo<TMini>(collection);
        }

        protected static void Remove(string item, string collection) {
            Db.Remove("FullPath", item, collection);
        }

        protected static void RemoveByPattern<TMini>(string value, string collection) where TMini : MiniItem {
            foreach (var item in QueryTo<TMini>(collection).AsQueryable<TMini>().Where(x => x.FullPath.Contains(value))) {
                Remove(item.FullPath, collection);
            }
        }

        protected static TMini Get<TMini>(string fullPath, string collection) where TMini : MiniItem {
            return (from f in QueryTo<TMini>(collection) where f.FullPath == fullPath select f).SingleOrDefault();
        }

        protected static void Save<TMini>(TMini item, string collection) where TMini : MiniItem {
            Db.Save(item, collection);
        }

        protected static IQueryable<TMini> QueryTo<TMini>(string collection) where TMini : MiniItem {
            return Db.Collection<TMini>(collection);
        }

        #endregion
    }
}