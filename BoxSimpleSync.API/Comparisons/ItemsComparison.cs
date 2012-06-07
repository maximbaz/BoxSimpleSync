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

        public bool DeletedOnLocal {
            get { return ServerIdenticalToDb; }
        }

        #endregion

        #region Protected and Private Properties and Indexers

        protected bool ExistsInDb {
            get { return (from f in Query where f.FullPath == Items.Local select f).Any(); }
        }

        protected bool ServerIdenticalToDb {
            get {
                var dbItem = (from f in Query where f.FullPath == Items.Local select f).Single();
                return dbItem.Sha1 == Items.Server.Sha1;
            }
        }

        protected IQueryable<DbItem> Query {
            get { return QueryTo(collection); }
        }

        #endregion

        #region Protected And Private Methods

        protected static void Remove(string item, string collection) {
            Db.Remove(collection, "FullPath", item);
        }

        protected static void Save(string folder, string sha1, string collection) {
            var item = (from f in QueryTo(collection) where f.FullPath == folder select f).SingleOrDefault() ?? new DbItem {FullPath = folder};
            item.Sha1 = sha1;
            Db.Save(collection, item);
        }

        protected static IQueryable<DbItem> QueryTo(string collection) {
            return Db.Collection<DbItem>(collection);
        }

        #endregion
    }
}