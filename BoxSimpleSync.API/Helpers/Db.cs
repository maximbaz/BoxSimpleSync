using System.Linq;
using BoxSimpleSync.API.Model;
using FluentMongo.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace BoxSimpleSync.API.Helpers
{
    public static class Db
    {
        #region Public and Internal Methods

        public static IQueryable<T> Collection<T>(string name) {
            return MongoDb.GetCollection<T>(name).AsQueryable();
        }

        public static void Save<T>(T data, string collection) {
            MongoDb.GetCollection<T>(collection).Save(data);
        }

        public static void Remove(string name, BsonValue value, string collection)
        {
            MongoDb.GetCollection(collection).Remove(Query.EQ(name, value));
        }

        #endregion

        #region Protected and Private Properties and Indexers

        private static MongoDatabase MongoDb {
            get {
                return MongoServer
                    .Create("mongodb://localhost/?safe=true")
                    .GetDatabase("BoxSimpleSync");
            }
        }

        #endregion
    }
}