using System.Linq;
using BoxSimpleSync.API.Helpers;
using BoxSimpleSync.API.Model;

namespace BoxSimpleSync.API.Comparisons
{
    public abstract class ItemsComparison<T> where T : Item
    {
        #region Constructors and Destructor

        protected ItemsComparison(string collection) {
            Collection = collection;
        }

        #endregion

        #region Public and Internal Properties and Indexers

        public Pair<T> Items { get; set; }

        #endregion

        #region Public and Internal Methods

        public bool CreatedOnServer<TMini>() where TMini : MiniItem {
            return !ExistsInDb<TMini>();
        }

        public bool DeletedOnServer<TMini>() where TMini : MiniItem {
            return ExistsInDb<TMini>();
        }

        public bool PreviousStateIsUnknown<TMini>() where TMini : MiniItem {
            return !ExistsInDb<TMini>();
        }

        #endregion

        #region Protected and Private Properties and Indexers

        protected static string Collection { get; private set; }

        #endregion

        #region Protected And Private Methods

        protected bool ExistsInDb<TMini>() where TMini : MiniItem {
            return (from f in Query<TMini>() where f.FullPath == Items.Local select f).Any();
        }

        protected virtual IQueryable<TMini> Query<TMini>() where TMini : MiniItem {
            return QueryTo<TMini>(Collection);
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